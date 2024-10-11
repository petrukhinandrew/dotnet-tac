using System.Reflection;
using Microsoft.Extensions.Logging;

namespace TACBuilder.ILReflection;

public class ILAssembly(Assembly assembly) : ILCacheable
{
    private readonly Assembly _assembly = assembly;
    public string? Name { get; private set; }
    public string? Location { get; private set; }
    public HashSet<ILType> Types { get; } = new();
    public List<ILAssembly> ReferencedAssemblies { get; } = new();
    public new bool IsConstructed = false;

    public override void Construct()
    {
        Name = _assembly.GetName().FullName;
        Location = _assembly.Location;
        Logger.LogInformation("Constructed {Name}", Name);
        if (ILInstanceBuilder.AssemblyFilters.Any(f => !f(_assembly))) return;

        var types = _assembly.GetTypes().Where(t => t.IsGenericTypeDefinition || !t.IsGenericType);
        foreach (var type in types)
        {
            Types.Add(ILInstanceBuilder.GetType(type));
        }

        var asms = _assembly.GetReferencedAssemblies();
        foreach (var asm in asms)
        {
            ReferencedAssemblies.Add(ILInstanceBuilder.GetAssembly(asm));
        }

        IsConstructed = true;
    }

    internal void EnsureTypeAttached(ILType ilType)
    {
        Types.Add(ilType);
    }
}
