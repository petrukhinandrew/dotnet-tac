using System.Reflection;

namespace TACBuilder.ILMeta;

public class AssemblyMeta(Assembly assembly) : CacheableMeta
{
    public List<TypeMeta>? Types { get; private set; }

    public override void Construct()
    {
        Types = assembly.GetTypes().Where(t => t.IsGenericTypeDefinition || !t.IsGenericType).Select(MetaCache.GetType).ToList();
    }
}
