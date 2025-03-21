namespace TACBuilder.ILReflection;

public class ILConstructQueue : Queue<IlCacheable>
{
    private readonly List<IlType> _freshInstances = new();
    private readonly List<IlAssembly> _builtAssemblies = new();

    public new void Enqueue(IlCacheable item)
    {
        base.Enqueue(item);
        switch (item)
        {
            case IlAssembly assembly:
                _builtAssemblies.Add(assembly);
                break;
            case IlType type:
                _freshInstances.Add(type);
                break;
        }
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