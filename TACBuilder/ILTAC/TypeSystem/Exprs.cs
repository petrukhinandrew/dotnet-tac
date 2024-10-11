using TACBuilder.ILReflection;

namespace TACBuilder.ILTAC.TypeSystem;

// impl 86-87
public class ILUnaryOperation(ILExpr operand) : ILExpr
{
    public ILExpr Operand => operand;

    public ILType Type => operand.Type;

    public new string ToString()
    {
        return "unOp " + operand.ToString();
    }
}

public class ILBinaryOperation(ILExpr lhs, ILExpr rhs, string op = " binop ") : ILExpr
{
    public ILType Type => lhs.Type;

    public ILExpr Lhs => lhs;
    public ILExpr Rhs => rhs;
    public new string ToString() => lhs.ToString() + op + rhs.ToString();
}

public class ILNewDefaultExpr(ILType type) : ILExpr
{
    public ILType Type => type;

    public override string ToString()
    {
        return $"new {Type}(default)";
    }
}

public class ILNewExpr(ILType type, ILExpr[] args) : ILExpr
{
    public ILType Type => type;
    public ILExpr[] Args = args;

    public override string ToString()
    {
        return "new " + Type + " (" + string.Join(", ", Args.Select(a => a.ToString())) + ")";
    }
}

public class ILSizeOfExpr(ILType type) : ILExpr
{
    public ILType Type => new ILType(typeof(int));
    public ILType Arg => type;

    public override string ToString()
    {
        return "sizeof " + Arg.ToString();
    }
}

public class ILNewArrayExpr(ILType type, ILExpr size) : ILExpr
{
    public ILType Type => type;
    public ILExpr Size => size;

    public override string ToString()
    {
        return "new " + Type + "[" + Size.ToString() + "]";
    }
}

public class ILArrayAccess(ILExpr arrRef, ILExpr idx) : ILLValue
{
    public ILType Type => arrRef.Type;
    public ILExpr Index => idx;

    public string Name => ToString();

    public override string ToString()
    {
        return arrRef.ToString() + "[" + Index.ToString() + "]";
    }
}

public class ILArrayLength(ILExpr arr) : ILExpr
{
    private ILExpr _arr = arr;
    private ILType _type = new ILType(typeof(int));
    public ILType Type => _type;

    public override string ToString()
    {
        return _arr.ToString() + ".Length";
    }
}

public abstract class ILCastExpr(ILType targetType, ILExpr target) : ILExpr
{
    protected ILType _targetType = targetType;
    protected ILExpr _target = target;
    public ILType Type => _targetType;

    public override string ToString()
    {
        return $"({_targetType}) {_target.ToString()}";
    }
}

public class ILConvExpr(ILType targetType, ILExpr value) : ILCastExpr(targetType, value)
{
}

public class ILBoxExpr(ILValue value) : ILCastExpr(new ILType(typeof(object)), value)
{
}

public class ILUnboxExpr(ILType targetType, ILExpr value) : ILCastExpr(targetType, value)
{
}

public class ILCastClassExpr(ILType targetType, ILExpr value) : ILCastExpr(targetType, value)
{
}

public class ILCondCastExpr(ILType targetType, ILExpr value) : ILCastExpr(targetType, value)
{
    public override string ToString()
    {
        return string.Format("{0} as {1}", _target.ToString(), _targetType.ToString());
    }
}

public interface ILRefExpr : ILLValue
{
    public ILExpr Value { get; }
}

public interface ILDerefExpr : ILLValue
{
}

public class PointerExprTypeResolver
{
    public static ILDerefExpr DerefAs(ILExpr instance, ILType type)
    {
        return instance.Type.IsManaged switch
        {
            true => new ILManagedDeref(instance, type),

            _ => new ILUnmanagedDeref(instance, type)
        };
    }
}

public class ILManagedRef(ILExpr value) : ILRefExpr
{
    public ILExpr Value => value;

    public ILType Type => value.Type; //new ILManagedPointer(Value.Type.ReflectedType, Value.Type);

    public override string ToString()
    {
        return "&" + Value.ToString();
    }
}
// TODO check &int64 |-> int* ~> *v

public class ILUnmanagedRef(ILExpr value) : ILRefExpr
{
    public ILExpr Value => value;

    public ILType Type => value.Type; //new ILUnmanagedPointer(Value.Type.ReflectedType, Value.Type);

    public override string ToString()
    {
        return "&" + Value.ToString();
    }
}

public class ILManagedDeref(ILExpr byRefVal, ILType resType) : ILDerefExpr
{
    private ILExpr Value = byRefVal;
    public ILType Type => resType;

    public override string ToString()
    {
        return "*" + Value.ToString();
    }
}

public class ILUnmanagedDeref(ILExpr pointedVal, ILType resType) : ILDerefExpr
{
    private ILExpr Value = pointedVal;
    public ILType Type => resType;

    public override string ToString()
    {
        return "*" + Value.ToString();
    }
}

public class ILStackAlloc(ILExpr size) : ILExpr
{
    public ILType Type =>
        new ILType(typeof(nint)); //new ILUnmanagedPointer(Array.Empty<byte>().GetType(), new ILType(typeof(byte)));

    private readonly ILExpr _size = size;

    public override string ToString()
    {
        return "stackalloc " + _size.ToString();
    }
}
