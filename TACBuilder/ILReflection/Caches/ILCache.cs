using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TACBuilder.ILReflection;

internal class ILCache
{
    public readonly Dictionary<Assembly, IlAssembly> _assemblies = new();
    public readonly Dictionary<Type, IlType> _types = new();
    public readonly Dictionary<MethodBase, IlMethod> _methods = new();
    public readonly Dictionary<FieldInfo, IlField> _fields = new();
    public readonly Dictionary<CustomAttributeData, IlAttribute> _attributes = new();

    public void AddAssembly(Assembly key, IlAssembly value)
    {
        _assemblies.Add(key, value);
    }

    public bool TryGetAssembly(Assembly key, [MaybeNullWhen(false)] out IlAssembly value)
    {
        return _assemblies.TryGetValue(key, out value);
    }

    public List<IlAssembly> GetAssemblies()
    {
        return _assemblies.Values.ToList();
    }

    public void AddType(Type key, IlType value)
    {
        _types[key] = value;
    }

    public bool TryGetType(Type key, [MaybeNullWhen(false)] out IlType value)
    {
        return _types.TryGetValue(key, out value);
    }

    public void AddMethod(MethodBase key, IlMethod value)
    {
        _methods[key] = value;
    }

    public bool TryGetMethod(MethodBase key, [MaybeNullWhen(false)] out IlMethod value)
    {
        return _methods.TryGetValue(key, out value);
    }

    public void AddField(FieldInfo key, IlField value)
    {
        _fields[key] = value;
    }

    public bool TryGetField(FieldInfo key, [MaybeNullWhen(false)] out IlField value)
    {
        return _fields.TryGetValue(key, out value);
    }

    public void AddAttribute(CustomAttributeData key, IlAttribute value)
    {
        _attributes[key] = value;
    }

    public bool TryGetAttribute(CustomAttributeData key, [MaybeNullWhen(false)] out IlAttribute value)
    {
        return _attributes.TryGetValue(key, out value);
    }
}
