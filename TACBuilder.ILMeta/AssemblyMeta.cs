using System.Reflection;
using TACBuilder.ILMeta.ILBodyParser;

namespace TACBuilder.ILMeta;

public class AssemblyMeta(Assembly assembly)
{
    public AssemblyMeta(string assemblyPath) : this(Assembly.LoadFrom(assemblyPath))
    {
        _isFromPath = true;
    }

    public AssemblyMeta(AssemblyName assemblyName) : this(Assembly.Load(assemblyName))
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
        // TODO asm.GetReferencedAssemblies()
        foreach (var t in asm.GetTypesChecked())
        {
            var type = t.IsGenericType ? t.GetGenericTypeDefinition() : t;
            // TODO prev line is needed? 
            if (type.FullName is null) continue;
            types.Add(new TypeMeta(type));
        }

        return types;
    }
}