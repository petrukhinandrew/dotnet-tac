using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.CompilerServices;
using TACBuilder.Utils;

namespace TACBuilder.ILReflection;

public class IlType(Type type) : IlMember(type)
{
    private readonly Type _type = type;
    public new bool IsConstructed;

    private const BindingFlags BindingFlags =
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static |
        System.Reflection.BindingFlags.DeclaredOnly;

    public virtual IlType? DeclaringType { get; private set; }

    public int Size { get; private set; }

    public IlMethod? DeclaringMethod { get; private set; }

    public IlType? BaseType { get; private set; }

    public bool IsInterface => _type.IsInterface;
    public bool IsAbstract => _type.IsAbstract;
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

        Size = LayoutUtils.SizeOf(_type);
        
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

        if (_type.IsGenericParameter)
        {
            GenericParameterConstraints.AddRange(_type.GetGenericParameterConstraints()
                .Select(IlInstanceBuilder.GetType));
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

    public string Namespace => _type.Namespace ?? DeclaringType?.Namespace ?? "";

    public new readonly string Name = type.Name;

    public virtual string FullName => ConstructFullName();

    protected string ConstructFullName()
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

    public List<IlType> GenericParameterConstraints = [];

    public bool IsGenericType => _type.IsGenericType;
    public virtual bool IsUnmanaged => _type.IsUnmanaged();

    public virtual IlType ExpectedStackType() => throw new ApplicationException();

    internal void EnsureFieldAttached(IlField ilField)
    {
        Fields.Add(ilField);
    }

    internal void EnsureMethodAttached(IlMethod ilMethod)
    {
        Methods.Add(ilMethod);
    }

    public IlArrayType MakeArrayType()
    {
        var arr = IlInstanceBuilder.GetType(_type.MakeArrayType());
        Debug.Assert(arr is IlArrayType);
        return (IlArrayType)arr;
    }

    public IlPointerType MakePointerType()
    {
        var res = IlInstanceBuilder.GetType(_type.MakePointerType());
        Debug.Assert(res is IlPointerType { IsUnmanaged: true });
        return (IlPointerType)res;
    }

    public IlPointerType MakeByRefType()
    {
        var res = IlInstanceBuilder.GetType(_type.MakeByRefType());
        Debug.Assert(res is IlPointerType { IsManaged: true });
        return (IlPointerType)res;
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
    public static IlType MeetWith(this IlType type, IlType another)
    {
        if (type is not IlPointerType lp || another is not IlPointerType rp)
            return IlInstanceBuilder.GetType(MeetTypes(type.Type, another.Type));

        Debug.Assert(lp.IsManaged == rp.IsManaged);
        var elemType = IlInstanceBuilder.GetType(MeetTypes(lp.TargetType.Type, rp.TargetType.Type));
        var ptr = lp.IsManaged ? elemType.MakeByRefType() : elemType.MakePointerType();
        return ptr;
    }

    private static Type MeetTypes(Type? left, Type? right)
    {
        while (true)
        {
            if (left == null || right == null) return typeof(object);
            // TODO check if such condition may occur
            if (left.IsByRef && right.IsPointer || left.IsPointer && right.IsByRef)
            {
                return MeetTypes(left.GetElementType(), right.GetElementType()).MakeByRefType();
            }

            if (left.IsAssignableTo(right) || left.IsImplicitPrimitiveConvertibleTo(right)) return right;
            if (right.IsAssignableTo(left) || right.IsImplicitPrimitiveConvertibleTo(left)) return left;
            var workList = new Queue<Type>();
            if (left.BaseType != null) workList.Enqueue(left.BaseType);

            if (right.BaseType != null) workList.Enqueue(right.BaseType);
            foreach (var li in left.GetInterfaces()) workList.Enqueue(li);
            foreach (var ri in right.GetInterfaces()) workList.Enqueue(ri);
            Type? bestCandidate = null;
            while (workList.TryDequeue(out var candidate))
            {
                if (!left.IsAssignableTo(candidate) || !right.IsAssignableTo(candidate)) continue;

                if (bestCandidate == null || candidate.IsAssignableTo(bestCandidate)) bestCandidate = candidate;
            }

            if (bestCandidate != null) return bestCandidate;
            left = left.BaseType;
            right = right.BaseType;
        }
    }

    public static IlType NumericBinOpType(this IlType lhs, IlType rhs)
    {
        Debug.Assert(lhs is not IlReferenceType && rhs is not IlReferenceType);
        if (lhs is IlPrimitiveType && rhs is IlPrimitiveType)
            return lhs.MeetWith(rhs);
        if (lhs is IlPointerType lp) return lp;
        if (rhs is IlPointerType rp) return rp;
        throw new ApplicationException("Illegal NumericBinOpType");
    }
}