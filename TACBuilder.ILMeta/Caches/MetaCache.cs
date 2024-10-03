using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TACBuilder.ILMeta;

internal class MetaCache
{
    private readonly Dictionary<Assembly, AssemblyMeta> _assemblies = new();
    private readonly Dictionary<Type, TypeMeta> _types = new();
    private readonly Dictionary<MethodBase, MethodMeta> _methods = new();
    private readonly Dictionary<FieldInfo, FieldMeta> _fields = new();
    private readonly Dictionary<CustomAttributeData, AttributeMeta> _attributes = new();

    public void AddAssembly(Assembly key, AssemblyMeta value)
    {
        _assemblies.Add(key, value);
    }

    public bool TryGetAssembly(Assembly key, [MaybeNullWhen(false)] out AssemblyMeta value)
    {
        return _assemblies.TryGetValue(key, out value);
    }

    public List<AssemblyMeta> GetAssemblies()
    {
        return _assemblies.Values.ToList();
    }

    public void AddType(Type key, TypeMeta value)
    {
        _types[key] = value;
    }

    public bool TryGetType(Type key, [MaybeNullWhen(false)] out TypeMeta value)
    {
        return _types.TryGetValue(key, out value);
    }

    public void AddMethod(MethodBase key, MethodMeta value)
    {
        _methods[key] = value;
    }

    public bool TryGetMethod(MethodBase key, [MaybeNullWhen(false)] out MethodMeta value)
    {
        return _methods.TryGetValue(key, out value);
    }

    public void AddField(FieldInfo key, FieldMeta value)
    {
        _fields[key] = value;
    }

    public bool TryGetField(FieldInfo key, [MaybeNullWhen(false)] out FieldMeta value)
    {
        return _fields.TryGetValue(key, out value);
    }

    public void AddAttribute(CustomAttributeData key, AttributeMeta value)
    {
        _attributes[key] = value;
    }

    public bool TryGetAttribute(CustomAttributeData key, [MaybeNullWhen(false)] out AttributeMeta value)
    {
        return _attributes.TryGetValue(key, out value);
    }
}
