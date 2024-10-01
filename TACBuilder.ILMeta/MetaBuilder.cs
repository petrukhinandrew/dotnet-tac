using System.Reflection;

namespace TACBuilder.ILMeta;

// TODO expose MetaCache isntances
public class MetaBuilder
{
    public AssemblyMeta BuildFrom(string assemblyPath)
    {
        var meta = MetaCache.GetAssembly(assemblyPath);
        MetaCache.Construct();
        return meta;
    }

    public AssemblyMeta BuildFrom(AssemblyName assemblyName)
    {
        var meta = MetaCache.GetAssembly(assemblyName);
        MetaCache.Construct();
        return meta;
    }
}
