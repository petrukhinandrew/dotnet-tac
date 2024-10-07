using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using TACBuilder.ILMeta;
using TACBuilder.ILMeta.ILBodyParser;
using TACBuilder.ILTAC;
using TACBuilder.ILTAC.TypeSystem;
using TACBuilder.Utils;

namespace TACBuilder;

class MethodTacBuilder
{
    private readonly MethodMeta _meta;
    private readonly TACMethodInfo _tacMethodInfo = new();
    public List<ILLocal> Locals => _tacMethodInfo.Locals;

    public List<ILExpr> Params => _tacMethodInfo.Params;
    public Dictionary<int, TempVar> Temps => _tacMethodInfo.Temps;
    public List<ILExpr> Errs => _tacMethodInfo.Errs;
    public List<EHScope> Scopes => _tacMethodInfo.Scopes;
    public Dictionary<(int, int), ILMerged> Merged = new();
    public List<ILIndexedStmt> Tac = new();
    public Dictionary<int, BlockTacBuilder> BlockTacBuilders = new();
    public Dictionary<int, int?> ilToTacMapping = new();
    public Queue<BlockTacBuilder> Worklist = new();

    public MethodTacBuilder(MethodMeta meta)
    {
        _meta = meta;
    }

    public TACMethod Build()
    {
        _tacMethodInfo.Meta = _meta;
        if (_meta.HasThis)
        {
            var thisType = TypingUtil.ILTypeFrom(_meta.ParametersType[0].BaseType);
            if (thisType is ILValueType)
                Params.Add(new ILManagedRef(new ILLocal(thisType, "this")));
            else
                Params.Add(new ILLocal(thisType, "this"));
        }

        Params.AddRange(_meta.ParametersType.Skip(_meta.HasThis ? 1 : 0).Select((l, i) =>
            new ILLocal(TypingUtil.ILTypeFrom(l.BaseType), NamingUtil.ArgVar(i))));

        Locals.AddRange(_meta.LocalVarsType
            .Select((l, i) => new ILLocal(TypingUtil.ILTypeFrom(l.BaseType), NamingUtil.LocalVar(i))).ToList());

        if (!_meta.HasMethodBody) return new TACMethod(_tacMethodInfo, []);
        InitBlockBuilders();

        ProcessIL();
        // TODO may be put together with temp vars somehow
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
