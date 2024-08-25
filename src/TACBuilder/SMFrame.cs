using System.Reflection;
using Usvm.IL.Parser;
using Usvm.IL.TypeSystem;

namespace Usvm.IL.TACBuilder;

class SMFrame(MethodProcessor proc, Stack<ILExpr> stack, ILInstr.Instr instr)
{
    public (int, int) ilRange = (instr.idx, instr.idx);
    public Stack<ILExpr> Stack = stack;
    public List<ILStmt> TacLines = new List<ILStmt>();
    public ILInstr.Instr CurInstr = instr;
    private MethodProcessor _mp = proc;
    public static SMFrame CreateInitial(MethodProcessor mp)
    {
        return new SMFrame(mp, new Stack<ILExpr>(), (ILInstr.Instr)mp.GetBeginInstr());
    }
    public static SMFrame ContinueFrom(SMFrame frame, int target)
    {
        ILExpr[] copy = new ILExpr[frame.Stack.Count];
        frame.Stack.CopyTo(copy, 0);
        return new SMFrame(frame._mp, new Stack<ILExpr>(copy), (ILInstr.Instr)frame._mp.GetILInstr(target));
    }
    public void AdvanceILRange()
    {
        ilRange.Item2 += 1;
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
    // public void 
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
}