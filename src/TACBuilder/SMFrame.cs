using System.Reflection;
using Usvm.IL.Parser;
using Usvm.IL.TypeSystem;

namespace Usvm.IL.TACBuilder;

class SMFrame(MethodProcessor proc, Stack<ILExpr> stack, ILInstr.Instr instr)
{
    private ILStmt _pred;
    public Stack<ILExpr> Stack = stack;
    public ILInstr.Instr CurInstr = instr;
    private MethodProcessor _mp = proc;
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
        _mp.Successors.Add(line, new List<ILStmt>());
        _mp.Successors[_pred].Add(line);
        _pred = line;
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
}