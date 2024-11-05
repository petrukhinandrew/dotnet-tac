namespace TACBuilder.ILReflection;

public class IlSignature(byte[] value) : IlCacheable
{
    public static IlSignature FromByteArray(byte[] sig)
    {
        // TODO
        throw new NotImplementedException();

    }

    public byte[] Value => value;
    public IlType ReturnType { get; }
    public List<IlType> ArgumentTypes { get; } = new();
    public bool HasThis;
    public bool ExplicitThis;

    public new bool IsConstructed = true;

    public override void Construct()
    {
    }

    public override int GetHashCode()
    {
        return value.GetHashCode();
    }
}
