namespace Usvm.IL.TypeSystem;

public abstract class ILPointer(ILType targetType) : ILType
{
    public ILType TargetType = targetType;
    public abstract override string ToString();
}
class ILManagedPointer(ILType targetType) : ILPointer(targetType)
{
    public override string ToString() => TargetType.ToString() + "&";
    public override bool Equals(object? obj)
    {
        return obj is ILManagedPointer pt && TargetType == pt.TargetType;
    }

    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }
}
class ILUnmanagedPointer(ILType targetType) : ILPointer(targetType)
{
    public override string ToString() => TargetType.ToString() + "*";
    public override bool Equals(object? obj)
    {
        return obj is ILUnmanagedPointer pt && TargetType == pt.TargetType;
    }

    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }
}
