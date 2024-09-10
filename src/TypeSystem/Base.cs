using System.Reflection;

namespace Usvm.IL.TypeSystem;

public interface ILType
{
}

public interface ILExpr
{
    ILType Type { get; }
    public string ToString();
}

interface ILValue : ILExpr
{
}

interface ILLValue : ILValue
{
}

class ILNullValue : ILValue
{
    private static ILType _instance = new ILNull();
    public ILType Type => _instance;

    public override string ToString() => "null";

    public override bool Equals(object? obj)
    {
        return obj is ILNullValue;
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

class ILLocal(ILType type, string name, bool merged = false) : ILLValue
{
    public ILType Type => type;

    public new string ToString() => name;

    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return obj is ILLocal loc && loc.ToString() == ToString();
    }
}

class ILObjectLiteral(ILType type, object? obj) : ILValue
{
    public object? Object = obj;

    public ILType Type => type;

    public override string ToString()
    {
        return Type + " obj";
    }
}

class ILLiteral(ILType type, string value) : ILValue
{
    public ILType Type => type;

    public new string ToString() => type switch
    {
        ILString => $"\"{value}\"",
        _ => value
    };

    public override bool Equals(object? obj)
    {
        return obj is ILLiteral literal && literal.ToString() == ToString();
    }

    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }
}

class ILMethod(MethodBase mb, ILType retType, string declType, string name, int argCount, int token) : ILExpr
{
    public static ILMethod FromMethodBase(MethodBase mb)
    {
        int paramCount = mb.GetParameters().Count(p => !p.IsRetval);
        Type retType = typeof(void);
        if (mb is MethodInfo methodInfo)
        {
            retType = methodInfo.ReturnType;
        }

        ILType ilRetType = TypeSolver.Resolve(retType);
        ILMethod method = new ILMethod(mb, ilRetType, mb.DeclaringType?.FullName ?? "", mb.Name, paramCount,
            mb.GetMetadataToken());

        foreach (var t in mb.GetGenericArguments())
        {
            method.GenericArgs.Add(TypeSolver.Resolve(t));
        }

        return method;
    }

    private int _metadataToken = token;

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
    public List<ILType> GenericArgs = new();
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
        string genericExtra =
            IsGeneric ? $"<{string.Join(", ", GenericArgs.Select(a => a.ToString()))}>" : "";
        if (Receiver is ILNullValue)
            return string.Format("{0} {1}{3}({2})", ReturnType.ToString(), Name,
                string.Join(", ", Args.Select(p => p.ToString())), genericExtra);
        return string.Format("{0} {1}.{2}{4}({3})", ReturnType.ToString(), Receiver.ToString(), Name,
            string.Join(", ", Args.Select(p => p.ToString())), genericExtra);
    }

    public bool IsInitializeArray()
    {
        return DeclaringType == "System.Runtime.CompilerServices.RuntimeHelpers" && Name == "InitializeArray";
    }

    public bool Returns()
    {
        return ReturnType is not ILVoid && ReturnType is not ILNull;
    }

    public override bool Equals(object? obj)
    {
        return obj is ILMethod m && m._metadataToken == _metadataToken;
    }

    public override int GetHashCode()
    {
        return _metadataToken.GetHashCode();
    }
}

class ILField(ILType type, string declType, string name, bool isStatic, int token) : ILLValue
{
    public static ILField Static(FieldInfo f)
    {
        return new ILField(TypeSolver.Resolve(f.FieldType), f.DeclaringType?.FullName ?? "", f.Name, true,
            f.GetMetadataToken());
    }

    public static ILField Instance(FieldInfo f, ILExpr inst)
    {
        ILField field = new ILField(TypeSolver.Resolve(f.FieldType), f.DeclaringType?.FullName ?? "", f.Name, false,
            f.GetMetadataToken())
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
    private int _metadataToken = token;

    public override string ToString()
    {
        if (!IsStatic && Receiver == null) throw new Exception("instance field with null receiver");
        return IsStatic switch
        {
            true => $"{DeclaringType}.{Name}",
            false => $"{Receiver!.ToString()}.{Name}"
        };
    }

    public override bool Equals(object? obj)
    {
        return obj is ILField f && f._metadataToken == _metadataToken;
    }

    public override int GetHashCode()
    {
        return _metadataToken.GetHashCode();
    }
}