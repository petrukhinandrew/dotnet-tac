using System.Collections;
using System.Reflection;
using Usvm.IL.Parser;
using Usvm.IL.TypeSystem;

namespace Usvm.IL.TACBuilder;

class SMFrame(MethodProcessor proc, SMFrame? pred, Stack<ILExpr> stack, ILInstr.Instr instr)
{
    public readonly Stack<ILExpr> Stack = stack;
    public int ILFirst = instr.idx;
    private ILInstr.Instr _firstInstr = instr;
    public readonly List<ILStmt> TacLines = new();
    public ILInstr.Instr CurInstr = instr;
    private MethodProcessor _mp = proc;
    private bool _isMerging = false;
    private Dictionary<SMFrame, Stack<ILExpr>> _preds = new();
    public List<ILStmt> ExtraAssignments = new();

    private static Stack<ILExpr> stackCopy(Stack<ILExpr> stack)
    {
        ILExpr[] newStack = new ILExpr[stack.Count];
        stack.CopyTo(newStack, 0);
        return new Stack<ILExpr>(newStack);
    }

    public static SMFrame CreateInitial(MethodProcessor mp)
    {
        return new SMFrame(mp, null, new Stack<ILExpr>(), (ILInstr.Instr)mp.GetBeginInstr());
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

    public void StopBranching()
    {
        _mp.Successors.TryAdd(ILFirst, []);
    }

    private void ContinueTo(ILInstr instr)
    {
        StopBranching();
        _mp.Successors[ILFirst].Insert(0, instr.idx);
        if (_mp.TacBlocks.ContainsKey(instr.idx)) return;
        _mp.TacBlocks.Add(instr.idx, new SMFrame(_mp, this, stackCopy(Stack), (ILInstr.Instr)instr));
        _mp.TacBlocks[instr.idx].Branch();
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
        if (_isMerging && Stack.Count == 0) return MergeStacksValues();
        return ToSingleAddr(Stack.Pop());
    }

    public void InsertExtraAssignments()
    {
        var pos = TacLines.FindIndex(l => l is ILBranchStmt);
        pos = pos == -1 ? TacLines.Count : pos;
        // TODO reorder extra assignments so that prev line does not use var from next line 
        TacLines.AddRange(ExtraAssignments);
    }

    public bool MergeStacksFrom(List<SMFrame> frames)
    {
        _preds = frames.ToDictionary(f => f, f => stackCopy(f.Stack));
        ILStmt[] copy = new ILStmt[TacLines.Count];
        TacLines.CopyTo(copy, 0);
        _isMerging = true;
        TacLines.Clear();
        Stack.Clear();
        CurInstr = _firstInstr;
        this.Branch();
        return copy.SequenceEqual(TacLines);
    }

    private ILExpr MergeStacksValues()
    {
        List<(SMFrame, ILExpr)> values = _preds.Keys.Select(s => (s, ToSingleAddr(s.Stack.Pop()))).ToList();
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