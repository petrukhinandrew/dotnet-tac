using TACBuilder.BodyBuilder;
using TACBuilder.Exprs;
using TACBuilder.ILReflection;
using TACBuilder.ILTAC.TypeSystem;
using TACBuilder.Utils;

namespace TACBuilder;

class MethodBuilder(IlMethod method)
{
    private readonly IlMethod _method = method;
    public List<IlLocalVar> LocalVars => method.LocalVars;

    public List<IlValue> Params = new();
    public Dictionary<int, IlTempVar> Temps => method.Temps;
    public List<IlLocalVar> Errs => method.Errs;
    public List<EHScope> Scopes => method.Scopes;
    public readonly Dictionary<(int, int), IlMerged> Merged = new();
    public readonly List<IlStmt> Tac = new();
    public Dictionary<int, BlockTacBuilder> BlockTacBuilders = new();
    public Dictionary<int, int?> ilToTacMapping = new();
    public readonly Queue<BlockTacBuilder> Worklist = new();

    public List<IlStmt> Build()
    {
        Params.AddRange(_method.Parameters.Select((mp, index) => mp switch
        {
            IlMethod.Parameter p => new IlArgument(mp),
            IlMethod.This t => mp.Type.IsValueType switch
            {
                true => (IlValue)new IlManagedRef(new IlArgument(mp)),
                _ => new IlArgument(mp)
            },
            _ => throw new Exception($"Unknown method parameter type at {mp}")
        }));

        if (!_method.HasMethodBody) return [];
        InitBlockBuilders();

        ProcessIL();
        foreach (var m in Merged.Values)
        {
            var temp = GetNewTemp(m, Temps.Keys.Max() + 1);
            m.MakeTemp(temp);
        }

        foreach (var bb in BlockTacBuilders)
        {
            bb.Value.InsertExtraAssignments();
        }

        ComposeTac();
        // TODO
        // Debug.Assert(Tac.Last().Stmt is ILReturnStmt or ILEHStmt {Value: "throw"});
        return Tac;
    }

    private void InitBlockBuilders()
    {
        BlockTacBuilders = _method.BasicBlocks.ToDictionary(bb => bb.Entry.idx, bb => new BlockTacBuilder(this, bb));

        foreach (var blockBuilder in BlockTacBuilders.Values)
        {
            blockBuilder.ConnectSuccsAndPreds(
                BlockTacBuilders.Values.Where(bb => blockBuilder.Meta.Successors.Contains(bb.Meta.Entry.idx)).ToList(),
                BlockTacBuilders.Values.Where(bb => blockBuilder.Meta.Predecessors.Contains(bb.Meta.Entry.idx))
                    .ToList());
        }
    }

    private void ProcessIL()
    {
        Worklist.Clear();

        foreach (var blockIdx in _method.StartBlocksIndices)
        {
            Worklist.Enqueue(BlockTacBuilders[blockIdx]);
        }

        while (Worklist.Count > 0)
        {
            var current = Worklist.Dequeue();
            if (current.Rebuild() || current.Successors.Any(succ => !succ.BuiltAtLeastOnce))
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
        int lineNum = 0;
        var successors = _method.BasicBlocks.ToDictionary(bb => bb.Entry.idx, bb => bb.Successors);
        foreach (var ilIdx in successors.Keys.OrderBy(k => k))
        {
            ilToTacMapping[ilIdx] = null;
        }

        var tacBlocksIndexed = BlockTacBuilders.OrderBy(b => b.Key).ToList();
        foreach (var (i, bb) in tacBlocksIndexed.Select((e, i) => (i, e.Value)))
        {
            ilToTacMapping[bb.IlFirst] = lineNum;
            foreach (var line in bb.TacLines)
            {
                Tac.Add(line);
            }

            if (tacBlocksIndexed.Count > i + 1 && successors.ContainsKey(bb.IlFirst) &&
                successors[bb.IlFirst].Count > 0 && successors[bb.IlFirst][0] != tacBlocksIndexed[i + 1].Key)
            {
                Tac.Add(new IlGotoStmt(successors[bb.IlFirst][0]));
            }
        }

        foreach (var stmt in Tac)
        {
            if (stmt is IlBranchStmt branch)
            {
                branch.Target = (int)ilToTacMapping[branch.Target]!;
            }
        }
    }

    // TODO put merge here
    internal IlTempVar GetNewTemp(IlExpr value, int instrIdx)
    {
        if (!Temps.ContainsKey(instrIdx))
        {
            Temps.Add(instrIdx, new IlTempVar(Temps.Count, value));
        }

        return Temps[instrIdx];
    }

    internal IlExpr GetNewErr(Type type)
    {
        Errs.Add(new IlLocalVar(new IlType(type), Errs.Count, false));
        return Errs.Last();
    }

    internal IlMerged GetMerged(int blockIdx, int stackDepth)
    {
        if (!Merged.ContainsKey((blockIdx, stackDepth)))
        {
            Merged.Add((blockIdx, stackDepth), new IlMerged(NamingUtil.MergedVar(Merged.Count)));
        }

        return Merged[(blockIdx, stackDepth)];
    }
}
