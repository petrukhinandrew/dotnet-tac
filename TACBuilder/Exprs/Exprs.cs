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

public class IlBinaryOperation : IlExpr
{
    private readonly IlExpr _lhs;
    private readonly IlExpr _rhs;
    private readonly string _op;

    public IlBinaryOperation(IlExpr lhs, IlExpr rhs, string op = " binop ")
    {
        _lhs = lhs;
        _rhs = rhs;
        _op = op;
        if (_lhs.Type == null)
        {
            Console.WriteLine("kek");
        }

        Type = lhs.Type;
    }

    public IlType Type { get; }

    public IlExpr Lhs => _lhs;
    public IlExpr Rhs => _rhs;
    public new string ToString() => $"{Lhs.ToString()} {_op} {Rhs.ToString()}";
}

public class IlNewExpr(IlType type) : IlExpr
{
    public IlType Type => type;
    public override string ToString()
    {
        return "new " + Type;
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

public class IlNewArrayExpr(IlArrayType type, IlExpr size) : IlExpr
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
    public IlType Type => (arrRef.Type as IlArrayType)?.ElementType ??
                          throw new Exception($"not an array type: {arrRef} with {arrRef.Type}");

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
    public IlType Type => IlInstanceBuilder.GetType(typeof(int));

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

public class IlConvCastExpr(IlType targetType, IlExpr value) : IlCastExpr(targetType, value)
{
}

public class IlBoxExpr(IlType targetType, IlExpr value) : IlCastExpr(targetType, value)
{
}

public class IlUnboxExpr(IlType targetType, IlExpr value) : IlCastExpr(targetType, value)
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

public interface ILDerefExpr : IlValue
{
    public IlExpr Value { get; }
}

public abstract class PointerExprTypeResolver
{
    public static ILDerefExpr Deref(IlExpr instance, IlType? expectedType = null)
    {
        return instance.Type.IsManaged switch
        {
            true => new IlManagedDeref(instance),

            _ => new IlUnmanagedDeref(instance, expectedType!)
        };
    }
}

public class IlManagedRef(IlExpr value) : ILRefExpr
{
    public IlExpr Value => value;

    // TODO
    public IlType Type => new IlManagedReference(value.Type.Type.MakeByRefType());

    public override string ToString()
    {
        return "&" + Value.ToString();
    }
}

public class IlUnmanagedRef(IlExpr value) : ILRefExpr
{
    public IlExpr Value => value;

    // TODO
    public IlType Type => new IlPointerType(value.Type.Type.MakePointerType());

    public override string ToString()
    {
        return "&" + Value.ToString();
    }
}

public class IlManagedDeref(IlExpr byRefVal) : ILDerefExpr
{
    public IlExpr Value => byRefVal;
    public IlType Type => ((IlManagedReference)byRefVal.Type).ReferencedType;

    public override string ToString()
    {
        return "*" + byRefVal.ToString();
    }
}

public class IlUnmanagedDeref : ILDerefExpr
{
    public IlUnmanagedDeref(IlExpr pointedVal, IlType expectedType)
    {
        Value = pointedVal;
        if (Value.Type is IlPrimitiveType)
        {
            Type = expectedType;
        }
        else if (Value.Type is IlPointerType pointerType) Type = pointerType.PointedType;
        else throw new Exception($"unexpected pointer type: {Value.Type}");
    }

    public IlExpr Value { get; }
    public IlType Type { get; }

    public override string ToString()
    {
        return "*" + Value.ToString();
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
