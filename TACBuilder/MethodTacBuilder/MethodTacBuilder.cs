using System.Reflection;
using TACBuilder.ILMeta;
using TACBuilder.ILMeta.ILBodyParser;
using TACBuilder.ILTAC;
using TACBuilder.ILTAC.TypeSystem;
using TACBuilder.Utils;

namespace Usvm.TACBuilder;

class MethodTacBuilder(MethodMeta meta)
{
    public readonly MethodInfo MethodInfo = meta.MethodInfo;
    private TACMethodInfo _tacMethodInfo = new();
    private readonly Module _declaringModule = meta.MethodInfo.Module;
    private List<ehClause> _ehs = new();

    public List<ILLocal> Locals => _tacMethodInfo.Locals;

    public List<ILLocal> Params => _tacMethodInfo.Params;
    public List<ILExpr> Temps => _tacMethodInfo.Temps;
    public List<ILExpr> Errs => _tacMethodInfo.Errs;
    public List<EHScope> Scopes => _tacMethodInfo.Scopes;
    public Dictionary<(int, int), ILMerged> Merged = new();
    public List<ILIndexedStmt> Tac = new();
    public List<ILInstr> Leaders = new();
    public Dictionary<int, List<int>> Successors = new();
    public Dictionary<int, BlockTacBuilder> TacBlocks = new();
    private ILInstr _begin;
    public readonly Dictionary<int, int?> ilToTacMapping = new();
    public Queue<BlockTacBuilder> Worklist = new();
    private MethodMeta _meta = meta;

    public TACMethod Build()
    {
        meta.Resolve();
        _tacMethodInfo.Meta = _meta;
        _begin = meta.FirstInstruction;
        _ehs = meta.EhClauses;
        int hasThis = 0;
        if (!MethodInfo.IsStatic)
        {
            _tacMethodInfo.Params.Add(
                new ILLocal(TypingUtil.ILTypeFrom(MethodInfo.ReflectedType), NamingUtil.ArgVar(0)));
            hasThis = 1;
        }

        _tacMethodInfo.Params.AddRange(MethodInfo.GetParameters().OrderBy(p => p.Position).Select(l =>
            new ILLocal(TypingUtil.ILTypeFrom(l.ParameterType), NamingUtil.ArgVar(l.Position + hasThis))));
        _tacMethodInfo.Locals = _meta.MethodInfo.GetMethodBody().LocalVariables.OrderBy(l => l.LocalIndex)
            .Select(l => new ILLocal(TypingUtil.ILTypeFrom(l.LocalType), NamingUtil.LocalVar(l.LocalIndex))).ToList();
        InitEhScopes();
        
        Leaders = CollectLeaders();
        ProcessIL();
        foreach (var m in Merged.Values)
        {
            var temp = GetNewTemp(m.Type, m);
            m.MakeTemp(temp.ToString());
        }

        foreach (var bb in TacBlocks)
        {
            bb.Value.InsertExtraAssignments();
        }

        ComposeTac();
        return new TACMethod(_tacMethodInfo, Tac);
    }

    // move to cfg
    private List<ILInstr> CollectLeaders()
    {
        ILInstr cur = _begin;
        HashSet<ILInstr> leaders = [cur];
        while (cur is not ILInstr.Back)
        {
            if (cur.IsJump())
            {
                leaders.Add(((ILInstrOperand.Target)cur.arg).value);
                leaders.Add(cur.next);
            }

            cur = cur.next;
        }

        return [.. leaders.OrderBy(l => l.idx)];
    }

    // move to meta smth
    private void InitEhScopes()
    {
        foreach (var ehc in _ehs)
        {
            EHScope scope = EHScope.FromClause(ehc);
            if (!_tacMethodInfo.Scopes.Contains(scope))
            {
                if (scope is EHScopeWithVarIdx s)
                {
                    s.ErrIdx = Errs.Count;
                    _tacMethodInfo.Errs.Add(new ILLocal(TypingUtil.ILTypeFrom(s.Type), NamingUtil.ErrVar(s.ErrIdx)));
                }

                _tacMethodInfo.Scopes.Add(scope);
            }
        }
    }

    private void EnqueueEhsBlocks()
    {
        foreach (var scope in Scopes)
        {
            int hbIndex = scope.ilLoc.hb.idx;
            Leaders.Add(scope.ilLoc.hb);

            if (scope is EHScopeWithVarIdx scopeWithVar)
            {
                TacBlocks[hbIndex] = new BlockTacBuilder(this, null,
                    new EvaluationStack<ILExpr>([Errs[scopeWithVar.ErrIdx]]),
                    (ILInstr.Instr)scopeWithVar.ilLoc.hb);
                // scopeWithVar.HandlerFrame = TacBlocks[hbIndex];
            }
            else
            {
                TacBlocks[hbIndex] =
                    new BlockTacBuilder(this, null, new EvaluationStack<ILExpr>(),
                        (ILInstr.Instr)scope.ilLoc.hb);
            }

            if (scope is FilterScope filterScope)
            {
                int fbIndex = filterScope.fb.idx;
                Leaders.Add(filterScope.fb);
                TacBlocks[fbIndex] = new BlockTacBuilder(this, null,
                    new EvaluationStack<ILExpr>([Errs[filterScope.ErrIdx]]),
                    (ILInstr.Instr)filterScope.fb);
                // filterScope.FilterFrame = TacBlocks[fbIndex];
                Worklist.Enqueue(TacBlocks[fbIndex]);
            }

            Worklist.Enqueue(TacBlocks[hbIndex]);
        }
    }

    private void ProcessIL()
    {
        TacBlocks[0] =
            new BlockTacBuilder(this, null, new EvaluationStack<ILExpr>(), (ILInstr.Instr)_begin);
        Successors.Add(0, []);
        Worklist.Clear();
        Worklist.Enqueue(TacBlocks[0]);
        EnqueueEhsBlocks();
        while (Worklist.Count > 0)
        {
            Worklist.Dequeue().Branch();
        }
    }

    private void ComposeTac()
    {
        // TODO separate ordering and Tac composition 
        int lineNum = 0;
        foreach (var m in Successors)
        {
            ilToTacMapping.Add(m.Key, null);
        }

        var tacBlocksIndexed = TacBlocks.OrderBy(b => b.Key).ToList();
        foreach (var (i, bb) in tacBlocksIndexed.Select((e, i) => (i, e.Value)))
        {
            ilToTacMapping[bb.ILFirst] = lineNum;
            foreach (var line in bb.TacLines)
            {
                Tac.Add(new ILIndexedStmt(lineNum++, line));
            }

            if (tacBlocksIndexed.Count > i + 1 && Successors.ContainsKey(bb.ILFirst) &&
                Successors[bb.ILFirst].Count > 0 && Successors[bb.ILFirst][0] != tacBlocksIndexed[i + 1].Key)
            {
                Tac.Add(new ILIndexedStmt(lineNum++, new ILGotoStmt(Successors[bb.ILFirst][0])));
            }
        }

        foreach (var stmt in Tac)
        {
            if (stmt.Stmt is ILBranchStmt branch)
            {
                branch.Target = (int)ilToTacMapping[branch.Target]!;
            }
        }
    }

    internal ILLocal GetNewTemp(ILType type, ILExpr value)
    {
        Temps.Add(value);
        return new ILLocal(type, NamingUtil.TempVar(Temps.Count - 1));
    }

    internal ILMerged GetMerged(int blockIdx, int stackDepth)
    {
        if (!Merged.ContainsKey((blockIdx, stackDepth)))
        {
            Merged.Add((blockIdx, stackDepth), new ILMerged(NamingUtil.MergedVar(Merged.Count)));
        }

        return Merged[(blockIdx, stackDepth)];
    }

    internal FieldInfo ResolveField(int target)
    {
        return _declaringModule.ResolveField(target,
            (MethodInfo.ReflectedType ?? MethodInfo.DeclaringType)!.GetGenericArguments(),
            MethodInfo.GetGenericArguments()) ?? throw new Exception("cannot resolve field");
    }

    internal Type ResolveType(int target)
    {
        return _declaringModule.ResolveType(target) ?? throw new Exception("cannot resolve type");
    }

    internal MethodBase ResolveMethod(int target)
    {
        return _declaringModule.ResolveMethod(target) ?? throw new Exception("cannot resolve method");
    }

    internal byte[] ResolveSignature(int target)
    {
        return _declaringModule.ResolveSignature(target);
    }

    internal string ResolveString(int target)
    {
        return _declaringModule.ResolveString(target);
    }

    internal ILInstr GetFirstInstr()
    {
        return _begin;
    }
}