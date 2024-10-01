namespace TACBuilder.ILMeta;

public interface CacheableMeta
{
    // public int MetadataToken { get; }
}

public class StringMeta(string value) : CacheableMeta
{
    public string Value => value;

    public override string ToString()
    {
        return Value;
    }
}

public class SignatureMeta(byte[] value) : CacheableMeta
{
    public byte[] Value => value;
}
