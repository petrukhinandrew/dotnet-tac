using System.Diagnostics;
using System.Reflection;

namespace TACBuilder.ILMeta.ILBodyParser;

public static class ResolveTypeUtil
{
    public static Type ResolveType(this MethodBase methodBase)
    {
        var resolveType = methodBase.ReflectedType ?? methodBase.DeclaringType;
        Debug.Assert(resolveType is not null);
        return resolveType;
    }
}

public static class TokenResolver
{
    public static FieldInfo ResolveField(int target, MethodBase source)
    {
        return source.Module.ResolveField(target,
            source.ResolveType().GetGenericArguments(),
            source.GetGenericArguments()) ?? throw new Exception("cannot resolve field in " + source.Module.Name);
    }

    public static Type ResolveType(int target, MethodBase source)
    {
        return source.Module.ResolveType(target,
            source.ResolveType().GetGenericArguments(),
            source.GetGenericArguments()) ?? throw new Exception("cannot resolve type in " + source.Module.Name);
    }

    internal static MethodBase ResolveMethod(int target, MethodBase source)
    {
        return source.Module.ResolveMethod(target,
                   source.ResolveType().GetGenericArguments(),
                   source.GetGenericArguments()) ??
               throw new Exception("cannot resolve method in " + source.Module.Name);
    }

    internal static MemberInfo ResolveMember(int target, MethodBase source)
    {
        return source.Module.ResolveMember(target,
            source.ResolveType().GetGenericArguments(),
            source.GetGenericArguments()) ?? throw new Exception("cannot resolve member");
    }

    internal static byte[] ResolveSignature(int target, MethodBase source)
    {
        return source.Module.ResolveSignature(target);
    }

    internal static string ResolveString(int target, MethodBase source)
    {
        return source.Module.ResolveString(target);
    }
}
