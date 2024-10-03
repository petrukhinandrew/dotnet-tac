using System.Reflection;
using Microsoft.Extensions.Logging;

namespace TACBuilder.ILMeta;

public class AssemblyMeta(Assembly assembly) : CacheableMeta
{
    private readonly Assembly _assembly = assembly;
    public string? Name { get; private set; }
    public string? Location { get; private set; }
    public HashSet<TypeMeta> Types { get; } = new();
    public new bool IsConstructed = false;

    public override void Construct()
    {
        Name = _assembly.GetName().FullName;
        Location = _assembly.Location;
        Logger.LogInformation("Constructed {Name}", Name);
        if (MetaBuilder.AssemblyFilters.All(f => !f(_assembly))) return;
        // TODO referenced assemblies

        var types = _assembly.GetTypes().Where(t => t.IsGenericTypeDefinition || !t.IsGenericType);
        foreach (var type in types)
        {
            Types.Add(MetaBuilder.GetType(type));
        }

        IsConstructed = true;
    }

    internal void EnsureTypeAttached(TypeMeta type)
    {
        Types.Add(type);
    }
}
