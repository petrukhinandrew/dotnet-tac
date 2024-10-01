using System.Reflection;

namespace TACBuilder.ILMeta;

public abstract class CacheableMeta
{
    public abstract void Construct();
}

public class StringMeta(string value) : CacheableMeta
{
    public string Value => value;

    public override string ToString()
    {
        return Value;
    }

    public override void Construct()
    {
    }
}

public class SignatureMeta(byte[] value) : CacheableMeta
{
    public byte[] Value => value;

    public override void Construct()
    {
    }
}
