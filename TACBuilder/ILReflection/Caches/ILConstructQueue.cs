using TACBuilder.Serialization;

namespace TACBuilder.ILReflection;

public class ILConstructQueue : Queue<IlCacheable>
{
    private readonly List<IlCacheable> _freshInstances = new();

    public new void Enqueue(IlCacheable item)
    {
        base.Enqueue(item);
        _freshInstances.Add(item);
    }

    public List<IlCacheable> FreshInstances => _freshInstances;
    public void DropFreshInstances()
    {
        _freshInstances.Clear();
    }
}
