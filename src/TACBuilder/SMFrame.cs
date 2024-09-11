using System.Collections;
using System.Reflection;
using Usvm.IL.Parser;
using Usvm.IL.TypeSystem;

namespace Usvm.IL.TACBuilder;

class SMFrame
{
    public readonly EvaluationStack<ILExpr> Stack = new();
    public int ILFirst;
    internal ILInstr.Instr _firstInstr;
    public readonly List<ILStmt> TacLines = new();
    public ILInstr.Instr CurInstr;

    private MethodProcessor _mp;
    internal List<ILStmt> _lastTacLines = new();

    internal bool? _cachedTacLinesEq;

    private HashSet<SMFrame> _preds = new();

    // TODO make HashSet instead
    public List<ILStmt> ExtraAssignments = new();

    public SMFrame(MethodProcessor proc, SMFrame? pred, EvaluationStack<ILExpr> stack, ILInstr.Instr instr)
    {
        ILFirst = instr.idx;
        _firstInstr = instr;
        CurInstr = instr;
        _mp = proc;
        if (pred != null) _preds.Add(pred);
        _mp.Successors.TryAdd(ILFirst, []);
        if (pred == null)
            Stack.CloneFrom(stack);
    }

    // TODO move to Eval stack class 
    private static EvaluationStack<ILExpr> CopyOf(EvaluationStack<ILExpr> stack)
    {
        ILExpr[] newStack = new ILExpr[stack.Count];
        stack.CopyTo(newStack, 0);
        return new EvaluationStack<ILExpr>(newStack);
    }

    public void ResetVirtualStack()
    {
        Stack.ResetVirtualStack();
    }

    public void AddPredecessor(SMFrame pred)
    {
        _preds.Add(pred);
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
            _mp.TacBlocks[instr.idx].AddPredecessor(this);
        }
        else
        {
            _mp.TacBlocks.Add(instr.idx, new SMFrame(_mp, this, CopyOf(Stack), (ILInstr.Instr)instr));
            _mp.Worklist.Enqueue(_mp.TacBlocks[instr.idx]);
            return;
        }

        if (_cachedTacLinesEq == null) _cachedTacLinesEq = TacLines.SequenceEqual(_lastTacLines);
        if (_cachedTacLinesEq == false) _mp.Worklist.Enqueue(_mp.TacBlocks[instr.idx]);
    }

    public bool IsLeader(ILInstr instr)
    {
        return _mp.Leaders.Select(l => l.idx).Contains(instr.idx);
    }

    public List<ILLocal> Locals => _mp.Locals;
    public List<EHScope> Scopes => _mp.Scopes;
    public List<ILLocal> Params => _mp.Params;
    public List<ILExpr> Temps => _mp.Temps;
    public List<ILExpr> Errs => _mp.Errs;

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
        if (Stack.Count == 0) return MergeStacksValues();
        return ToSingleAddr(Stack.Pop());
    }

    private ILExpr PopSingleAddrVirt()
    {
        if (Stack.Count == 0) return MergeStacksValues();
        return ToSingleAddr(Stack.Pop(true));
    }

    // TODO reoder by lhs index
    public void InsertExtraAssignments()
    {
        var pos = TacLines.FindIndex(l => l is ILBranchStmt);
        pos = pos == -1 ? TacLines.Count : pos;
        TacLines.InsertRange(pos, ExtraAssignments);
    }

    private ILExpr MergeStacksValues()
    {
        if (_preds.Count == 0) throw new Exception("no pred exist for " + ILFirst + " at " + CurInstr.idx);
        List<(SMFrame, ILExpr)> values = _preds.Select(s => (s, s.PopSingleAddrVirt())).ToList();
        if (values.Select(p => p.Item2).Distinct().Count() == 1) return values.Select(p => p.Item2).First();
        ILLocal tmp = GetNewTemp(values[0].Item2.Type, new ILMergedValueExpr(values[0].Item2.Type));
        for (int i = 0; i < values.Count; i++)
        {
            values[i].Item1.ExtraAssignments.Add(new ILAssignStmt(tmp, values[i].Item2));
        }

        return tmp;
    }

    public void Push(ILExpr expr)
    {
        Stack.Push(expr);
    }

    public void ClearStack()
    {
        Stack.Clear();
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
        return new ILLocal(type, Logger.TempVarName(Temps.Count - 1));
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