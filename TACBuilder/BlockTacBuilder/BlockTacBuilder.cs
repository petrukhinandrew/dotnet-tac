using System.Diagnostics;
using System.Reflection;
using Usvm.IL.Parser;
using Usvm.IL.TypeSystem;

namespace Usvm.IL.TACBuilder;

class BlockTacBuilder
{
    internal ILInstr.Instr _firstInstr;
    private readonly MethodTacBuilder _mp;

    public ILInstr.Instr CurInstr;

    private EvaluationStack<ILExpr> _entryStackState = new();
    private EvaluationStack<ILExpr> _stack = new();

    private HashSet<BlockTacBuilder> _preds = new();

    private readonly Dictionary<ILMerged, ILExpr> _extraAssignments = new();
    public readonly List<ILStmt> TacLines = new();

    public BlockTacBuilder(MethodTacBuilder proc, BlockTacBuilder? pred, EvaluationStack<ILExpr> stack,
        ILInstr.Instr instr)
    {
        _mp = proc;
        _firstInstr = instr;
        CurInstr = instr;
        _mp.Successors.TryAdd(ILFirst, []);
        if (pred == null)
        {
            _stack = EvaluationStack<ILExpr>.CopyOf(stack);
            _entryStackState = EvaluationStack<ILExpr>.CopyOf(stack);
        }
        else
        {
            _preds.Add(pred);
        }
    }

    public List<ILLocal> Locals => _mp.Locals;
    public List<ILLocal> Params => _mp.Params;
    public List<ILExpr> Temps => _mp.Temps;
    public int ILFirst => _firstInstr.idx;
    public string MethodName => _mp.MethodInfo.Name;
    public Type MethodReturnType => _mp.MethodInfo.ReturnParameter.ParameterType;

    public void ContinueBranchingTo(ILInstr uncond, ILInstr? cond)
    {
        if (cond != null) ContinueTo(cond);
        ContinueTo(uncond);
    }

    public void ContinueBranchingToMultiple(List<ILInstr> targets)
    {
        foreach (var target in targets)
        {
            ContinueTo(target);
        }
    }

    private void ContinueTo(ILInstr instr)
    {
        _mp.Successors[ILFirst].Insert(0, instr.idx);
        if (_mp.TacBlocks.ContainsKey(instr.idx))
        {
            _mp.TacBlocks[instr.idx]._preds.Add(this);
        }
        else
        {
            _mp.TacBlocks.Add(instr.idx,
                new BlockTacBuilder(_mp, this, new EvaluationStack<ILExpr>(), (ILInstr.Instr)instr));
        }

        _mp.Worklist.Enqueue(_mp.TacBlocks[instr.idx]);
    }

    private static int _mergedCnt = 0;

    public bool StackInitIsTheSame()
    {
        if (_preds.Count == 0) return false;
        EvaluationStack<ILExpr> copy = EvaluationStack<ILExpr>.CopyOf(_entryStackState);
        var stacks = _preds.Select((p, i) => (i, EvaluationStack<ILExpr>.CopyOf(p._stack))).ToList();
        List<ILExpr> newStack = new();
        var lengths = stacks.Select(p => p.Item2.Count);
        Debug.Assert(lengths.Max() == lengths.Min());
        for (int j = 0; j < lengths.Max(); j++)
        {
            var values = stacks.Select(s => s.Item2.Pop()).ToList();
            if (values.Distinct().Count() == 1)
            {
                newStack.Add(values[0]);
                continue;
            }

            ILMerged tmp = _mp.GetMerged(_mergedCnt++);
            tmp.MergeOf(values);
            foreach (var (i, p) in _preds.Select((v, i) => (i, v)))
            {
                p._extraAssignments[tmp] = values[i];
            }
        }

        _entryStackState = new EvaluationStack<ILExpr>(newStack);
        return copy.SequenceEqual(_entryStackState);
    }

    public void ResetStackToInitial()
    {
        _stack = EvaluationStack<ILExpr>.CopyOf(_entryStackState);
    }

    public bool IsLeader(ILInstr instr)
    {
        return _mp.Leaders.Select(l => l.idx).Contains(instr.idx);
    }

    public void InsertExtraAssignments()
    {
        var pos = TacLines.FindIndex(l => l is ILBranchStmt);
        pos = pos == -1 ? TacLines.Count : pos;
        TacLines.InsertRange(pos,
            _extraAssignments.OrderBy(p => p.Key.ToString())
                .Select(p => new ILAssignStmt(p.Key, p.Value)));
    }

    // private ILExpr MergeStacksValues(int targetInstrIdx)
    // {
    //     if (_preds.Count == 0) throw new Exception("no pred exist for " + ILFirst + " at " + CurInstr.idx);
    //     List<(BlockTacBuilder, ILExpr)> values = _preds.Select(s => (s, s.PopSingleAddrVirt(targetInstrIdx))).ToList();
    //     if (values.Select(p => p.Item2).Distinct().Count() == 1) return values.Select(p => p.Item2).First();
    //     ILMerged tmp = _mp.GetMerged(targetInstrIdx);
    //     tmp.MergeOf(values.Select(p => p.Item2).ToList());
    //     foreach (var (frame, val) in values)
    //     {
    //         frame._extraAssignments[tmp] = val;
    //     }
    //
    //     return tmp;
    // }

    public ILExpr Pop()
    {
        return _stack.Pop();
    }

    public void Push(ILExpr expr)
    {
        // check expr Type
        // if < ILInt => push ((ILInt) expr)
        // coercion
        _stack.Push(expr);
    }

    public void ClearStack()
    {
        _stack.Clear();
    }

    private ILExpr ToSingleAddr(ILExpr val)
    {
        if (val is not ILValue)
        {
            ILLocal tmp = GetNewTemp(val.Type, val);
            NewLine(new ILAssignStmt(tmp, val));
            val = tmp;
        }

        return val;
    }

    public void NewLine(ILStmt line)
    {
        TacLines.Add(line);
    }

    public void PushLiteral<T>(T value)
    {
        ILLiteral lit = new ILLiteral(TypingUtil.ILTypeFrom(typeof(T)), value?.ToString() ?? "");
        Push(lit);
    }

    public ILLocal GetNewTemp(ILType type, ILExpr value)
    {
        return _mp.GetNewTemp(type, value);
    }

    public FieldInfo ResolveField(int target)
    {
        return _mp.ResolveField(target);
    }

    public Type ResolveType(int target)
    {
        return _mp.ResolveType(target);
    }

    public MethodBase ResolveMethod(int target)
    {
        return _mp.ResolveMethod(target);
    }

    // TODO find test case with calli
    public byte[] ResolveSignature(int target)
    {
        return _mp.ResolveSignature(target);
    }

    public string ResolveString(int target)
    {
        return _mp.ResolveString(target);
    }


    public override bool Equals(object? obj)
    {
        return obj is BlockTacBuilder f && ILFirst == f.ILFirst;
    }

    public override int GetHashCode()
    {
        return ILFirst;
    }

    public override string ToString()
    {
        return ILFirst.ToString();
    }
}