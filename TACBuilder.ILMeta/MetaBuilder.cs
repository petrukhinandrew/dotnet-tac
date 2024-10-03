using System.Reflection;

namespace TACBuilder.ILMeta;

public static class MetaBuilder
{
    private static readonly MetaConstructQueue _queue = new();
    private static readonly MetaCache _cache = new();
    private static readonly AssemblyCache _assemblyCache = new();

    internal static readonly List<Func<Assembly, bool>> AssemblyFilters = new();
    internal static readonly List<Func<Type, bool>> TypeFilters = new();
    internal static readonly List<Func<MethodBase, bool>> MethodFilters = new();

    public static AssemblyMeta BuildFrom(string assemblyPath)
    {
        var meta = GetAssembly(assemblyPath);
        Construct();
        return meta;
    }

    public static AssemblyMeta BuildFrom(AssemblyName assemblyName)
    {
        var meta = GetAssembly(assemblyName);
        Construct();
        return meta;
    }

    public static void AddAssemblyFilter(Func<Assembly, bool> filter)
    {
        AssemblyFilters.Add(filter);
    }

    public static void AddTypeFilter(Func<Type, bool> filter)
    {
        TypeFilters.Add(filter);
    }

    public static void AddMethodFilter(Func<MethodBase, bool> filter)
    {
        MethodFilters.Add(filter);
    }

    private static void Construct()
    {
        while (_queue.TryDequeue(out var meta))
        {
            meta.Construct();
        }
    }

    public static List<AssemblyMeta> GetAssemblies()
    {
        return _cache.GetAssemblies();
    }

    internal static AssemblyMeta GetAssembly(Assembly assembly)
    {
        if (_cache.TryGetAssembly(assembly, out var meta)) return meta;
        var newAssembly = new AssemblyMeta(assembly);
        _cache.AddAssembly(assembly, newAssembly);
        _queue.Enqueue(newAssembly);
        return newAssembly;
    }

    internal static AssemblyMeta GetAssembly(string assemblyPath)
    {
        var asm = _assemblyCache.Get(assemblyPath);
        return GetAssembly(asm);
    }

    internal static AssemblyMeta GetAssembly(AssemblyName assemblyName)
    {
        var asm = _assemblyCache.Get(assemblyName);
        return GetAssembly(asm);
    }

    internal static TypeMeta GetType(Type type)
    {
        if (_cache.TryGetType(type, out var meta)) return meta;
        var newType = new TypeMeta(type);
        _cache.AddType(type, newType);
        _queue.Enqueue(newType);
        return newType;
    }

    internal static TypeMeta GetType(MethodBase source, int token)
    {
        var args = SafeGenericArgs(source);
        try
        {
            Type type = source.Module.ResolveType(token, args.FromType, args.FromMethod);
            return GetType(type);
        }
        catch
        {
            return new UnknownTypeMeta();
        }
    }

    internal static MethodMeta GetMethod(MethodBase method)
    {
        if (_cache.TryGetMethod(method, out var meta)) return meta;
        var newMethod = new MethodMeta(method);
        _cache.AddMethod(method, newMethod);
        _queue.Enqueue(newMethod);
        return newMethod;
    }

    internal static MethodMeta GetMethod(MethodBase source, int token)
    {
        var args = SafeGenericArgs(source);
        try
        {
            MethodBase? method = source.Module.ResolveMethod(token, args.FromType, args.FromMethod);
            return method is null ? new UnknownMethodMeta() : GetMethod(method);
        }
        catch
        {
            return new UnknownMethodMeta();
        }
    }

    internal static FieldMeta GetField(FieldInfo field)
    {
        if (_cache.TryGetField(field, out var meta)) return meta;
        var newField = new FieldMeta(field);
        _cache.AddField(field, newField);
        _queue.Enqueue(newField);
        return newField;
    }

    internal static FieldMeta GetField(MethodBase source, int token)
    {
        var args = SafeGenericArgs(source);
        try
        {
            FieldInfo? field = source.Module.ResolveField(token, args.FromType, args.FromMethod);
            return field is null ? new UnknownFieldMeta() : GetField(field);
        }
        catch
        {
            return new UnknownFieldMeta();
        }
    }

    internal static AttributeMeta GetAttribute(CustomAttributeData attr)
    {
        if (_cache.TryGetAttribute(attr, out var meta)) return meta;
        var newAttr = new AttributeMeta(attr);
        _cache.AddAttribute(attr, newAttr);
        _queue.Enqueue(newAttr);
        return newAttr;
    }

    private static MemberMeta GetMember(MemberInfo member)
    {
        if (member is Type type) return GetType(type);
        if (member is MethodBase method) return GetMethod(method);
        if (member is FieldInfo field) return GetField(field);
        throw new Exception("unexpected member type");
    }

    internal static MemberMeta GetMember(MethodBase source, int token)
    {
        var args = SafeGenericArgs(source);
        try
        {
            MemberInfo? member = source.Module.ResolveMember(token, args.FromType, args.FromMethod);
            return member is null ? new UnknownMemberMeta() : GetMember(member);
        }
        catch
        {
            return new UnknownMemberMeta();
        }
    }

    internal static StringMeta GetString(MethodBase source, int token)
    {
        var value = source.Module.ResolveString(token);
        return new StringMeta(value);
    }

    internal static SignatureMeta GetSignature(MethodBase source, int token)
    {
        var value = source.Module.ResolveSignature(token);
        return new SignatureMeta(value);
    }

    private static (Type[] FromType, Type[] FromMethod) SafeGenericArgs(MethodBase source)
    {
        Type? t = (source.ReflectedType ?? source.DeclaringType);
        Type[] typeGenericArgs = t is null || !t.IsGenericType ? [] : t.GetGenericArguments();
        Type[] methodGenericArgs = source.IsGenericMethod ? source.GetGenericArguments() : [];
        return (typeGenericArgs, methodGenericArgs);
    }
}
