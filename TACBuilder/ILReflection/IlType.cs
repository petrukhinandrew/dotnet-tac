using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;
using TACBuilder.Exprs;
using TACBuilder.Utils;

namespace TACBuilder.ILReflection;

public class IlValueType(Type type) : IlType(type);

public class IlPrimitiveType(Type type) : IlValueType(type);

public class IlEnumType(Type type) : IlValueType(type)
{
    public IlType UnderlyingType = IlInstanceBuilder.GetType(Enum.GetUnderlyingType(type));
    public Dictionary<string, IlConstant> NameToValueMapping = new();

    public override void Construct()
    {
        Debug.Assert(Type.IsEnum);
        base.Construct();
        foreach (var value in Enum.GetValues(Type))
        {
            var name = Enum.GetName(Type, value) ?? "";
            NameToValueMapping.Add(name, TypingUtil.ResolveConstant(value, Type));
        }
    }
}

public class IlStructType(Type type) : IlValueType(type);

public class IlReferenceType(Type type) : IlType(type);

public class IlArrayType(Type type) : IlReferenceType(type)
{
    public IlType ElementType => IlInstanceBuilder.GetType(Type.GetElementType()!);
}

public class IlClassType(Type type) : IlReferenceType(type);

public class IlType(Type type) : IlMember(type)
{
    private readonly Type _type = type;
    public Type BaseType => _type;
    public new bool IsConstructed;

    private const BindingFlags BindingFlags =
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static |
        System.Reflection.BindingFlags.DeclaredOnly;

    public override void Construct()
    {
        Logger.LogInformation("Constructing {Name}", Name);
        DeclaringAssembly = IlInstanceBuilder.GetAssembly(_type.Assembly);
        if (_type.IsGenericType)
        {
            GenericArgs = _type.GetGenericArguments().Select(IlInstanceBuilder.GetType).ToList();
        }

        Attributes = _type.CustomAttributes.Select(IlInstanceBuilder.GetAttribute).ToList();

        DeclaringAssembly.EnsureTypeAttached(this);
        if (IlInstanceBuilder.TypeFilters.All(f => !f(_type))) return;

        var fields = _type.GetFields(BindingFlags);
        foreach (var field in fields)
        {
            Fields.Add(IlInstanceBuilder.GetField(field));
        }

        var constructors = _type.GetConstructors(BindingFlags);
        var methods = _type.GetMethods(BindingFlags)
            .Where(method => method.IsGenericMethodDefinition || !method.IsGenericMethod);
        foreach (var callable in methods.Concat<MethodBase>(constructors))
        {
            Methods.Add(IlInstanceBuilder.GetMethod(callable));
        }

        IsConstructed = true;
    }

    public IlAssembly DeclaringAssembly { get; private set; }
    public int AsmToken => _type.Assembly.GetHashCode();
    public int ModuleToken => _type.Module.MetadataToken;
    public int MetadataToken => _type.MetadataToken;
    public List<IlAttribute> Attributes { get; private set; }
    public List<IlType> GenericArgs { get; private set; } = new();
    public HashSet<IlMethod> Methods { get; } = new();
    public HashSet<IlField> Fields { get; } = new();
    public new string Name => _type.Name;
    protected Type Type => _type;
    public bool IsValueType => _type.IsValueType;
    public bool IsManaged => !_type.IsUnmanaged();
    public bool IsGenericParameter => _type.IsGenericParameter;
    public bool IsUnmanaged => _type.IsUnmanaged();

    internal void EnsureFieldAttached(IlField ilField)
    {
        Fields.Add(ilField);
    }

    internal void EnsureMethodAttached(IlMethod ilMethod)
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
        return obj is IlType other && _type == other._type;
    }

    public override int GetHashCode()
    {
        return _type.GetHashCode();
    }
}

public class IlField(FieldInfo fieldInfo) : IlMember(fieldInfo)
{
    // TODO attributes
    private readonly FieldInfo _fieldInfo = fieldInfo;
    public IlType? DeclaringType { get; private set; }
    public bool IsStatic => _fieldInfo.IsStatic;
    public IlType? Type { get; private set; }
    public new string Name => _fieldInfo.Name;
    public int ModuleToken => _fieldInfo.Module.MetadataToken;
    public int MetadataToken => _fieldInfo.MetadataToken;
    public object? GetValue(object? value) => _fieldInfo.GetValue(value);
    public List<IlAttribute> Attributes { get; private set; }
    public new bool IsConstructed = false;

    public override void Construct()
    {
        DeclaringType = IlInstanceBuilder.GetType((_fieldInfo.ReflectedType ?? _fieldInfo.DeclaringType)!);
        Type = IlInstanceBuilder.GetType(_fieldInfo.FieldType);
        Attributes = _fieldInfo.CustomAttributes.Select(IlInstanceBuilder.GetAttribute).ToList();
        DeclaringType.EnsureFieldAttached(this);
        IsConstructed = true;
    }

    public override string ToString()
    {
        return $"{Type} {Name}";
    }

    public override bool Equals(object? obj)
    {
        return obj is IlField other && other._fieldInfo == _fieldInfo;
    }

    public override int GetHashCode()
    {
        return _fieldInfo.GetHashCode();
    }
}
