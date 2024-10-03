using Microsoft.Extensions.Logging;

namespace TACBuilder.ILMeta;

public abstract class CacheableMeta
{
    protected static readonly ILogger Logger = LoggerFactory.Create(builder => builder.AddConsole())
        .CreateLogger("Meta");

    public bool IsConstructed { get; private set; } = false;
    public abstract void Construct();
}

public class StringMeta(string value) : CacheableMeta
{
    public string Value => value;
    public new bool IsConstructed = true;

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
    public new bool IsConstructed = true;

    public override void Construct()
    {
    }
}
