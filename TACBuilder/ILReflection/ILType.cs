using System.Reflection;
using Microsoft.Extensions.Logging;
using TACBuilder.Utils;

namespace TACBuilder.ILReflection;

public class ILType(Type type) : ILMember(type)
{
    private readonly Type _type = type;
    public Type BaseType => _type;
    public new bool IsConstructed = false;

    private const BindingFlags BindingFlags =
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static |
        System.Reflection.BindingFlags.DeclaredOnly;

    public override void Construct()
    {
        Logger.LogInformation("Constructing {Name}", Name);
        DeclaringAssembly = ILInstanceBuilder.GetAssembly(_type.Assembly);
        if (_type.IsGenericType)
        {
            GenericArgs = _type.GetGenericArguments().Select(ILInstanceBuilder.GetType).ToList();
        }

        var attributes = _type.CustomAttributes;
        foreach (var attribute in attributes)
        {
            Attributes.Add(ILInstanceBuilder.GetAttribute(attribute));
        }

        DeclaringAssembly.EnsureTypeAttached(this);
        if (ILInstanceBuilder.TypeFilters.Any(f => !f(_type))) return;

        var fields = _type.GetFields(BindingFlags);
        foreach (var field in fields)
        {
            Fields.Add(ILInstanceBuilder.GetField(field));
        }

        var constructors = _type.GetConstructors(BindingFlags);
        var methods = _type.GetMethods(BindingFlags)
            .Where(method => method.IsGenericMethodDefinition || !method.IsGenericMethod);
        foreach (var callable in methods.Concat<MethodBase>(constructors))
        {
            Methods.Add(ILInstanceBuilder.GetMethod(callable));
        }

        IsConstructed = true;
    }

    public ILAssembly DeclaringAssembly { get; private set; }
    public List<ILAttribute> Attributes { get; } = new();
    public List<ILType> GenericArgs { get; private set; } = new();
    public HashSet<ILMethod> Methods { get; private set; } = new();
    public HashSet<ILField> Fields { get; private set; } = new();
    public new string Name => _type.Name;
    public Type Type => _type;
    public bool IsValueType => _type.IsValueType;
    public bool IsManaged => !_type.IsUnmanaged();

    internal void EnsureFieldAttached(ILField ilField)
    {
        Fields.Add(ilField);
    }

    internal void EnsureMethodAttached(ILMethod ilMethod)
    {
        Methods.Add(ilMethod);
    }

    public override string ToString()
    {
        return Name.Split("`").First() +
               (GenericArgs.Count > 0 ? $"<{string.Join(", ", GenericArgs.Select(ga => ga.ToString()))}>" : "");
    }

    public override bool Equals(object? obj)
    {
        return obj is ILType other && _type == other._type;
    }

    public override int GetHashCode()
    {
        return _type.GetHashCode();
    }
}

public class ILField(FieldInfo fieldInfo) : ILMember(fieldInfo)
{
    // TODO attributes
    private readonly FieldInfo _fieldInfo = fieldInfo;
    public FieldInfo Info => _fieldInfo;
    public ILType? DeclaringType { get; private set; }
    public bool IsStatic => _fieldInfo.IsStatic;
    public ILType? Type { get; private set; }
    public new string Name => _fieldInfo.Name;
    public new int MetadataToken => _fieldInfo.MetadataToken;
    public new bool IsConstructed = false;

    public override void Construct()
    {
        DeclaringType = ILInstanceBuilder.GetType((_fieldInfo.ReflectedType ?? _fieldInfo.DeclaringType)!);
        Type = ILInstanceBuilder.GetType(_fieldInfo.FieldType);
        DeclaringType.EnsureFieldAttached(this);
        IsConstructed = true;
    }

    public override string ToString()
    {
        return $"{Type} {Name}";
    }

    public override bool Equals(object? obj)
    {
        return obj is ILField other && other._fieldInfo == _fieldInfo;
    }

    public override int GetHashCode()
    {
        return _fieldInfo.GetHashCode();
    }
}
