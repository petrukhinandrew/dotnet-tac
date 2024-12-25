using System.Reflection;
using Microsoft.Extensions.Logging;

namespace TACBuilder.ILReflection;

public class IlAssembly(Assembly assembly) : IlCacheable
{
    public readonly Assembly _assembly = assembly;
    public string Name { get; } = assembly.GetName().FullName;
    public string? Location { get; private set; }
    public HashSet<IlType> Types { get; } = [];
    public List<IlAssembly> ReferencedAssemblies { get; } = [];

    public new bool IsConstructed = false;
    public int MetadataToken => _assembly.GetHashCode();

    public override void Construct()
    {
        Location = _assembly.Location;

        foreach (var asm in _assembly.GetReferencedAssemblies())
        {
            ReferencedAssemblies.Add(IlInstanceBuilder.GetAssembly(asm));
        }

        foreach (var type in assembly.GetTypes())
        {
            Types.Add(IlInstanceBuilder.GetType(type));
        }

        Logger.LogInformation("Constructed {Name}", Name);
        IsConstructed = true;
    }

    internal void EnsureTypeAttached(IlType ilType)
    {
        Types.Add(ilType);
    }
}