using System.Reflection;
using Usvm.IL.Parser;
using Usvm.IL.TypeSystem;

namespace Usvm.IL.TACBuilder;

class SMFrame(MethodProcessor proc, int initl, Stack<ILExpr> stack, ILInstr.Instr instr)
{
    public Stack<ILExpr> Stack = stack;
    public int Initl = initl;
    public List<ILStmt> TacLines = new List<ILStmt>();
    public ILInstr.Instr CurInstr = instr;
    private MethodProcessor _mp = proc;
    private ILInstr? condSucc;
    private ILInstr? uncondSucc;

    public static SMFrame CreateInitial(MethodProcessor mp)
    {
        return new SMFrame(mp, 0, new Stack<ILExpr>(), (ILInstr.Instr)mp.GetBeginInstr());
    }
    private void ContinueTo(ILInstr instr)
    {
        if (_mp.TacBlocks.ContainsKey(instr.idx)) return;
        ILExpr[] stackCopy = new ILExpr[Stack.Count];
        Stack.CopyTo(stackCopy, 0);
        _mp.TacBlocks.Add(instr.idx, new SMFrame(_mp, instr.idx, new Stack<ILExpr>(stackCopy), (ILInstr.Instr)instr));
        _mp.Successors.TryAdd(Initl, []);
        _mp.Successors[Initl].Add(instr.idx);
        _mp.TacBlocks[instr.idx].Branch();
    }
    public void ContinueBranchingTo(ILInstr uncond, ILInstr? cond)
    {
        uncondSucc = uncond;
        condSucc = cond;
        ContinueTo(uncond);
        if (cond != null) ContinueTo(cond);
    }
    // public void StopBranching(ILInstr target)
    // {
    //     NewLine(new ILGotoStmt(StmtIndex, target.idx));
    // }
    public bool IsLeader(ILInstr instr)
    {
        return _mp.Leaders.Select(l => l.idx).Contains(instr.idx);
    }
    public int StmtIndex
    {
        get
        {
            return _mp.StmtIndex;
        }
    }
    public List<ILLocal> Locals
    {
        get
        {
            return _mp.Locals;
        }
    }
    public List<ILLocal> Params
    {
        get
        {
            return _mp.Params;
        }
    }
    public List<ILExpr> Temps
    {
        get
        {
            return _mp.Temps;
        }
    }
    public List<ILExpr> Errs
    {
        get
        {
            return _mp.Errs;
        }
    }
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
        return ToSingleAddr(Stack.Pop());
    }
    public void PushLiteral<T>(T value)
    {
        ILLiteral lit = new ILLiteral(TypeSolver.Resolve(typeof(T)), value?.ToString() ?? "");
        Stack.Push(lit);
    }
    private ILExpr ToSingleAddr(ILExpr val)
    {
        if (val is not ILValue)
        {
            ILLocal tmp = GetNewTemp(val.Type, val);
            NewLine(new ILAssignStmt(StmtIndex, tmp, val));
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
        return obj != null && obj is SMFrame f && Initl == f.Initl;
    }
    public override int GetHashCode()
    {
        return Initl;
    }
    public override string ToString()
    {
        return Initl.ToString();
    }
}