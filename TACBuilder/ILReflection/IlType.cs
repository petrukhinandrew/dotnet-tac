using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Reflection;
using Microsoft.Extensions.Logging;
using TACBuilder.Exprs;
using TACBuilder.Utils;
using Exception = System.Exception;

namespace TACBuilder.ILReflection;

/*
 * baseType
 * interfaces
 * genericConstraints
 * genericDefn
 * genericArgs
 * declType
 */

public class IlType(Type type) : IlMember(type)
{
    private readonly Type _type = type;
    public new bool IsConstructed;

    private const BindingFlags BindingFlags =
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static |
        System.Reflection.BindingFlags.DeclaredOnly;

    public IlType? DeclaringType { get; private set; }
    public IlMethod? DeclaringMethod { get; private set; }
    public IlType? BaseType { get; private set; }
    public List<IlType> Interfaces { get; } = [];
    public List<IlType> GenericArgs = [];

    public IlType? GenericDefinition;

    public override void Construct()
    {
        Logger.LogInformation("Constructing {Name}", Name);
        DeclaringAssembly = IlInstanceBuilder.GetAssembly(_type.Assembly);
        DeclaringAssembly.EnsureTypeAttached(this);
        if (_type.DeclaringType != null)
        {
            DeclaringType = IlInstanceBuilder.GetType(_type.DeclaringType);
        }

        if (_type.IsGenericParameter && _type.DeclaringMethod != null)
        {
            DeclaringMethod = IlInstanceBuilder.GetMethod(_type.DeclaringMethod);
        }

        if (_type.BaseType != null)
        {
            BaseType = IlInstanceBuilder.GetType(_type.BaseType);
        }

        if (_type.IsGenericType)
        {
            GenericDefinition = IlInstanceBuilder.GetType(_type.GetGenericTypeDefinition());
        }

        GenericArgs.AddRange(_type.GetGenericArguments().Select(IlInstanceBuilder.GetType).ToList());
        Interfaces.AddRange(_type.GetInterfaces().Select(IlInstanceBuilder.GetType));

        Attributes.AddRange(_type.CustomAttributes.Select(IlInstanceBuilder.GetAttribute).ToList());

        foreach (var ctor in _type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic |
                                                   BindingFlags.Instance | BindingFlags.Static))
        {
            Methods.Add(IlInstanceBuilder.GetMethod(ctor));
        }

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
    public string AsmName => _type.Assembly.GetName().ToString();

    public string Namespace => type.Namespace ?? DeclaringType?.Namespace ?? "";

    public new readonly string Name = type.Name;

    public string FullName => ConstructFullName();

    private string ConstructFullName()
    {
        if (IsGenericType && _type.IsGenericMethodParameter)
            return DeclaringMethod!.NonGenericSignature + "!" + Name;
        if (IsGenericType && _type.IsGenericTypeParameter)
            return DeclaringType!.FullName + "!" + Name;
        if (DeclaringType != null)
            return DeclaringType.FullName + "+" + Name;
        return Namespace == "" ? Name : Namespace + "." + Name;
    }

    public int ModuleToken => _type.Module.MetadataToken;
    public int MetadataToken => _type.MetadataToken;
    public List<IlAttribute> Attributes { get; } = [];

    public HashSet<IlMethod> Methods { get; } = new();
    public HashSet<IlField> Fields { get; } = new();

    public Type Type => _type;
    public bool IsValueType => _type.IsValueType;
    public virtual bool IsManaged => !_type.IsUnmanaged();

    public bool IsGenericParameter => _type.IsGenericParameter;

    public bool HasRefTypeConstraint => IsGenericParameter &&
                                        _type.GenericParameterAttributes.HasFlag(GenericParameterAttributes
                                            .ReferenceTypeConstraint);

    public bool HasNotNullValueTypeConstraint => IsGenericParameter &&
                                                 _type.GenericParameterAttributes.HasFlag(GenericParameterAttributes
                                                     .NotNullableValueTypeConstraint);

    public bool HasDefaultCtorConstraint => IsGenericParameter &&
                                            _type.GenericParameterAttributes.HasFlag(GenericParameterAttributes
                                                .DefaultConstructorConstraint);

    public bool IsCovariant => IsGenericParameter &&
                               _type.GenericParameterAttributes.HasFlag(GenericParameterAttributes.Covariant);

    public bool IsContravariant => IsGenericParameter &&
                                   _type.GenericParameterAttributes.HasFlag(GenericParameterAttributes.Contravariant);

    public bool IsGenericDefinition => _type.IsGenericTypeDefinition;


    public bool IsGenericType => _type.IsGenericType;
    public virtual bool IsUnmanaged => _type.IsUnmanaged();

    public virtual IlType ExpectedStackType() => this;

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

// TODO #2 makegenericType should be introduced here, not in AppTacBuilder
internal static class IlTypeHelpers
{
    public static IlArrayType MakeArrayType(this IlType type)
    {
        return (IlArrayType)IlInstanceBuilder.GetType(type.Type.MakeArrayType());
    }

    public static IlType MeetWith(this IlType type, IlType another)
    {
        return MeetIlTypes(type, another);
    }

    private static IlType MeetIlTypes(IlType? left, IlType? right)
    {
        if (left == null || right == null) return IlInstanceBuilder.GetType(typeof(object));
        if (left is IlPointerType lp && right is IlPointerType rp)
            return IlInstanceBuilder.GetType(MeetTypes(lp.TargetType.Type, rp.TargetType.Type));
        return IlInstanceBuilder.GetType(MeetTypes(left.Type, right.Type));
    }

    private static Type MeetTypes(Type? left, Type? right)
    {
        if (left == null || right == null) return typeof(object);
        // TODO defn improper, we may want pointer instead of by ref 
        if (left.IsByRef && right.IsPointer || left.IsPointer && right.IsByRef)
        {
            return MeetTypes(left.GetElementType(), right.GetElementType()).MakeByRefType();
        }

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
}