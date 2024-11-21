using TACBuilder.ILReflection;

namespace TACBuilder.Exprs;

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
    public IlType Type => new IlPointerType(value.Type.Type);

    public override string ToString()
    {
        return "&" + Value.ToString();
    }
}

public class IlUnmanagedRef(IlExpr value) : ILRefExpr
{
    public IlExpr Value => value;

    // TODO
    public IlType Type => new IlPointerType(value.Type.Type);

    public override string ToString()
    {
        return "&" + Value.ToString();
    }
}

public class IlManagedDeref(IlExpr byRefVal) : ILDerefExpr
{
    public IlExpr Value => byRefVal;

    public IlType Type => byRefVal.Type is IlPointerType managedRef
        ? managedRef.TargetType
        : throw new Exception("pointer type expected, got " + Type);

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
        else if (Value.Type is IlPointerType pointerType) Type = pointerType.TargetType;
        else throw new Exception($"unexpected pointer type: {Value.Type}");
    }

    public IlExpr Value { get; }
    public IlType Type { get; }

    public override string ToString()
    {
        return "*" + Value.ToString();
    }
}