using Microsoft.Extensions.Logging;

namespace TACBuilder.ILReflection;


public abstract class IlCacheable
{
    protected static readonly ILogger Logger = LoggerFactory
        .Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.None))
        .CreateLogger("Meta");

    public bool IsConstructed { get; private set; } = false;
    public abstract void Construct();
}

public class IlString(string value) : IlCacheable
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

    public override int GetHashCode()
    {
        return value.GetHashCode();
    }
}
