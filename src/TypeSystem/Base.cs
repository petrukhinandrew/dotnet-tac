using System.Reflection;
using System.Runtime.Versioning;

namespace Usvm.IL.TypeSystem;

public interface ILType { }

public interface ILExpr
{
    ILType Type { get; }
    public string ToString();
}

interface ILValue : ILExpr { }
interface ILLValue : ILValue { }
class ILNullValue : ILValue
{
    private static ILType _instance = new ILNull();
    public ILType Type => _instance;

    public override string ToString() => "null";
    public override bool Equals(object? obj)
    {
        return obj != null && obj is ILNullValue;
    }
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
class ILVarArgValue(string methodName) : ILValue
{
    private static ILType _instance = new ILHandleRef();
    public ILType Type => _instance;
    public string Method = methodName;
    public override string ToString() => Method + ".__arglist";

}
class ILLocal(ILType type, string name) : ILLValue
{
    public ILType Type => type;

    public new string ToString() => name;

}

class ILObjectLiteral(ILType type, object? obj) : ILValue
{
    public object? Object = obj;

    public ILType Type => type;

    public override string ToString()
    {
        return Type.ToString() + " obj";
    }
}

class ILLiteral(ILType type, string value) : ILValue
{
    public ILType Type => type;
    public new string ToString() => value;

}

class ILMethod(ILType retType, string declType, string name, int argCount, ILExpr[] args) : ILValue
{
    public static ILMethod FromMethodBase(MethodBase mb)
    {
        int paramCount = mb.GetParameters().Where(p => !p.IsRetval).Count();

        Type retType = typeof(void);
        if (mb is MethodInfo methodInfo)
        {
            retType = methodInfo.ReturnType;
        }
        ILType ilRetType = TypeSolver.Resolve(retType);
        return new ILMethod(ilRetType, mb.DeclaringType?.FullName ?? "", mb.Name, paramCount, new ILExpr[paramCount])
        {
            _methodBase = mb
        };
    }
    public void LoadArgs(Stack<ILExpr> stack)
    {
        for (int i = _argCount - 1; i >= 0; i--)
        {
            Args[i] = stack.Pop();
        }
        if (_methodBase.CallingConvention.HasFlag(CallingConventions.HasThis))
            Receiver = stack.Pop();
    }
    public ILType ReturnType = retType;
    public ILExpr Receiver = new ILNullValue();
    public string DeclaringType = declType;
    public string Name = name;
    private int _argCount = argCount;
    public ILExpr[] Args = args;
    private MethodBase _methodBase;
    public ILType Type => throw new NotImplementedException();

    // TODO declaringClass
    // TODO handle constructors 
    public override string ToString()
    {
        if (Receiver is ILNullValue)
            return string.Format("{0} {1}({2})", ReturnType.ToString(), Name, string.Join(", ", Args.Select(p => p.ToString())));
        return string.Format("{0} {1}.{2}({3})", ReturnType.ToString(), Receiver.ToString(), Name, string.Join(", ", Args.Select(p => p.ToString())));
    }
    public bool IsInitializeArray()
    {
        return DeclaringType == "System.Runtime.CompilerServices.RuntimeHelpers" && Name == "InitializeArray";
    }
    public bool Returns()
    {
        return ReturnType is not ILVoid && ReturnType is not ILNull;
    }
}