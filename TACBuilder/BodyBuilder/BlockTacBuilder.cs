using System.Diagnostics;
using TACBuilder.BodyBuilder;
using TACBuilder.BodyBuilder.ILBodyParser;
using TACBuilder.Exprs;
using TACBuilder.ILReflection;
using TACBuilder.Utils;

namespace TACBuilder;

class BlockTacBuilder(MethodBuilder methodBuilder, IlBasicBlock meta)
{
    public IlBasicBlock Meta => meta;

    public bool BuiltAtLeastOnce => _builtAtLeastOnce;
    internal bool _builtAtLeastOnce = false;

    internal List<IlType> ArrayTypesRegister = new();
    internal List<int> ArrayDimsRegister = new();
    
    internal IlExpr? SwitchRegister;
    internal int? SwitchBranch;

    internal readonly IlInstr FirstInstr = meta.Entry;
    internal IlInstr CurInstr = meta.Entry;

    private EvaluationStack<IlExpr> _entryStackState =
        meta.StackErrType is null ? new EvaluationStack<IlExpr>() : new EvaluationStack<IlExpr>([methodBuilder.GetNewErr(meta.StackErrType)]);

    private EvaluationStack<IlExpr> _stack =
        meta.StackErrType is null ? new EvaluationStack<IlExpr>() : new EvaluationStack<IlExpr>([methodBuilder.GetNewErr(meta.StackErrType)]);

    private HashSet<BlockTacBuilder> _preds = new();
    private HashSet<BlockTacBuilder> _succs = new();
    public List<BlockTacBuilder> Successors => _succs.ToList();

    private readonly Dictionary<IlMerged, IlExpr> _extraAssignments = new();
    public readonly List<IlStmt> TacLines = new();

    public List<IlLocalVar> Locals => methodBuilder.LocalVars;
    public List<IlValue> Params => methodBuilder.Params;
    public int IlFirst => FirstInstr.idx;

    public void ConnectSuccsAndPreds(List<BlockTacBuilder> succs, List<BlockTacBuilder> preds)
    {
        _succs = succs.ToHashSet();
        _preds = preds.ToHashSet();
    }

    public bool StackInitIsTheSame()
    {
        if (_preds.Count == 0) return true;
        var copy = EvaluationStack<IlExpr>.CopyOf(_entryStackState);

        var stacks = _preds.Where(bb => bb._builtAtLeastOnce)
            .Select((p, i) => (i, EvaluationStack<IlExpr>.CopyOf(p._stack))).ToList();
        List<IlExpr> newStack = new();
        var stackLengths = stacks.Select(p => p.Item2.Count).ToList();
        if (stacks.All(s => s.Item2.Count == 0)) return true;
        if (stackLengths.Max() != stackLengths.Min())
            Debug.Assert(stackLengths.Max() == stackLengths.Min(),
                Meta.MethodMeta!.Name + Meta.MethodMeta.Parameters.Count);
        for (var j = 0; j < stackLengths.Max(); j++)
        {
            var values = stacks.Select(s => s.Item2.Pop()).ToList();
            if (values.Distinct().Count() == 1)
            {
                newStack.Add(values[0]);
                continue;
            }

            var tmp = methodBuilder.GetMerged(IlFirst, j);
            tmp.MergeOf(values);
            foreach (var (i, p) in _preds.Where(bb => bb._builtAtLeastOnce).Select((v, i) => (i, v)))
            {
                p._extraAssignments[tmp] = values[i];
            }

            newStack.Add(tmp);
        }

        newStack.Reverse();
        _entryStackState = new EvaluationStack<IlExpr>(newStack);
        return copy.SequenceEqual(_entryStackState);
    }

    private void ResetStackToInitial()
    {
        _stack = EvaluationStack<IlExpr>.CopyOf(_entryStackState);
    }

    public void Reset()
    {
        ResetStackToInitial();
        TacLines.Clear();
        CurInstr = FirstInstr;
        TempIndexer.Clear();
    }

    internal Dictionary<int, int> TempIndexer = new();

    public void InsertExtraAssignments()
    {
        var pos = TacLines.FindIndex(l => l is IlBranchStmt);
        pos = pos == -1 ? TacLines.Count : pos;
        TacLines.InsertRange(pos,
            _extraAssignments.OrderBy(p => p.Key.ToString())
                .Select(p => new IlAssignStmt(p.Key, p.Value)));
    }

    public IlExpr Pop()
    {
        return _stack.Pop();
    }

    public void Push(IlExpr expr, int optInstrIdx = -1)
    {
        var instrIdx = optInstrIdx == -1 ? CurInstr.idx : optInstrIdx;
        if (expr is IlSimpleValue)
        {
            _stack.Push(expr.Coerced());
        }
        else
        {
            var tmp = GetNewTemp(expr, instrIdx);
            NewLine(new IlAssignStmt(tmp, expr));
            _stack.Push(tmp.Coerced());
        }
    }

    public void ClearStack()
    {
        _stack.Clear();
    }

    internal bool CurInstrIsLast()
    {
        return CurInstr == Meta.Exit;
    }

    public void NewLine(IlStmt line)
    {
        if (CurInstr.idx < methodBuilder.MonoInstructions.Count)
        {
            var sp = methodBuilder.MonoInstructions[CurInstr.idx].SequencePoint;
            if (sp != null)
            {
                line.Line = sp.StartLine;
            }
        }
        TacLines.Add(line);
    }

    public IlTempVar GetNewTemp(IlExpr value, int? instrIdx = null)
    {
        TempIndexer.TryAdd(CurInstr.idx, 0);
        return methodBuilder.GetNewTemp(value, instrIdx ?? CurInstr.idx, internalIdx: TempIndexer[CurInstr.idx]++);
    }

    public override bool Equals(object? obj)
    {
        return obj is BlockTacBuilder f && IlFirst == f.IlFirst;
    }

    public override int GetHashCode()
    {
        return IlFirst;
    }

    public override string ToString()
    {
        return $"{Meta.Entry.idx} {Meta.Exit.idx}";
    }
}