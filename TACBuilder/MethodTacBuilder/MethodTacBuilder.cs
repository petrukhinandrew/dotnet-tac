using System.Reflection;
using Usvm.IL.Parser;
using Usvm.IL.TACBuilder.Utils;
using Usvm.IL.TypeSystem;

namespace Usvm.IL.TACBuilder;

class MethodTacBuilder
{
    private readonly Module _declaringModule;
    public readonly MethodInfo MethodInfo;
    private ehClause[] _ehs;
    public List<ILLocal> Locals;
    public List<ILLocal> Params = new();
    public List<ILExpr> Temps = new();
    public List<ILExpr> Errs = new();
    public Dictionary<(int, int), ILMerged> Merged = new();
    public List<ILIndexedStmt> Tac = new();
    public List<ILInstr> Leaders;
    public Dictionary<int, List<int>> Successors = new();
    public Dictionary<int, BlockTacBuilder> TacBlocks;
    private readonly ILInstr _begin;
    public List<EHScope> Scopes = [];
    public Dictionary<int, int?> ilToTacMapping = new();
    public Queue<BlockTacBuilder> Worklist = new();

    public MethodTacBuilder(Module declaringModule, MethodInfo methodInfo, IList<LocalVariableInfo> locals,
        ILInstr begin, ehClause[] ehs)
    {
        _begin = begin;
        TacBlocks = new Dictionary<int, BlockTacBuilder>();
        _ehs = ehs;
        _declaringModule = declaringModule;
        MethodInfo = methodInfo;
        int hasThis = 0;
        if (!methodInfo.IsStatic)
        {
            Params.Add(new ILLocal(TypingUtil.ILTypeFrom(methodInfo.ReflectedType), NamingUtil.ArgVar(0)));
            hasThis = 1;
        }

        Params.AddRange(methodInfo.GetParameters().OrderBy(p => p.Position).Select(l =>
            new ILLocal(TypingUtil.ILTypeFrom(l.ParameterType), NamingUtil.ArgVar(l.Position + hasThis))));
        Locals = locals.OrderBy(l => l.LocalIndex)
            .Select(l => new ILLocal(TypingUtil.ILTypeFrom(l.LocalType), NamingUtil.LocalVar(l.LocalIndex))).ToList();
        InitEhScopes();
        Leaders = CollectLeaders();
        ProcessNonExceptionalIL();
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
    }

    private List<ILInstr> CollectLeaders()
    {
        ILInstr cur = _begin;
        HashSet<ILInstr> leaders = [cur];
        while (cur is not ILInstr.Back)
        {
            if (cur.isJump())
            {
                leaders.Add(((ILInstrOperand.Target)cur.arg).value);
                leaders.Add(cur.next);
            }

            cur = cur.next;
        }

        return [.. leaders.OrderBy(l => l.idx)];
    }

    private void ProcessNonExceptionalIL()
    {
        TacBlocks[0] = new BlockTacBuilder(this, null, new EvaluationStack<ILExpr>(), (ILInstr.Instr)_begin);
        Successors.Add(0, []);
        Worklist.Clear();
        Worklist.Enqueue(TacBlocks[0]);
        EnqueueEhsBlocks();
        while (Worklist.Count > 0)
        {
            Worklist.Dequeue().Branch();
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
                scopeWithVar.HandlerFrame = TacBlocks[hbIndex];
            }
            else
            {
                TacBlocks[hbIndex] =
                    new BlockTacBuilder(this, null, new EvaluationStack<ILExpr>(), (ILInstr.Instr)scope.ilLoc.hb);
            }

            if (scope is FilterScope filterScope)
            {
                int fbIndex = filterScope.fb.idx;
                Leaders.Add(filterScope.fb);
                TacBlocks[fbIndex] = new BlockTacBuilder(this, null,
                    new EvaluationStack<ILExpr>([Errs[filterScope.ErrIdx]]),
                    (ILInstr.Instr)filterScope.fb);
                filterScope.FilterFrame = TacBlocks[fbIndex];
                Worklist.Enqueue(TacBlocks[fbIndex]);
            }

            Worklist.Enqueue(TacBlocks[hbIndex]);
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

    private void InitEhScopes()
    {
        foreach (var ehc in _ehs)
        {
            EHScope scope = EHScope.FromClause(ehc);
            if (!Scopes.Contains(scope))
            {
                if (scope is EHScopeWithVarIdx s)
                {
                    s.ErrIdx = Errs.Count;
                    Errs.Add(new ILLocal(TypingUtil.ILTypeFrom(s.Type), NamingUtil.ErrVar(s.ErrIdx)));
                }

                Scopes.Add(scope);
            }
        }
    }

    public ILLocal GetNewTemp(ILType type, ILExpr value)
    {
        Temps.Add(value);
        return new ILLocal(type, NamingUtil.TempVar(Temps.Count - 1));
    }

    public ILMerged GetMerged(int blockIdx, int stackDepth)
    {
        if (!Merged.ContainsKey((blockIdx, stackDepth)))
        {
            Merged.Add((blockIdx, stackDepth), new ILMerged(NamingUtil.MergedVar(Merged.Count)));
        }

        return Merged[(blockIdx, stackDepth)];
    }

    public FieldInfo ResolveField(int target)
    {
        // TODO mb reflectedtype 
        return _declaringModule.ResolveField(target, MethodInfo.DeclaringType!.GetGenericArguments(),
            MethodInfo.GetGenericArguments()) ?? throw new Exception("cannot resolve field");
    }

    public Type ResolveType(int target)
    {
        return _declaringModule.ResolveType(target) ?? throw new Exception("cannot resolve type");
    }

    public MethodBase ResolveMethod(int target)
    {
        return _declaringModule.ResolveMethod(target) ?? throw new Exception("cannot resolve method");
    }

    public byte[] ResolveSignature(int target)
    {
        return _declaringModule.ResolveSignature(target);
    }

    public string ResolveString(int target)
    {
        return _declaringModule.ResolveString(target);
    }

    public ILInstr GetFirstInstr()
    {
        return _begin;
    }
}