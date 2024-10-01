using System.Reflection;
using System.Runtime.CompilerServices;
using TACBuilder.ILMeta;
using TACBuilder.ILMeta.ILBodyParser;
using TACBuilder.ILTAC;
using TACBuilder.ILTAC.TypeSystem;
using TACBuilder.Utils;

namespace Usvm.TACBuilder;

class MethodTacBuilder
{
    public readonly MethodBase MethodBase;
    private readonly MethodMeta _meta;
    private readonly TACMethodInfo _tacMethodInfo = new();
    private readonly Module _declaringModule;
    public List<ILLocal> Locals => _tacMethodInfo.Locals;

    public List<ILLocal> Params => _tacMethodInfo.Params;
    public List<ILExpr> Temps => _tacMethodInfo.Temps;
    public List<ILExpr> Errs => _tacMethodInfo.Errs;
    public List<EHScope> Scopes => _tacMethodInfo.Scopes;
    public Dictionary<(int, int), ILMerged> Merged = new();
    public List<ILIndexedStmt> Tac = new();
    public Dictionary<int, BlockTacBuilder> BlockTacBuilders = new();
    private ILInstr _begin;
    public Dictionary<int, int?> ilToTacMapping = new();
    public Queue<BlockTacBuilder> Worklist = new();

    public MethodTacBuilder(MethodMeta meta)
    {
        _meta = meta;
        MethodBase = meta.MethodBase;
        _declaringModule = meta.MethodBase.Module;
    }

    public TACMethod Build()
    {
        _tacMethodInfo.Meta = _meta;
        // if (!_meta.HasMethodBody) return new TACMethod(_tacMethodInfo, []);
        _begin = _meta.FirstInstruction;
        int hasThisIndexingDelta = 0;
        if (!MethodBase.IsStatic)
        {
            _tacMethodInfo.Params.Add(
                new ILLocal(TypingUtil.ILTypeFrom(MethodBase.ReflectedType), NamingUtil.ArgVar(0)));
            hasThisIndexingDelta = 1;
        }

        _tacMethodInfo.Params.AddRange(MethodBase.GetParameters().OrderBy(p => p.Position).Select(l =>
            new ILLocal(TypingUtil.ILTypeFrom(l.ParameterType), NamingUtil.ArgVar(l.Position + hasThisIndexingDelta))));
        _tacMethodInfo.Locals = _meta.MethodBase.GetMethodBody().LocalVariables.OrderBy(l => l.LocalIndex)
            .Select(l => new ILLocal(TypingUtil.ILTypeFrom(l.LocalType), NamingUtil.LocalVar(l.LocalIndex))).ToList();
        // InitEhScopes();

        InitBlockBuilders();

        ProcessIL();

        foreach (var m in Merged.Values)
        {
            var temp = GetNewTemp(m.Type, m);
            m.MakeTemp(temp.ToString());
        }

        foreach (var bb in BlockTacBuilders)
        {
            bb.Value.InsertExtraAssignments();
        }

        ComposeTac();
        return new TACMethod(_tacMethodInfo, Tac);
    }

    private void InitBlockBuilders()
    {
        BlockTacBuilders = _meta.BasicBlocks.ToDictionary(bb => bb.Entry.idx, bb => new BlockTacBuilder(this, bb));

        foreach (var blockBuilder in BlockTacBuilders.Values)
        {
            blockBuilder.ConnectSuccsAndPreds(
                BlockTacBuilders.Values.Where(bb => blockBuilder.Meta.Successors.Contains(bb.Meta.Entry.idx)).ToList(),
                BlockTacBuilders.Values.Where(bb => blockBuilder.Meta.Predecessors.Contains(bb.Meta.Entry.idx))
                    .ToList());
        }
    }

    // move to meta smth
    // mb remove
    private void InitEhScopes()
    {
        foreach (var ehc in _meta.EhClauses)
        {
            EHScope scope = EHScope.FromClause(ehc);
            if (!_tacMethodInfo.Scopes.Contains(scope))
            {
                if (scope is EHScopeWithVarIdx s)
                {
                    s.ErrIdx = Errs.Count;
                    // _tacMethodInfo.Errs.Add(new ILLocal(TypingUtil.ILTypeFrom(s.Type), NamingUtil.ErrVar(s.ErrIdx)));
                }

                _tacMethodInfo.Scopes.Add(scope);
            }
        }
    }

    private void ProcessIL()
    {
        Worklist.Clear();

        foreach (var blockIdx in _meta.StartBlocksIndices)
        {
            Worklist.Enqueue(BlockTacBuilders[blockIdx]);
        }

        while (Worklist.Count > 0)
        {
            var current = Worklist.Dequeue();
            if (current.Rebuild())
            {
                foreach (var successor in current.Successors)
                {
                    Worklist.Enqueue(successor);
                }
            }
        }
    }

    private void ComposeTac()
    {
        // TODO separate ordering and Tac composition
        int lineNum = 0;
        var successors = _meta.Cfg.Succsessors;
        foreach (var ilIdx in successors.Keys)
        {
            ilToTacMapping[ilIdx] = null;
        }

        var tacBlocksIndexed = BlockTacBuilders.OrderBy(b => b.Key).ToList();
        foreach (var (i, bb) in tacBlocksIndexed.Select((e, i) => (i, e.Value)))
        {
            ilToTacMapping[bb.ILFirst] = lineNum;
            foreach (var line in bb.TacLines)
            {
                Tac.Add(new ILIndexedStmt(lineNum++, line));
            }

            if (tacBlocksIndexed.Count > i + 1 && successors.ContainsKey(bb.ILFirst) &&
                successors[bb.ILFirst].Count > 0 && successors[bb.ILFirst][0] != tacBlocksIndexed[i + 1].Key)
            {
                Tac.Add(new ILIndexedStmt(lineNum++, new ILGotoStmt(successors[bb.ILFirst][0])));
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

    internal ILExpr GetNewErr(Type type)
    {
        _tacMethodInfo.Errs.Add(new ILLocal(TypingUtil.ILTypeFrom(type), NamingUtil.ErrVar(_tacMethodInfo.Errs.Count)));
        return _tacMethodInfo.Errs.Last();
    }

    internal ILMerged GetMerged(int blockIdx, int stackDepth)
    {
        if (!Merged.ContainsKey((blockIdx, stackDepth)))
        {
            Merged.Add((blockIdx, stackDepth), new ILMerged(NamingUtil.MergedVar(Merged.Count)));
        }

        return Merged[(blockIdx, stackDepth)];
    }
}
