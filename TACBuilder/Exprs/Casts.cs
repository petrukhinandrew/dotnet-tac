using TACBuilder.ILReflection;

namespace TACBuilder.Exprs;

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

public class IlBoxExpr(IlType targetType, IlExpr value) : IlCastExpr(IlInstanceBuilder.GetType(typeof(object)), value)
{
    public IlType BoxedType => targetType;
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