using System.Diagnostics;
using System.Reflection;

namespace TACBuilder.ILMeta;

static class MetaCache
{
    private static MetaConstructQueue _queue = new();
    private static AssemblyCache _assemblyCache = new();
    private static Dictionary<Assembly, AssemblyMeta> _assemblies = new();
    private static Dictionary<Type, TypeMeta> _types = new();
    private static Dictionary<MethodBase, MethodMeta> _methods = new();
    private static Dictionary<FieldInfo, FieldMeta> _fields = new();

    public static void Construct()
    {
        while (_queue.Count > 0)
        {
            _queue.Dequeue().Construct();
        }
    }

    public static AssemblyMeta GetAssembly(Assembly assembly)
    {
        if (_assemblies.TryGetValue(assembly, out var assemblyMeta)) return assemblyMeta;
        _assemblies.Add(assembly, new AssemblyMeta(assembly));
        var meta = _assemblies[assembly];
        _queue.Enqueue(meta);
        return meta;
    }

    public static AssemblyMeta GetAssembly(string assemblyPath)
    {
        var asm = _assemblyCache.Get(assemblyPath);
        return GetAssembly(asm);
    }

    public static AssemblyMeta GetAssembly(AssemblyName assemblyName)
    {
        var asm = _assemblyCache.Get(assemblyName);
        return GetAssembly(asm);
    }

    public static TypeMeta GetType(Type type)
    {
        if (_types.TryGetValue(type, out var typeMeta)) return typeMeta;
        _types.Add(type, new TypeMeta(type));
        var meta = _types[type];
        _queue.Enqueue(meta);
        return meta;
    }

    public static TypeMeta GetType(MethodBase source, int token)
    {
        var args = safeGenericArgs(source);
        var type = source.Module.ResolveType(token, args.FromType, args.FromMethod);
        Debug.Assert(type is not null);
        return GetType(type);
    }

    public static MethodMeta GetMethod(MethodBase method)
    {
        if (_methods.TryGetValue(method, out var methodMeta)) return methodMeta;
        _methods.Add(method, new MethodMeta(method));
        var meta = _methods[method];
        _queue.Enqueue(meta);
        return meta;
    }

    public static MethodMeta GetMethod(MethodBase source, int token)
    {
        var args = safeGenericArgs(source);
        var method = source.Module.ResolveMethod(token, args.FromType, args.FromMethod);
        Debug.Assert(method is not null);
        return GetMethod(method);
    }

    public static FieldMeta GetField(FieldInfo field)
    {
        if (_fields.TryGetValue(field, out var fieldMeta)) return fieldMeta;
        _fields.Add(field, new FieldMeta(field));
        var meta = _fields[field];
        _queue.Enqueue(meta);
        return meta;
    }

    public static FieldMeta GetField(MethodBase source, int token)
    {
        var args = safeGenericArgs(source);
        var field = source.Module.ResolveField(token, args.FromType, args.FromMethod);
        Debug.Assert(field is not null);
        return GetField(field);
    }

    public static MemberMeta GetMember(MemberInfo member)
    {
        if (member is Type type) return GetType(type);
        if (member is MethodBase method) return GetMethod(method);
        if (member is FieldInfo field) return GetField(field);
        throw new Exception("unexpected member type");
    }

    public static MemberMeta GetMember(MethodBase source, int token)
    {
        var args = safeGenericArgs(source);
        var member = source.Module.ResolveMember(token, args.FromType, args.FromMethod);
        Debug.Assert(member is not null);
        return GetMember(member);
    }

    public static StringMeta GetString(MethodBase source, int token)
    {
        var value = source.Module.ResolveString(token);
        return new StringMeta(value);
    }

    public static SignatureMeta GetSignature(MethodBase source, int token)
    {
        var value = source.Module.ResolveSignature(token);
        return new SignatureMeta(value);
    }

    private static (Type[] FromType, Type[] FromMethod) safeGenericArgs(MethodBase source)
    {
        Type[] typeGenericArgs = [];
        Type[] methodGenericArgs = [];
        try
        {
            typeGenericArgs = (source.ReflectedType ?? source.DeclaringType)!.GetGenericArguments();
        }
        catch
        {
            // ignored
        }

        try
        {
            methodGenericArgs = source.GetGenericArguments();
        }
        catch
        {
            // ignored
        }

        return (typeGenericArgs, methodGenericArgs);
    }
}
