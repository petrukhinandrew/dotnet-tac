using System.Reflection;
using System.Runtime.Loader;
using TACBuilder.ILMeta.ILBodyParser;

namespace TACBuilder.ILMeta;

public class AssemblyMeta(Assembly assembly)
{
    private class AssemblyLoader : AssemblyLoadContext
    {
        // TODO introduce cache
    }

    private static readonly AssemblyLoader _loader = new();

    public AssemblyMeta(string assemblyPath) : this(_loader.LoadFromAssemblyPath(assemblyPath))
    {
        _isFromPath = true;
    }

    public AssemblyMeta(AssemblyName assemblyName) : this(_loader.LoadFromAssemblyName(assemblyName))
    {
        _isFromName = true;
    }

    private Assembly _assembly = assembly;

    private bool _isFromPath = false;

    public bool IsFromPath => _isFromPath;

    private bool _isFromName = false;

    public bool IsFromName => _isFromName;
    public List<TypeMeta> Types { get; } = resolveTypes(assembly);

    private static List<TypeMeta> resolveTypes(Assembly asm)
    {
        var types = new List<TypeMeta>();
        foreach (var t in asm.GetTypesChecked())
        {
            var type = t.IsGenericType ? t.GetGenericTypeDefinition() : t;
            if (type is null) continue;
            types.Add(new TypeMeta(type));
        }

        return types;
    }
}