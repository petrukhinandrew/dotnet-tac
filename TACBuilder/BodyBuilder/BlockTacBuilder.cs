using System.Diagnostics;
using TACBuilder.BodyBuilder;
using TACBuilder.Exprs;
using TACBuilder.ILReflection;
using TACBuilder.ILTAC.TypeSystem;
using TACBuilder.Utils;

namespace TACBuilder;

class BlockTacBuilder(MethodBuilder methodBuilder, IlBasicBlock meta)
{
    public IlBasicBlock Meta => meta;

    public bool BuiltAtLeastOnce => _builtAtLeastOnce;
    internal bool _builtAtLeastOnce = false;
    internal IlExpr? _switchRegister;
    internal int? _switchBranch;

    internal readonly ILInstr _firstInstr = meta.Entry;
    internal ILInstr CurInstr = meta.Entry;

    private EvaluationStack<IlExpr> _entryStackState =
        meta.StackErrType is null ? new() : new([methodBuilder.GetNewErr(meta.StackErrType)]);

    private EvaluationStack<IlExpr> _stack =
        meta.StackErrType is null ? new() : new([methodBuilder.GetNewErr(meta.StackErrType)]);

    private HashSet<BlockTacBuilder> _preds = new();
    private HashSet<BlockTacBuilder> _succs = new();
    public List<BlockTacBuilder> Successors => _succs.ToList();

    private readonly Dictionary<IlMerged, IlExpr> _extraAssignments = new();
    public readonly List<IlStmt> TacLines = new();

    public List<IlLocalVar> Locals => methodBuilder.LocalVars;
    public List<IlValue> Params => methodBuilder.Params;
    public int IlFirst => _firstInstr.idx;

    public void ConnectSuccsAndPreds(List<BlockTacBuilder> succs, List<BlockTacBuilder> preds)
    {
        _succs = succs.ToHashSet();
        _preds = preds.ToHashSet();
    }

    public bool StackInitIsTheSame()
    {
        if (_preds.Count == 0) return true;
        EvaluationStack<IlExpr> copy = EvaluationStack<IlExpr>.CopyOf(_entryStackState);

        var stacks = _preds.Where(bb => bb._builtAtLeastOnce)
            .Select((p, i) => (i, EvaluationStack<IlExpr>.CopyOf(p._stack))).ToList();
        List<IlExpr> newStack = new();
        var stackLengths = stacks.Select(p => p.Item2.Count).ToList();
        if (stacks.All(s => s.Item2.Count == 0)) return true;
        if (stackLengths.Max() != stackLengths.Min())
            Debug.Assert(stackLengths.Max() == stackLengths.Min(),
                meta.MethodMeta.Name + meta.MethodMeta.Parameters.Count);
        for (int j = 0; j < stackLengths.Max(); j++)
        {
            var values = stacks.Select(s => s.Item2.Pop()).ToList();
            if (values.Distinct().Count() == 1)
            {
                newStack.Add(values[0]);
                continue;
            }

            IlMerged tmp = methodBuilder.GetMerged(IlFirst, j);
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

    public void ResetStackToInitial()
    {
        _stack = EvaluationStack<IlExpr>.CopyOf(_entryStackState);
    }

    public void InsertExtraAssignments()
    {
        var pos = TacLines.FindIndex(l => l is IlBranchStmt);
        pos = pos == -1 ? TacLines.Count : pos;
        TacLines.InsertRange(pos,
            _extraAssignments.OrderBy(p => p.Key.ToString())
                .Select(p => new ILAssignStmt(p.Key, p.Value)));
    }

    public IlExpr Pop()
    {
        return _stack.Pop();
    }

    public void Push(IlExpr expr, int optInstrIdx = -1)
    {
        var instrIdx = optInstrIdx == -1 ? CurInstr.idx : optInstrIdx;
        if (expr is IlValue)
        {
            _stack.Push(expr.Coerced());
        }
        else
        {
            var tmp = GetNewTemp(expr, instrIdx);
            NewLine(new ILAssignStmt(tmp, expr));
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
        TacLines.Add(line);
    }

    public IlTempVar GetNewTemp(IlExpr value, int instrIdx)
    {
        return methodBuilder.GetNewTemp(value, instrIdx);
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
        return IlFirst.ToString();
    }
}
