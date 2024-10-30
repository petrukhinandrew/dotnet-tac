using TACBuilder.Exprs;
using TACBuilder.ILReflection;

namespace TACBuilder.ILTAC.TypeSystem;

// impl 86-87
public class IlUnaryOperation(IlExpr operand) : IlExpr
{
    public IlExpr Operand => operand;

    public IlType Type => operand.Type;

    public new string ToString()
    {
        return "unOp " + operand.ToString();
    }
}

public class IlBinaryOperation(IlExpr lhs, IlExpr rhs, string op = " binop ") : IlExpr
{
    public IlType Type => lhs.Type;

    public IlExpr Lhs => lhs;
    public IlExpr Rhs => rhs;
    public new string ToString() => $"{Lhs.ToString()} {op} {Rhs.ToString()}";
}

public class IlInitExpr(IlType type) : IlExpr
{
    public IlType Type => type;

    public override string ToString()
    {
        return $"init {Type}";
    }
}

// TODO separate from ctor call
public class IlNewExpr(IlType type, IlExpr[] args) : IlExpr
{
    public IlType Type => type;
    public IlExpr[] Args = args;

    public override string ToString()
    {
        return "new " + Type + " (" + string.Join(", ", Args.Select(a => a.ToString())) + ")";
    }
}

public class IlSizeOfExpr(IlType type) : IlExpr
{
    public IlType Type => IlInstanceBuilder.GetType(typeof(int));
    public IlType Arg => type;

    public override string ToString()
    {
        return "sizeof " + Arg.ToString();
    }
}

public class IlNewArrayExpr(IlType type, IlExpr size) : IlExpr
{
    public IlType Type => type;
    public IlExpr Size => size;

    public override string ToString()
    {
        return "new " + Type + "[" + Size.ToString() + "]";
    }
}

public class IlFieldAccess(IlField field, IlExpr? instance = null) : IlValue
{
    public IlField Field => field;
    public IlType DeclaringType => field.DeclaringType;
    public string Name => field.Name;
    public bool IsStatic => field.IsStatic;
    public IlExpr? Receiver => instance;
    public IlType Type => field.Type;

    public override string ToString()
    {
        if (!IsStatic && Receiver == null) throw new Exception("instance ilField with null receiver");
        return IsStatic switch
        {
            true => $"{DeclaringType.Name}.{Name}",
            false => $"{Receiver!.ToString()}.{Name}"
        };
    }

    public override bool Equals(object? obj)
    {
        return obj is IlFieldAccess f && Field == f.Field;
    }

    public override int GetHashCode()
    {
        return Field.GetHashCode();
    }
}

public class IlArrayAccess(IlExpr arrRef, IlExpr idx) : IlValue
{
    public IlType Type => arrRef.Type;
    public IlExpr Array => arrRef;
    public IlExpr Index => idx;

    public override string ToString()
    {
        return arrRef.ToString() + "[" + Index.ToString() + "]";
    }
}

public class IlArrayLength(IlExpr array) : IlExpr
{
    public IlExpr Array => array;
    public IlType Type { get; } = new(typeof(int));

    public override string ToString()
    {
        return Array.ToString() + ".Length";
    }
}

public abstract class IlCastExpr(IlType targetType, IlExpr target) : IlExpr
{
    public IlType Type => targetType;
    public IlExpr Target => target;

    public override string ToString()
    {
        return $"({Type}) {Target.ToString()}";
    }
}

public class IlConvExpr(IlType targetType, IlExpr value) : IlCastExpr(targetType, value)
{
}

public class IlBoxExpr(IlValue value) : IlCastExpr(IlInstanceBuilder.GetType(typeof(object)), value)
{
}

public class IlUnboxExpr(IlType targetType, IlExpr value) : IlCastExpr(targetType, value)
{
}

public class IlCastClassExpr(IlType targetType, IlExpr value) : IlCastExpr(targetType, value)
{
}

public class IlIsInstExpr(IlType targetType, IlExpr value) : IlCastExpr(targetType, value)
{
    public override string ToString()
    {
        return $"{Target.ToString()} as {Type}";
    }
}

public interface ILRefExpr : IlValue
{
    public IlExpr Value { get; }
}

public interface ILDerefExpr : IlValue;

public abstract class PointerExprTypeResolver
{
    public static ILDerefExpr DerefAs(IlExpr instance, IlType type)
    {
        return instance.Type.IsManaged switch
        {
            true => new IlManagedDeref(instance, type),

            _ => new IlUnmanagedDeref(instance, type)
        };
    }
}

public class IlManagedRef(IlExpr value) : ILRefExpr
{
    public IlExpr Value => value;

    public IlType Type => value.Type; //new ILManagedPointer(Value.Type.ReflectedType, Value.Type);

    public override string ToString()
    {
        return "&" + Value.ToString();
    }
}
// TODO check &int64 |-> int* ~> *v

public class IlUnmanagedRef(IlExpr value) : ILRefExpr
{
    public IlExpr Value => value;

    public IlType Type => value.Type; //new ILUnmanagedPointer(Value.Type.ReflectedType, Value.Type);

    public override string ToString()
    {
        return "&" + Value.ToString();
    }
}

public class IlManagedDeref(IlExpr byRefVal, IlType resType) : ILDerefExpr
{
    public IlExpr Value => byRefVal;
    public IlType Type => resType;

    public override string ToString()
    {
        return "*" + byRefVal.ToString();
    }
}

public class IlUnmanagedDeref(IlExpr pointedVal, IlType resType) : ILDerefExpr
{
    public IlExpr Value => pointedVal;
    public IlType Type => resType;

    public override string ToString()
    {
        return "*" + pointedVal.ToString();
    }
}

public class IlStackAlloc(IlExpr size) : IlExpr
{
    public IlType Type =>
        IlInstanceBuilder
            .GetType(typeof(nint)); //new ILUnmanagedPointer(Array.Empty<byte>().GetType(), new Type(typeof(byte)));

    public IlExpr Size => size;

    public override string ToString()
    {
        return "stackalloc " + Size.ToString();
    }
}
