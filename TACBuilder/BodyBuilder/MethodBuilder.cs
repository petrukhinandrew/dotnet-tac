using System.Diagnostics;
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
    public Dictionary<int, List<IlTempVar>> Temps = new(); //=> method.Temps;
    public List<IlErrVar> Errs => method.Errs;
    public List<EhScope> EhScopes => method.Scopes;
    public readonly Dictionary<(int, int), IlMerged> Merged = new();
    public readonly List<IlStmt> Tac = new();
    public Dictionary<int, BlockTacBuilder> BlockTacBuilders = new();
    public readonly Queue<BlockTacBuilder> Worklist = new();

    public List<IlStmt> Build()
    {
        try
        {
            Params.AddRange(_method.Parameters.Select((mp, index) => mp switch // new IlCall.Argument(mp)
                {
                    IlMethod.Parameter p => new IlCall.Argument(mp),
                    IlMethod.This t => mp.Type.IsValueType switch
                    {
                        true => (IlValue)new IlManagedRef(new IlCall.Argument(mp)),
                        _ => new IlCall.Argument(mp)
                    },
                    _ => throw new Exception($"Unknown method parameter type at {mp}")
                }
            ));

            if (!_method.HasMethodBody) return [];
            InitBlockBuilders();

            ProcessIl();
            foreach (var m in Merged.Values)
            {
                var newTmpIdx = Temps.Values.Select(v => v.Count).Sum();
                var temp = GetNewTemp(m, newTmpIdx, 0);
                m.MakeTemp(temp);
            }

            foreach (var bb in BlockTacBuilders)
            {
                bb.Value.InsertExtraAssignments();
            }

            ComposeTac();
            EhScopes.ForEach(scope => Debug.Assert(scope.tacLoc.te < scope.tacLoc.hb));
            var idx = 0;
            foreach (var tmp in Temps.SelectMany(keyValuePair => keyValuePair.Value))
            {
                method.Temps[idx++] = tmp;
            }
        }
        catch (KnownBug kb)
        {
            Console.WriteLine(kb.Message + " found at " + (method.DeclaringType?.ToString() ?? " ") + " " +
                              method);
            return [];
        }
        catch (Exception e)
        {
            Console.WriteLine(_method.Name + " FAIL");
            return [];
        }

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

    private void ProcessIl()
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
        Dictionary<int, int?> ilToTacMapping = new();
        var successors = _method.BasicBlocks.ToDictionary(bb => bb.Entry.idx, bb => bb.Successors);
        foreach (var ilIdx in successors.Keys.Concat(_method.BasicBlocks.Select(bb => bb.Exit.idx)).OrderBy(k => k))
        {
            ilToTacMapping[ilIdx] = null;
        }

        var tacBlocksIndexed = BlockTacBuilders.OrderBy(b => b.Key).ToList();
        foreach (var (i, bb) in tacBlocksIndexed.Select((e, i) => (i, e.Value)))
        {
            ilToTacMapping[bb.IlFirst] = Tac.Count;
            foreach (var line in bb.TacLines)
            {
                Tac.Add(line);
            }

            if (bb.TacLines.Count == 0)
            {
                ilToTacMapping[bb.Meta.Exit.idx] = Tac.Count;
                continue;
            }

            if (tacBlocksIndexed.Count > i + 1 && successors.ContainsKey(bb.IlFirst) &&
                successors[bb.IlFirst].Count > 0 && successors[bb.IlFirst][0] != tacBlocksIndexed[i + 1].Key &&
                bb.TacLines.Last() is not IlLeaveStmt && bb.TacLines.Last() is not IlGotoStmt)
            {
                Tac.Add(new IlGotoStmt(successors[bb.IlFirst][0]));
            }

            ilToTacMapping[bb.Meta.Exit.idx] = Tac.Count - 1;
        }

        foreach (var stmt in Tac)
        {
            if (stmt is IlBranchStmt branch)
            {
                branch.Target = (int)ilToTacMapping[branch.Target]!;
            }
        }

        foreach (var scope in method.Scopes)
        {
            if (scope is FilterScope filterScope && ilToTacMapping.TryGetValue(filterScope.fb, out var fbv))
                filterScope.fbt = (int)fbv!;
            if (ilToTacMapping.TryGetValue(scope.ilLoc.tb, out var tbv))
                scope.tacLoc.tb = (int)tbv!;
            if (ilToTacMapping.TryGetValue(scope.ilLoc.te, out var tev))
                scope.tacLoc.te = (int)tev!;
            if (ilToTacMapping.TryGetValue(scope.ilLoc.hb, out var hbv))
                scope.tacLoc.hb = (int)hbv!;
            if (ilToTacMapping.TryGetValue(scope.ilLoc.he, out var hev))
                scope.tacLoc.he = (int)hev!;
            if (scope.tacLoc.te >= scope.tacLoc.hb || scope.tacLoc.hb > scope.tacLoc.he)
                Console.WriteLine(method.Name);
        }
    }

    internal IlTempVar GetNewTemp(IlExpr value, int instrIdx, int internalIdx)
    {
        var tmpIdx = Temps.Select(v => v.Value.Count).Sum();
        if (!Temps.TryGetValue(instrIdx, out var tmps))
        {
            Debug.Assert(internalIdx == 0);
            tmps =
            [
                new IlTempVar(tmpIdx, value)
            ];
            Temps.Add(instrIdx, tmps);
            return tmps[0];
        }

        if (tmps.Count > internalIdx) return tmps[internalIdx];
        Debug.Assert(tmps.Count == internalIdx);
        tmps.Add(new IlTempVar(tmpIdx, value));
        return tmps.Last();
    }

    internal IlExpr GetNewErr(Type type)
    {
        Errs.Add(new IlErrVar(IlInstanceBuilder.GetType(type), Errs.Count));
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