using System.Reflection;

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

class ILMethod(MethodBase mb, ILType retType, string declType, string name, int argCount) : ILExpr
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
        ILMethod method = new ILMethod(mb, ilRetType, mb.DeclaringType?.FullName ?? "", mb.Name, paramCount);

        foreach (var t in mb.GetGenericArguments())
        {
            method.GenericArgs.Add(TypeSolver.Resolve(t));
        }
        return method;
    }
    public void LoadArgs(Stack<ILExpr> stack)
    {
        for (int i = _argCount - 1; i >= 0; i--)
        {
            Args.Add(stack.Pop());
        }
        if (_methodBase.CallingConvention.HasFlag(CallingConventions.HasThis))
            Receiver = stack.Pop();
    }
    public bool IsGeneric => GenericArgs.Count > 0;
    public List<ILType> GenericArgs = new List<ILType>();
    public ILType ReturnType = retType;
    public ILExpr Receiver = new ILNullValue();
    public string DeclaringType = declType;
    public string Name = name;
    private int _argCount = argCount;
    public List<ILExpr> Args = new List<ILExpr>();
    private MethodBase _methodBase = mb;
    public ILType Type => ReturnType;

    public override string ToString()
    {
        string genericExtra = IsGeneric ? string.Format("<{0}>", string.Join(", ", GenericArgs.Select(a => a.ToString()))) : "";
        if (Receiver is ILNullValue)
            return string.Format("{0} {1}{3}({2})", ReturnType.ToString(), Name, string.Join(", ", Args.Select(p => p.ToString())), genericExtra);
        return string.Format("{0} {1}.{2}{4}({3})", ReturnType.ToString(), Receiver.ToString(), Name, string.Join(", ", Args.Select(p => p.ToString())), genericExtra);
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

class ILField(ILType type, string declType, string name, bool isStatic) : ILLValue
{
    public static ILField Static(FieldInfo f)
    {
        return new ILField(TypeSolver.Resolve(f.FieldType), f.DeclaringType?.FullName ?? "", f.Name, true);
    }
    public static ILField Instance(FieldInfo f, ILExpr inst)
    {
        ILField field = new ILField(TypeSolver.Resolve(f.FieldType), f.DeclaringType?.FullName ?? "", f.Name, false)
        {
            Receiver = inst
        };
        return field;
    }
    public string DeclaringType = declType;
    public string Name = name;
    public bool IsStatic = isStatic;
    public ILExpr? Receiver;
    public ILType Type => type;
    public override string ToString()
    {
        if (!IsStatic && Receiver == null) throw new Exception("instance field with null receiver");
        return IsStatic switch
        {
            true => string.Format("{0}.{1}", DeclaringType, Name),
            false => string.Format("{0}.{1}", Receiver!.ToString(), Name)
        };
    }
}