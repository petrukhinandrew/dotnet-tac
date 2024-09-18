using System.Reflection;
using System.Runtime.Loader;

namespace TACBuilder.ILMeta;

public class AssemblyMeta(Assembly assembly)
{
    internal class AssemblyLoader : AssemblyLoadContext
    {
        // TODO introduce cache
    }

    public static AssemblyMeta FromPath(string assemblyPath)
    {
        var asm = new AssemblyLoader().LoadFromAssemblyPath(assemblyPath);
        var instance = new AssemblyMeta(asm)
        {
            _isFromPath = true
        };
        return instance;
    }

    public static AssemblyMeta FromName(AssemblyName assemblyName)
    {
        var asm = new AssemblyLoader().LoadFromAssemblyName(assemblyName);
        var instance = new AssemblyMeta(asm)
        {
            _isFromName = true
        };
        return instance;
    }

    private Assembly _assembly = assembly;
    private List<TypeMeta> _types = assembly.GetTypes().Select(t => new TypeMeta(t)).ToList();

    private bool _isFromPath = false;
    public bool IsFromPath => _isFromPath;

    private bool _isFromName = false;
    public bool IsFromName => _isFromName;
}