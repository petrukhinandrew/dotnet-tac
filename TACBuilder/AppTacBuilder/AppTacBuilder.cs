using System.Reflection;
using TACBuilder.ILMeta;

namespace Usvm.TACBuilder;

public class AppTacBuilder
{
    private List<AssemblyMeta> _assemblies;

    public AppTacBuilder(string rootAssemblyPath)
    {
        var rootAssembly = AssemblyMeta.FromPath(rootAssemblyPath);
        // var referencedAssemblies = rootAssembly.GetRefAssemblies();
        // referencedAssemblies.Add(rootAssembly);
        _assemblies = AssemblyMeta.CachedAssemblies.GetAll();
    }

    public void Resolve()
    {
        foreach (var asm in _assemblies)
        {
            asm.Resolve();
        }
    }
}
