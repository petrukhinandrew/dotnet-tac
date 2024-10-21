using System.Diagnostics;
using System.Reflection;

namespace TACBuilder.ILReflection;

public static class ILInstanceBuilder
{
    private static readonly ILConstructQueue _queue = new();
    private static readonly ILCache _cache = new();
    private static readonly AssemblyCache _assemblyCache = new();
    // private static int Indexer = 0;
    internal static readonly List<Func<Assembly, bool>> AssemblyFilters = new();
    internal static readonly List<Func<Type, bool>> TypeFilters = new();
    internal static readonly List<Func<MethodBase, bool>> MethodFilters = new();

    public static ILAssembly BuildFrom(string assemblyPath)
    {
        var meta = GetAssembly(assemblyPath);
        Construct();
        return meta;
    }

    public static ILAssembly BuildFrom(AssemblyName assemblyName)
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

    public static List<ILAssembly> GetAssemblies()
    {
        return _cache.GetAssemblies();
    }

    internal static ILAssembly GetAssembly(Assembly assembly)
    {
        if (_cache.TryGetAssembly(assembly, out var meta)) return meta;
        var newAssembly = new ILAssembly(assembly);
        _cache.AddAssembly(assembly, newAssembly);
        _queue.Enqueue(newAssembly);
        return newAssembly;
    }

    internal static ILAssembly GetAssembly(string assemblyPath)
    {
        var asm = _assemblyCache.Get(assemblyPath);
        return GetAssembly(asm);
    }

    internal static ILAssembly GetAssembly(AssemblyName assemblyName)
    {
        var asm = _assemblyCache.Get(assemblyName);
        return GetAssembly(asm);
    }

    internal static ILType GetType(Type type)
    {
        if (_cache.TryGetType(type, out var meta)) return meta;
        var newType = new ILType(type);
        _cache.AddType(type, newType);
        _queue.Enqueue(newType);
        return newType;
    }

    internal static ILType GetType(MethodBase source, int token)
    {
        var args = SafeGenericArgs(source);
        try
        {
            Type type = source.Module.ResolveType(token, args.FromType, args.FromMethod);
            return GetType(type);
        }
        catch
        {
            AssertUnknownMetaComeFromCoreLib(source);
            return new UnknownIlType(source);
        }
    }

    internal static ILMethod GetMethod(MethodBase method)
    {
        if (_cache.TryGetMethod(method, out var meta)) return meta;
        var newMethod = new ILMethod(method);
        _cache.AddMethod(method, newMethod);
        _queue.Enqueue(newMethod);
        return newMethod;
    }

    internal static ILMethod GetMethod(MethodBase source, int token)
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

    internal static ILMethod.IParameter GetThisParameter(ILType ilType)
    {
        var instance = new ILMethod.This(ilType);
        _queue.Enqueue(instance);
        return instance;
    }

    internal static ILMethod.Parameter GetMethodParameter(ParameterInfo parameter, int index)
    {
        var instance = new ILMethod.Parameter(parameter, index);
        _queue.Enqueue(instance);
        return instance;
    }

    internal static ILMethod.ILBody GetMethodIlBody(ILMethod method)
    {
        var instance = new ILMethod.ILBody(method);
        _queue.Enqueue(instance);
        return instance;
    }

    internal static ILMethod.TACBody GetMethodTacBody(ILMethod method)
    {
        var instance = new ILMethod.TACBody(method);
        _queue.Enqueue(instance);
        return instance;
    }

    internal static ILField GetField(FieldInfo field)
    {
        if (_cache.TryGetField(field, out var meta)) return meta;
        var newField = new ILField(field);
        _cache.AddField(field, newField);
        _queue.Enqueue(newField);
        return newField;
    }

    internal static ILField GetField(MethodBase source, int token)
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

    internal static ILAttribute GetAttribute(CustomAttributeData attr)
    {
        if (_cache.TryGetAttribute(attr, out var meta)) return meta;
        var newAttr = new ILAttribute(attr);
        _cache.AddAttribute(attr, newAttr);
        _queue.Enqueue(newAttr);
        return newAttr;
    }

    private static ILMember GetMember(MemberInfo member)
    {
        if (member is Type type) return GetType(type);
        if (member is MethodBase method) return GetMethod(method);
        if (member is FieldInfo field) return GetField(field);
        throw new Exception("unexpected member type");
    }

    internal static ILMember GetMember(MethodBase source, int token)
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

    internal static ILString GetString(MethodBase source, int token)
    {
        var value = source.Module.ResolveString(token);
        return new ILString(value);
    }

    internal static ILSignature GetSignature(MethodBase source, int token)
    {
        var value = source.Module.ResolveSignature(token);
        return new ILSignature(value);
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
