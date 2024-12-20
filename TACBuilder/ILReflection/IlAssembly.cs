using System.Reflection;
using Microsoft.Extensions.Logging;

namespace TACBuilder.ILReflection;

public class IlAssembly(Assembly assembly) : IlCacheable
{
    public readonly Assembly _assembly = assembly;
    public string Name { get; } = assembly.GetName().FullName;
    public string? Location { get; private set; }
    public HashSet<IlType> Types { get; } = new();

    public List<IlAssembly> ReferencedAssemblies { get; } =
        assembly.GetReferencedAssemblies().Select(IlInstanceBuilder.GetAssembly).ToList();

    public new bool IsConstructed = false;
    public int MetadataToken => assembly.GetHashCode();

    public override void Construct()
    {
        Location = _assembly.Location;
        Logger.LogInformation("Constructed {Name}", Name);
        if (IlInstanceBuilder.AssemblyFilters.All(f => !f(_assembly))) return;

        // TODO CRITICAL
        var types = _assembly.GetTypes(); //.Where(t => t.IsGenericTypeDefinition || !t.IsGenericType);
        foreach (var type in types)
        {
            Types.Add(IlInstanceBuilder.GetType(type));
        }

        IsConstructed = true;
    }

    internal void EnsureTypeAttached(IlType ilType)
    {
        Types.Add(ilType);
    }
}