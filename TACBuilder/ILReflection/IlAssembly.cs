using System.Reflection;
using Microsoft.Extensions.Logging;

namespace TACBuilder.ILReflection;

public class IlAssembly(Assembly assembly) : IlCacheable
{
    private readonly Assembly _assembly = assembly;
    public string? Name { get; private set; }
    public string? Location { get; private set; }
    public HashSet<IlType> Types { get; } = new();
    public List<IlAssembly> ReferencedAssemblies { get; } = new();
    public new bool IsConstructed = false;
    public int MetadataToken => assembly.GetHashCode();
    public override void Construct()
    {
        Name = _assembly.GetName().FullName;
        Location = _assembly.Location;
        Logger.LogInformation("Constructed {Name}", Name);
        if (IlInstanceBuilder.AssemblyFilters.Any(f => !f(_assembly))) return;

        var types = _assembly.GetTypes().Where(t => t.IsGenericTypeDefinition || !t.IsGenericType);
        foreach (var type in types)
        {
            Types.Add(IlInstanceBuilder.GetType(type));
        }

        var asms = _assembly.GetReferencedAssemblies();
        foreach (var asm in asms)
        {
            ReferencedAssemblies.Add(IlInstanceBuilder.GetAssembly(asm));
        }

        IsConstructed = true;
    }

    internal void EnsureTypeAttached(IlType ilType)
    {
        Types.Add(ilType);
    }
}
