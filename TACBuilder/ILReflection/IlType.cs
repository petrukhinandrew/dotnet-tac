using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Reflection;
using Microsoft.Extensions.Logging;
using TACBuilder.Exprs;
using TACBuilder.Utils;

namespace TACBuilder.ILReflection;

public class IlPointerType(Type baseType) : IlType(baseType)
{
    public override bool IsManaged => false;
    public override bool IsUnmanaged => true;
    public IlType PointedType => IlInstanceBuilder.GetType(Type.GetElementType() ?? throw new("123"));
}

public class IlValueType(Type type) : IlType(type);

public class IlPrimitiveType(Type type) : IlValueType(type)
{
    public override bool IsManaged => false;
    public override bool IsUnmanaged => true;

    public new IlPrimitiveType ExpectedStackType()
    {
        if (Type == typeof(IntPtr) || Type == typeof(UIntPtr))
            return (IlPrimitiveType)
                IlInstanceBuilder.GetType(Type);
        return (IlPrimitiveType)(Type.GetTypeCode(Type) switch
        {
            TypeCode.SByte or TypeCode.Int16 or TypeCode.Int32 => IlInstanceBuilder.GetType(typeof(int)),
            TypeCode.Int64 => IlInstanceBuilder.GetType(typeof(long)),
            TypeCode.Byte or TypeCode.Char or TypeCode.UInt16 or TypeCode.UInt32 => IlInstanceBuilder.GetType(
                typeof(uint)),
            TypeCode.UInt64 => IlInstanceBuilder.GetType(typeof(ulong)),
            TypeCode.Single => IlInstanceBuilder.GetType(typeof(float)),
            TypeCode.Double => IlInstanceBuilder.GetType(typeof(double)),
            _ => throw new NotSupportedException("unhandled primitive stackc type " + ToString()),
        });
    }
}

public class IlEnumType(Type type) : IlValueType(type)
{
    public override bool IsManaged => false;
    public override bool IsUnmanaged => true;

    public IlType UnderlyingType = IlInstanceBuilder.GetType(Enum.GetUnderlyingType(type));
    public Dictionary<string, IlConstant> NameToValueMapping = new();

    public override void Construct()
    {
        Debug.Assert(Type.IsEnum);
        base.Construct();
        foreach (var value in Enum.GetValues(Type))
        {
            var name = Enum.GetName(Type, value) ?? "";
            NameToValueMapping.TryAdd(name, TypingUtil.ResolveConstant(value, Type));
        }
    }
}

public class IlStructType(Type type) : IlValueType(type);

public class IlReferenceType(Type type) : IlType(type)
{
    public override bool IsManaged => true;
    public override bool IsUnmanaged => false;
}

public class IlManagedReference(Type type) : IlReferenceType(type)
{
    public override bool IsManaged => true;
    public override bool IsUnmanaged => false;
    public IlType ReferencedType => IlInstanceBuilder.GetType(Type.GetElementType()!);
}

public class IlArrayType(Type type) : IlReferenceType(type)
{
    public IlType ElementType => IlInstanceBuilder.GetType(Type.GetElementType()!);
}

public class IlClassType(Type type) : IlReferenceType(type);

public class IlType(Type type) : IlMember(type)
{
    private readonly Type _type = type;
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

    public IlType ExpectedStackType()
    {
        return this;
    }

    public IlArrayType MakeArrayType()
    {
        return (IlArrayType)IlInstanceBuilder.GetType(Type.MakeArrayType());
    }

    public IlType MeetWith(IlType another)
    {
        return IlInstanceBuilder.GetType(MeetTypes(Type, another.Type));
    }

    private static Type MeetTypes(Type? left, Type? right)
    {
        if (left == null || right == null) return typeof(object);
        if (left.IsAssignableTo(right) || left.IsImplicitPrimitiveConvertibleTo(right)) return right;
        if (right.IsAssignableTo(left) || right.IsImplicitPrimitiveConvertibleTo(left)) return left;
        var workList = new Queue<Type>();
        if (left.BaseType != null)
            workList.Enqueue(left.BaseType);

        if (right.BaseType != null)
            workList.Enqueue(right.BaseType);
        foreach (var li in left.GetInterfaces())
            workList.Enqueue(li);
        foreach (var ri in right.GetInterfaces())
            workList.Enqueue(ri);
        Type? bestCandidate = null;
        while (workList.TryDequeue(out var candidate))
        {
            if (left.IsAssignableTo(candidate) && right.IsAssignableTo(candidate))
                if (bestCandidate == null || candidate.IsAssignableTo(bestCandidate))
                    bestCandidate = candidate;
        }

        return bestCandidate ?? MeetTypes(left.BaseType, right.BaseType);
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
    public Type Type => _type;
    public bool IsValueType => _type.IsValueType;
    public virtual bool IsManaged => !_type.IsUnmanaged();
    public bool IsGenericParameter => _type.IsGenericParameter;
    public virtual bool IsUnmanaged => _type.IsUnmanaged();

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
