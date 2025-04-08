using TACBuilder.ILReflection;

namespace TACBuilder.Exprs;

public interface IlRefExpr : IlSimpleValue
{
    public IlExpr Value { get; }
}

public interface IlDerefExpr : IlSimpleValue
{
    public IlExpr Value { get; }
}

public abstract class PointerExprTypeResolver
{
    public static IlDerefExpr Deref(IlExpr instance, IlType? expectedType = null)
    {
        return instance.Type.IsManaged switch
        {
            true => new IlManagedDeref(instance),

            _ => new IlUnmanagedDeref(instance, expectedType!)
        };
    }
}

public class IlManagedRef(IlExpr value) : IlRefExpr
{
    public IlExpr Value => value;

    public IlType Type => value.Type.MakeByRefType();

    public override string ToString()
    {
        return "&" + Value.ToString();
    }
}

public class IlUnmanagedRef(IlExpr value) : IlRefExpr, IlComplexValue
{
    public IlExpr Value => value;

    public IlType Type => value.Type.MakePointerType();

    public override string ToString()
    {
        return "&" + Value.ToString();
    }
}

public class IlManagedDeref(IlExpr byRefVal) : IlDerefExpr
{
    public IlExpr Value => byRefVal;

    public IlType Type => byRefVal.Type is IlPointerType managedRef
        ? managedRef.TargetType
        : throw new Exception("managed deref got type " + byRefVal.Type);

    public override string ToString()
    {
        return "*" + byRefVal.ToString();
    }
}

public class IlUnmanagedDeref : IlDerefExpr, IlComplexValue
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