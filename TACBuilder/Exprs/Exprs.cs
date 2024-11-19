using TACBuilder.Exprs;
using TACBuilder.ILReflection;
using TACBuilder.Utils;

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

public class IlSizeOfExpr(IlType type) : IlExpr
{
    public IlType Type => IlInstanceBuilder.GetType(typeof(int));
    public IlType Arg => type;

    public override string ToString()
    {
        return "sizeof " + Arg.ToString();
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

public class IlArgListRef(IlMethod method) : IlConstant
{
    public IlType Type => IlInstanceBuilder.GetType(typeof(RuntimeArgumentHandle));
}