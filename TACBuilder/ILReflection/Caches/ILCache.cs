using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TACBuilder.ILReflection;

internal class ILCache
{
    public readonly Dictionary<Assembly, ILAssembly> _assemblies = new();
    public readonly Dictionary<Type, ILType> _types = new();
    public readonly Dictionary<MethodBase, ILMethod> _methods = new();
    public readonly Dictionary<FieldInfo, ILField> _fields = new();
    public readonly Dictionary<CustomAttributeData, ILAttribute> _attributes = new();

    public void AddAssembly(Assembly key, ILAssembly value)
    {
        _assemblies.Add(key, value);
    }

    public bool TryGetAssembly(Assembly key, [MaybeNullWhen(false)] out ILAssembly value)
    {
        return _assemblies.TryGetValue(key, out value);
    }

    public List<ILAssembly> GetAssemblies()
    {
        return _assemblies.Values.ToList();
    }

    public void AddType(Type key, ILType value)
    {
        _types[key] = value;
    }

    public bool TryGetType(Type key, [MaybeNullWhen(false)] out ILType value)
    {
        return _types.TryGetValue(key, out value);
    }

    public void AddMethod(MethodBase key, ILMethod value)
    {
        _methods[key] = value;
    }

    public bool TryGetMethod(MethodBase key, [MaybeNullWhen(false)] out ILMethod value)
    {
        return _methods.TryGetValue(key, out value);
    }

    public void AddField(FieldInfo key, ILField value)
    {
        _fields[key] = value;
    }

    public bool TryGetField(FieldInfo key, [MaybeNullWhen(false)] out ILField value)
    {
        return _fields.TryGetValue(key, out value);
    }

    public void AddAttribute(CustomAttributeData key, ILAttribute value)
    {
        _attributes[key] = value;
    }

    public bool TryGetAttribute(CustomAttributeData key, [MaybeNullWhen(false)] out ILAttribute value)
    {
        return _attributes.TryGetValue(key, out value);
    }
}
