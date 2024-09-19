using System.Reflection;
using TACBuilder.ILMeta;
using TACBuilder.ILMeta.ILBodyParser;
using TACBuilder.ILTAC;
using TACBuilder.ILTAC.TypeSystem;
using TACBuilder.Utils;
using Usvm.TACBuilder.BlockTacBuilder;

namespace Usvm.TACBuilder.MethodTacBuilder;

/*
 * notes on lazy API 
 *
 * we separate ctor (1), fields instantiation(2) and building(3)
 * (1) may be just setting method info
 * (1) and (2) may be merged if needed for lazy strategy or does not produce overhead
 * (3) calls MethodMeta resolve first 
 */

// TODO primary ctor is to take MethodMeta only
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
    public List<ILInstr> Leaders = new();
    public Dictionary<int, List<int>> Successors = new();
    public Dictionary<int, BlockTacBuilder.BlockTacBuilder> TacBlocks;
    private readonly ILInstr _begin;
    public List<EHScope> Scopes = [];
    public readonly Dictionary<int, int?> ilToTacMapping = new();
    public Queue<BlockTacBuilder.BlockTacBuilder> Worklist = new();

    public MethodTacBuilder(MethodMeta meta) : this(meta.MethodInfo.Module, meta.MethodInfo,
        meta.MethodInfo.GetMethodBody()!.LocalVariables, meta.FirstInstruction, meta.EhClauses)
    {
    }

    // TODO remove declaring module from ctor 
    public MethodTacBuilder(Module declaringModule, MethodInfo methodInfo, IList<LocalVariableInfo> locals,
        ILInstr begin, List<ehClause> ehs)
    {
        _begin = begin;
        TacBlocks = new Dictionary<int, BlockTacBuilder.BlockTacBuilder>();
        _ehs = ehs.ToArray();
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
    }

    public TACMethod Build()
    {
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
        return new TACMethod();
    }

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

    private void ProcessNonExceptionalIL()
    {
        TacBlocks[0] =
            new BlockTacBuilder.BlockTacBuilder(this, null, new EvaluationStack<ILExpr>(), (ILInstr.Instr)_begin);
        Successors.Add(0, []);
        Worklist.Clear();
        Worklist.Enqueue(TacBlocks[0]);
        EnqueueEhsBlocks();
        while (Worklist.Count > 0)
        {
            BlockTacLineBuilder.Branch(Worklist.Dequeue());
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
                TacBlocks[hbIndex] = new BlockTacBuilder.BlockTacBuilder(this, null,
                    new EvaluationStack<ILExpr>([Errs[scopeWithVar.ErrIdx]]),
                    (ILInstr.Instr)scopeWithVar.ilLoc.hb);
                scopeWithVar.HandlerFrame = TacBlocks[hbIndex];
            }
            else
            {
                TacBlocks[hbIndex] =
                    new BlockTacBuilder.BlockTacBuilder(this, null, new EvaluationStack<ILExpr>(),
                        (ILInstr.Instr)scope.ilLoc.hb);
            }

            if (scope is FilterScope filterScope)
            {
                int fbIndex = filterScope.fb.idx;
                Leaders.Add(filterScope.fb);
                TacBlocks[fbIndex] = new BlockTacBuilder.BlockTacBuilder(this, null,
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
        // TODO what if reflected type is null and when it is possible 
        return _declaringModule.ResolveField(target, MethodInfo.ReflectedType!.GetGenericArguments(),
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