using System.Drawing;
using System.Reflection;
using Microsoft.Extensions.Logging;
using TACBuilder.ILMeta.ILBodyParser;

namespace TACBuilder.ILMeta;

public class TypeMeta(Type type) : MemberMeta(type)
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
        DeclaringAssembly = MetaBuilder.GetAssembly(_type.Assembly);
        if (_type.IsGenericType)
        {
            GenericArgs = _type.GetGenericArguments().Select(MetaBuilder.GetType).ToList();
        }

        var attributes = _type.CustomAttributes;
        foreach (var attribute in attributes)
        {
            // attribute.ConstructorArguments.Select(a => a.)
            Attributes.Add(MetaBuilder.GetType(attribute.AttributeType));
        }

        DeclaringAssembly.EnsureTypeAttached(this);
        if (MetaBuilder.TypeFilters.All(f => !f(_type))) return;

        var fields = _type.GetFields(BindingFlags);
        foreach (var field in fields)
        {
            Fields.Add(MetaBuilder.GetField(field));
        }

        var constructors = _type.GetConstructors(BindingFlags);
        var methods = _type.GetMethods(BindingFlags)
            .Where(method => method.IsGenericMethodDefinition || !method.IsGenericMethod);
        foreach (var callable in methods.Concat<MethodBase>(constructors))
        {
            Methods.Add(MetaBuilder.GetMethod(callable));
        }

        IsConstructed = true;
    }

    public AssemblyMeta DeclaringAssembly { get; private set; }
    public List<TypeMeta> Attributes { get; } = new();
    public List<TypeMeta> GenericArgs { get; private set; } = new();
    public HashSet<MethodMeta> Methods { get; private set; } = new();
    public HashSet<FieldMeta> Fields { get; private set; } = new();
    public new string Name => _type.Name;
    public string Namespace => _type.Namespace ?? "";
    public new int MetadataToken => _type.MetadataToken;
    public Type Type => _type;

    internal void EnsureFieldAttached(FieldMeta field)
    {
        Fields.Add(field);
    }

    internal void EnsureMethodAttached(MethodMeta method)
    {
        Methods.Add(method);
    }

    public override bool Equals(object? obj)
    {
        return obj is TypeMeta other && _type == other._type;
    }

    public override int GetHashCode()
    {
        return _type.GetHashCode();
    }
}

public class FieldMeta(FieldInfo fieldInfo) : MemberMeta(fieldInfo)
{
    private readonly FieldInfo _fieldInfo = fieldInfo;
    public TypeMeta? DeclaringType { get; private set; }
    public TypeMeta? Type { get; private set; }
    public new string Name => _fieldInfo.Name;
    public new int MetadataToken => _fieldInfo.MetadataToken;
    public new bool IsConstructed = false;

    public override void Construct()
    {
        DeclaringType = MetaBuilder.GetType((_fieldInfo.ReflectedType ?? _fieldInfo.DeclaringType)!);
        Type = MetaBuilder.GetType(_fieldInfo.FieldType);
        DeclaringType.EnsureFieldAttached(this);
        IsConstructed = true;
    }

    public override bool Equals(object? obj)
    {
        return obj is FieldMeta other && other._fieldInfo == _fieldInfo;
    }

    public override int GetHashCode()
    {
        return _fieldInfo.GetHashCode();
    }
}
