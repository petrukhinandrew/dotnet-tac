using TACBuilder.Serialization;

namespace TACBuilder.ILReflection;

public class ILConstructQueue : Queue<IlCacheable>
{
    private readonly List<IlType> _freshInstances = new();
    private readonly List<IlAssembly> _builtAssemblies = new();

    public new void Enqueue(IlCacheable item)
    {
        base.Enqueue(item);
        if (item is IlAssembly assembly)
            _builtAssemblies.Add(assembly);
        if (item is IlType type)
            _freshInstances.Add(type);
    }

    public List<IlType> FreshInstances => _freshInstances;
    public List<IlAssembly> BuiltAssemblies => _builtAssemblies;

    public void DropBuiltAssemblies()
    {
        _builtAssemblies.Clear();
    }

    public void DropFreshInstances()
    {
        _freshInstances.Clear();
    }
}