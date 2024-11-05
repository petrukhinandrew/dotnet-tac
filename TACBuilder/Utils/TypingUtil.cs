using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using TACBuilder.Exprs;
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

    public static IlConstant ResolveConstant(object? obj)
    {
        if (obj == null) return new IlNullConst();
        if (obj is string strValue) return new IlStringConst(strValue);
        if (obj is bool boolValue) return new IlBoolConst(boolValue);
        if (obj is byte byteValue) return new IlByteConst(byteValue);
        if (obj is char charValue) return new IlIntConst(charValue);
        if (obj is short shortValue) return new IlIntConst(shortValue);
        if (obj is int intValue) return new IlIntConst(intValue);
        if (obj is long longValue) return new IlLongConst(longValue);
        if (obj is float floatValue) return new IlFloatConst(floatValue);
        if (obj is double doubleValue) return new IlDoubleConst(doubleValue);
        if (obj is nint nintValue) return new IlLongConst(nintValue);
        if (obj is Type type) return new IlTypeRef(IlInstanceBuilder.GetType(type));
        if (obj is MethodBase method) return new IlMethodRef(IlInstanceBuilder.GetMethod(method));
        if (obj is ReadOnlyCollection<CustomAttributeTypedArgument> coll) return resolveArrayConst(coll);
        if (obj is CustomAttributeTypedArgument attr) return ResolveConstant(attr.Value);
        if (obj.GetType().IsArray || obj is IEnumerable) return resolveArrayConst(obj);
        if (obj.GetType().IsEnum) return new IlEnumValue(obj);
        throw new Exception("unexpected const of type " + obj.GetType().Name);
    }

    // TODO test multi-dimensional array
    private static IlArrayConst resolveArrayConst(object obj)
    {
        var a = obj as ICollection;
        var values = new List<IlConstant>();
        foreach (var v in a)
        {
            values.Add(ResolveConstant(v));
        }

        if (a == null) throw new Exception("unexpected array constant " + obj);
        return new IlArrayConst(values[0].Type, values);
    }
}
