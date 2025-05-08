using System.Reflection;

namespace TACBuilder.ILReflection;

public static class IlInstanceBuilder
{
    private static readonly ILConstructQueue _queue = new();
    private static readonly ILCache _cache = new();
    private static readonly AssemblyCache _assemblyCache = new();
    internal static readonly List<Func<Type, bool>> TypeFilters = [(t => _requireConstruction.Contains(t))];

    internal static readonly List<Func<MethodBase, bool>> MethodFilters =
    [
        (m =>
        {
            var declType = m.ReflectedType ?? m.DeclaringType;
            return declType != null && _requireConstruction.Contains(declType);
        })
    ];

    private static readonly List<Type> _requireConstruction =
    [
        typeof(byte),
        typeof(sbyte),
        typeof(ushort),
        typeof(short),
        typeof(char),
        typeof(int),
        typeof(uint),
        typeof(long),
        typeof(ulong),
        typeof(float),
        typeof(double),
        typeof(nint),
        typeof(nuint),
        typeof(Nullable<>),
        typeof(string),
        typeof(bool)
    ];
    public static IlAssembly BuildFrom(string assemblyPath)
    {
        var meta = GetAssembly(assemblyPath);
        Construct();
        return meta;
    }

    public static IlAssembly BuildFrom(AssemblyName assemblyName)
    {
        var meta = GetAssembly(assemblyName);
        Construct();
        return meta;
    }

    internal static IlAssembly BuildFrom(Assembly assembly)
    {
        var meta = GetAssembly(assembly);
        Construct();
        return meta;
    }

    public static void AddTypeFilter(Func<Type, bool> filter)
    {
        TypeFilters.Add(filter);
    }

    public static void AddMethodFilter(Func<MethodBase, bool> filter)
    {
        MethodFilters.Add(filter);
    }

    internal static void Construct()
    {
        while (_queue.TryDequeue(out var instance))
        {
            instance.Construct();
        }
    }

    public static List<IlAssembly> GetAssemblies()
    {
        return _cache.GetAssemblies();
    }

    public static Dictionary<string, List<string>> GetAsmDependencyGraph()
    {
        var mapping = _queue.BuiltAssemblies.ToDictionary(asm => asm.Name.ToString(),
            asm => asm.ReferencedAssemblies.Select(it => it.Name).ToList());
        _queue.DropBuiltAssemblies();
        return mapping;
    }

    public static List<IlType> GetFreshTypes()
    {
        IlType[] instances = new IlType[_queue.FreshInstances.Count];
        _queue.FreshInstances.CopyTo(instances, 0);
        _queue.DropFreshInstances();
        return instances.ToList();
    }

    internal static IlAssembly GetAssembly(Assembly assembly)
    {
        if (_cache.TryGetAssembly(assembly, out var meta)) return meta;
        var newAssembly = new IlAssembly(assembly);
        _cache.AddAssembly(assembly, newAssembly);
        _queue.Enqueue(newAssembly);
        return newAssembly;
    }

    internal static IlAssembly GetAssembly(string assemblyPath)
    {
        var asm = _assemblyCache.Get(assemblyPath);
        return GetAssembly(asm);
    }

    internal static IlAssembly GetAssembly(AssemblyName assemblyName)
    {
        var asm = _assemblyCache.Get(assemblyName);
        return GetAssembly(asm);
    }
    
    internal static IlType GetType(Type type)
    {
        if (_cache.TryGetType(type, out var meta)) return meta;
        var newType = CreateIlType(type);
        _cache.AddType(type, newType);
        _queue.Enqueue(newType);
        return newType;
    }

    private static IlType CreateIlType(Type type)
    {
        if (type.IsPointer)
            return new IlPointerType(type.GetElementType()!);
        if (type.IsByRef)
            return new IlPointerType(type.GetElementType()!, false);
        if (type.IsPrimitive) return new IlPrimitiveType(type);
        if (type.IsEnum) return new IlEnumType(type);
        if (type.IsValueType) return new IlStructType(type);
        if (type.IsArray) return new IlArrayType(type);
        return new IlClassType(type);
    }

    internal static IlType GetType(MethodBase source, int token)
    {
        var args = SafeGenericArgs(source);
        try
        {
            var type = source.Module.ResolveType(token, args.FromType, args.FromMethod);
            return GetType(type);
        }
        catch
        {
            AssertUnknownMetaComeFromCoreLib(source);
            return new UnknownIlType(source);
        }
    }

    internal static IlMethod GetMethod(MethodBase method)
    {
        if (_cache.TryGetMethod(method, out var meta)) return meta;
        var newMethod = new IlMethod(method);
        _cache.AddMethod(method, newMethod);
        _queue.Enqueue(newMethod);
        return newMethod;
    }

    internal static IlMethod GetMethod(MethodBase source, int token)
    {
        var args = SafeGenericArgs(source);
        try
        {
            MethodBase? method = source.Module.ResolveMethod(token, args.FromType, args.FromMethod);
            return method is null ? new UnknownIlMethod(source) : GetMethod(method);
        }
        catch
        {
            AssertUnknownMetaComeFromCoreLib(source);
            return new UnknownIlMethod(source);
        }
    }

    internal static IlMethod.IParameter GetThisParameter(IlType ilType)
    {
        var instance = new IlMethod.This(ilType);
        _queue.Enqueue(instance);
        return instance;
    }

    internal static IlMethod.Parameter GetMethodParameter(ParameterInfo parameter, int index)
    {
        var instance = new IlMethod.Parameter(parameter, index);
        _queue.Enqueue(instance);
        return instance;
    }

    internal static IlMethod.IlBody GetMethodIlBody(IlMethod method)
    {
        var instance = new IlMethod.IlBody(method);
        instance.Construct();
        return instance;
    }

    internal static IlMethod.TacBody GetMethodTacBody(IlMethod method)
    {
        var instance = new IlMethod.TacBody(method);
        instance.Construct();
        return instance;
    }

    internal static IlField GetField(FieldInfo field)
    {
        if (_cache.TryGetField(field, out var meta)) return meta;
        var newField = new IlField(field);
        _cache.AddField(field, newField);
        _queue.Enqueue(newField);
        return newField;
    }

    internal static IlField GetField(MethodBase source, int token)
    {
        var args = SafeGenericArgs(source);
        try
        {
            FieldInfo? field = source.Module.ResolveField(token, args.FromType, args.FromMethod);
            return field is null ? new UnknownIlField(source) : GetField(field);
        }
        catch
        {
            AssertUnknownMetaComeFromCoreLib(source);
            return new UnknownIlField(source);
        }
    }

    internal static IlAttribute GetAttribute(CustomAttributeData attr)
    {
        var instance = new IlAttribute(attr);
        _queue.Enqueue(instance);
        return instance;
    }

    private static IlMember GetMember(MemberInfo member)
    {
        if (member is Type type) return GetType(type);
        if (member is MethodBase method) return GetMethod(method);
        if (member is FieldInfo field) return GetField(field);
        throw new Exception("unexpected member type");
    }

    internal static IlMember GetMember(MethodBase source, int token)
    {
        var args = SafeGenericArgs(source);
        try
        {
            MemberInfo? member = source.Module.ResolveMember(token, args.FromType, args.FromMethod);
            return member is null ? new UnknownIlMember(source) : GetMember(member);
        }
        catch
        {
            AssertUnknownMetaComeFromCoreLib(source);
            return new UnknownIlMember(source);
        }
    }

    internal static IlString GetString(MethodBase source, int token)
    {
        var value = source.Module.ResolveString(token);
        return new IlString(value);
    }

    internal static IlSignature GetSignature(MethodBase source, int token)
    {
        var value = source.Module.ResolveSignature(token);
        return new IlSignature(value, source);
    }

    private static void AssertUnknownMetaComeFromCoreLib(MethodBase source)
    {
        // Debug.Assert(source.Module.Assembly.FullName.StartsWith("System.Private.CoreLib"));
    }

    private static (Type[] FromType, Type[] FromMethod) SafeGenericArgs(MethodBase source)
    {
        Type? t = (source.ReflectedType ?? source.DeclaringType);
        Type[] typeGenericArgs = t is null || !t.IsGenericType ? [] : t.GetGenericArguments();
        Type[] methodGenericArgs = source.IsGenericMethod ? source.GetGenericArguments() : [];
        return (typeGenericArgs, methodGenericArgs);
    }
}