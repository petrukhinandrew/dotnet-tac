using System.Reflection;
using System.Runtime.Loader;
using TACBuilder.ILMeta.ILBodyParser;

namespace TACBuilder.ILMeta;

public partial class AssemblyMeta
{
    private Assembly _assembly;
    private Dictionary<int, ModuleCache> _moduleCaches;
    private List<AssemblyMeta> _refAssemblies;

    private AssemblyMeta(Assembly assembly)
    {
        _assembly = assembly;
        _moduleCaches = assembly.GetLoadedModules().ToDictionary(m => m.MetadataToken, m => new ModuleCache(this, m));
        _refAssemblies = LoadReferencedAssemblies();
        Types = CollectTypes(assembly);
    }

    public static AssemblyMeta FromPath(string assemblyPath)
    {
        return CachedAssemblies.Get(assemblyPath);
    }

    public static AssemblyMeta FromName(AssemblyName assemblyName)
    {
        return CachedAssemblies.Get(assemblyName);
    }

    public List<AssemblyMeta> GetRefAssemblies() => _refAssemblies;

    private List<AssemblyMeta> LoadReferencedAssemblies()
    {
        foreach (var refAssemblyName in _assembly.GetReferencedAssemblies())
        {
            CachedAssemblies.Get(refAssemblyName);
        }

        return CachedAssemblies.GetAll();
    }

    public List<TypeMeta> Types { get; }

    private List<TypeMeta> CollectTypes(Assembly asm)
    {

        var types = new List<TypeMeta>();
        foreach (var mod in asm.GetModules())
        {
            foreach (var t in mod.GetTypes())
            {
                var cache = GetCorrespondingModuleCache(t.Module.MetadataToken);
                if ((t.IsGenericType && !t.IsGenericTypeDefinition) || t.FullName is null) continue;
                types.Add(cache.PutType(new TypeMeta(this, t)));
            }
        }

        return types.Distinct().ToList();
    }

    public ModuleCache GetCorrespondingModuleCache(int moduleToken)
    {
        return _moduleCaches[moduleToken] ?? throw new Exception("no ModuleCache found");
    }

    public void Resolve()
    {
        foreach (var type in Types)
        {
            type.Resolve();
        }
    }
}
