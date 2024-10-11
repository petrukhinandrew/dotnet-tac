using TACBuilder.BodyBuilder;
using TACBuilder.ILReflection;
using TACBuilder.ILTAC.TypeSystem;
using TACBuilder.Utils;

namespace TACBuilder;

class MethodBuilder(ILMethod meta)
{
    private readonly ILMethod _meta = meta;
    public List<ILLocal> Locals => meta.Locals;

    public List<ILLValue> Params = new();
    public Dictionary<int, TempVar> Temps => meta.Temps;
    public List<ILExpr> Errs => meta.Errs;
    public List<EHScope> Scopes => meta.Scopes;
    public readonly Dictionary<(int, int), ILMerged> Merged = new();
    public readonly List<ILIndexedStmt> Tac = new();
    public Dictionary<int, BlockTacBuilder> BlockTacBuilders = new();
    public Dictionary<int, int?> ilToTacMapping = new();
    public readonly Queue<BlockTacBuilder> Worklist = new();

    public List<ILIndexedStmt>? Build()
    {
        Params.AddRange(_meta.Parameters.Select(mp => mp switch
        {
            ILMethod.Parameter p => new ILLocal(mp.IlType, p.Name),
            ILMethod.This t => mp.IlType.IsValueType switch
            {
                true => (ILLValue)new ILManagedRef(new ILLocal(mp.IlType, t.Name)),
                _ => new ILLocal(mp.IlType, t.Name)
            },
            _ => throw new Exception($"Unknown meta parameter type at {mp}")
        }));
        Locals.AddRange(_meta.LocalVarsType
            .Select((l, i) => new ILLocal(l, NamingUtil.LocalVar(i))).ToList());

        if (!_meta.HasMethodBody) return null;
        InitBlockBuilders();

        ProcessIL();
        foreach (var m in Merged.Values)
        {
            var temp = GetNewTemp(m, Temps.Keys.Max() + 1);
            m.MakeTemp(temp.ToString());
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
        BlockTacBuilders = _meta.BasicBlocks.ToDictionary(bb => bb.Entry.idx, bb => new BlockTacBuilder(this, bb));

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

        foreach (var blockIdx in _meta.StartBlocksIndices)
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
        var successors = _meta.BasicBlocks.ToDictionary(bb => bb.Entry.idx, bb => bb.Successors);
        foreach (var ilIdx in successors.Keys.OrderBy(k => k))
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

    // TODO put merge here
    internal TempVar GetNewTemp(ILExpr value, int instrIdx)
    {
        if (Temps.ContainsKey(instrIdx))
        {
        }
        else
        {
            Temps.Add(instrIdx, new TempVar(Temps.Count, value));
        }

        return Temps[instrIdx];
    }

    internal ILExpr GetNewErr(Type type)
    {
        Errs.Add(new ILLocal(new ILType(type), NamingUtil.ErrVar(Errs.Count)));
        return Errs.Last();
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
