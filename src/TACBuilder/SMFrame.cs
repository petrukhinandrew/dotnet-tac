using System.Collections;
using System.Reflection;
using Usvm.IL.Parser;
using Usvm.IL.TypeSystem;

namespace Usvm.IL.TACBuilder;

class SMFrame
{
    public int ILFirst;
    public readonly List<ILStmt> TacLines = new();
    public ILInstr.Instr CurInstr;

    private readonly MethodProcessor _mp;
    private readonly EvaluationStack<ILExpr> _stack = new();

    // TODO introduce tac lines cache
    internal List<ILStmt> _lastTacLines = new();
    internal bool? _cachedTacLinesEq;
    internal ILInstr.Instr _firstInstr;

    private HashSet<SMFrame> _preds = new();

    // TODO make HashSet instead
    private readonly List<ILStmt> _extraAssignments = new();

    public SMFrame(MethodProcessor proc, SMFrame? pred, EvaluationStack<ILExpr> stack, ILInstr.Instr instr)
    {
        ILFirst = instr.idx;
        CurInstr = instr;
        _firstInstr = instr;
        _mp = proc;
        _mp.Successors.TryAdd(ILFirst, []);
        if (pred == null)
        {
            _stack.CloneFrom(stack);
        }
        else
        {
            _preds.Add(pred);
        }
    }

    public SMFrame SetVirtualStack(IEnumerable<ILExpr> stack)
    {
        var virtPred = new SMFrame(_mp, null,
            new EvaluationStack<ILExpr>(stack),
            _firstInstr);
        _preds.Add(virtPred);
        return virtPred;
    }

    public void ResetVirtualStack()
    {
        _stack.ResetVirtualStack();
    }

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
                new SMFrame(_mp, this, EvaluationStack<ILExpr>.CopyOf(_stack), (ILInstr.Instr)instr));
            _mp.Worklist.Enqueue(_mp.TacBlocks[instr.idx]);
            return;
        }

        _cachedTacLinesEq ??= TacLines.SequenceEqual(_lastTacLines);
        if (_cachedTacLinesEq == false) _mp.Worklist.Enqueue(_mp.TacBlocks[instr.idx]);
    }

    public bool IsLeader(ILInstr instr)
    {
        return _mp.Leaders.Select(l => l.idx).Contains(instr.idx);
    }

    public List<ILLocal> Locals => _mp.Locals;
    public List<ILLocal> Params => _mp.Params;
    public List<ILExpr> Temps => _mp.Temps;

    public string GetMethodName()
    {
        return _mp.MethodInfo.Name;
    }

    public Type GetMethodReturnType()
    {
        return _mp.MethodInfo.ReturnParameter.ParameterType;
    }

    public ILExpr PopSingleAddr()
    {
        if (_stack.Count == 0) return MergeStacksValues();
        return ToSingleAddr(_stack.Pop());
    }

    private ILExpr PopSingleAddrVirt()
    {
        if (_stack.Count == 0) return MergeStacksValues();
        return ToSingleAddr(_stack.Pop(true));
    }

    // TODO reoder by lhs index
    public void InsertExtraAssignments()
    {
        var pos = TacLines.FindIndex(l => l is ILBranchStmt);
        pos = pos == -1 ? TacLines.Count : pos;
        TacLines.InsertRange(pos, _extraAssignments);
    }

    private ILExpr MergeStacksValues()
    {
        if (_preds.Count == 0) throw new Exception("no pred exist for " + ILFirst + " at " + CurInstr.idx);
        List<(SMFrame, ILExpr)> values = _preds.Select(s => (s, s.PopSingleAddrVirt())).ToList();
        if (values.Select(p => p.Item2).Distinct().Count() == 1) return values.Select(p => p.Item2).First();
        ILLocal tmp = GetNewTemp(values[0].Item2.Type, new ILMergedValueExpr(values[0].Item2.Type));
        for (int i = 0; i < values.Count; i++)
        {
            values[i].Item1._extraAssignments.Add(new ILAssignStmt(tmp, values[i].Item2));
        }

        return tmp;
    }

    public void Push(ILExpr expr)
    {
        _stack.Push(expr);
    }

    public void ClearStack()
    {
        _stack.Clear();
    }

    public void PushLiteral<T>(T value)
    {
        ILLiteral lit = new ILLiteral(TypeSolver.Resolve(typeof(T)), value?.ToString() ?? "");
        Push(lit);
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

    public ILLocal GetNewTemp(ILType type, ILExpr value)
    {
        Temps.Add(value);
        return new ILLocal(type, NamingUtil.TempVar(Temps.Count - 1));
    }

    public void NewLine(ILStmt line)
    {
        TacLines.Add(line);
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
        return obj is SMFrame f && ILFirst == f.ILFirst;
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