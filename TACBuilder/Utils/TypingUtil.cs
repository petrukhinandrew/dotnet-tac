using TACBuilder.ILReflection;

namespace TACBuilder.Utils;

public static class TypingUtil
{
    public static ILType Merge(List<ILType> types)
    {
        var res = types.First().BaseType;
        foreach (var type in types.Skip(1))
        {
            res = meetTypes(res.BaseType, type.BaseType);
        }

        return new ILType(res);
    }

    private static Type meetTypes(Type? left, Type? right)
    {
        if (left == null || right == null) return typeof(object);
        if (left.IsAssignableTo(right)) return right;
        if (right.IsAssignableTo(left)) return left;
        return meetTypes(left.BaseType, right.BaseType);
    }

    class UnmanagedCheck<T> where T : unmanaged
    {
    }
    // TODO introduce same thing inside TypeMeta
    public static bool IsUnmanaged(this Type t)
    {
        try
        {
            typeof(UnmanagedCheck<>).MakeGenericType(t);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
