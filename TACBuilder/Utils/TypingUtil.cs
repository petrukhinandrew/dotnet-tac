using TACBuilder.ILReflection;

namespace TACBuilder.Utils;

public static class TypingUtil
{
    public static IlType Merge(List<IlType> types)
    {
        var res = types.First().BaseType;
        foreach (var type in types.Skip(1))
        {
            res = MeetTypes(res, type.BaseType);
        }

        return IlInstanceBuilder.GetType(res);
    }

    private static bool IsImplicitPrimitiveConvertibleTo(this Type src, Type target)
    {
        if (!src.IsPrimitive || !target.IsPrimitive) return false;
        var conversionMapping = new Dictionary<Type, List<Type>>
        {
            {
                typeof(sbyte), [
                    typeof(short), typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal), typeof
                        (nint)
                ]
            },
            {
                typeof(byte), [
                    typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong),
                    typeof(float), typeof(double), typeof(decimal), typeof
                        (nint),
                    typeof(nuint)
                ]
            },
            {
                typeof(ushort),
                [
                    typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double),
                    typeof(decimal), typeof(nint), typeof(nuint)
                ]
            },
            {
                typeof(short), [typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal), typeof(nint)]
            },

            { typeof(int), [typeof(long), typeof(float), typeof(double), typeof(decimal), typeof(nint)] },
            {
                typeof(uint),
                [typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal), typeof(nuint)]
            },
            { typeof(long), [typeof(float), typeof(double), typeof(decimal)] },
            { typeof(ulong), [typeof(float), typeof(double), typeof(decimal)] },
            { typeof(float), [typeof(double)] },
            { typeof(nint), [typeof(long), typeof(float), typeof(double), typeof(decimal)] },
            { typeof(nuint), [typeof(ulong), typeof(float), typeof(double), typeof(decimal)] },
        };
        return conversionMapping.TryGetValue(src, out var possible) && possible.Contains(target);
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

    class UnmanagedCheck<T> where T : unmanaged
    {
    }

    public static bool IsUnmanaged(this Type t)
    {
        try
        {

            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            typeof(UnmanagedCheck<>).MakeGenericType(t);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
