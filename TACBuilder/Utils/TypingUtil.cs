using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using TACBuilder.Exprs;
using TACBuilder.ILReflection;
using TACBuilder.ILTAC.TypeSystem;

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

    public static IlConstant ResolveConstant(object? obj, Type? baseEnum = null)
    {
        if (obj == null) return new IlNullConst();
        if (baseEnum is { IsEnum: true })
        {
            return new IlEnumConst(IlInstanceBuilder.GetType(baseEnum), ResolveConstant(obj));
        }

        if (obj.GetType().IsEnum)
        {
            return new IlEnumConst(IlInstanceBuilder.GetType(obj.GetType()),
                ResolveConstant(Convert.ChangeType(obj, obj.GetType().GetEnumUnderlyingType())));
        }

        var resultConst = obj switch
        {
            string strValue => new IlStringConst(strValue),
            bool boolValue => new IlBoolConst(boolValue),
            byte byteValue => new IlByteConst(byteValue),
            char charValue => new IlIntConst(charValue),
            short shortValue => new IlIntConst(shortValue),
            int intValue => new IlIntConst(intValue),
            long longValue => new IlLongConst(longValue),
            float floatValue => new IlFloatConst(floatValue),
            double doubleValue => new IlDoubleConst(doubleValue),
            nint nintValue => new IlLongConst(nintValue),
            Type type => new IlTypeRef(IlInstanceBuilder.GetType(type)),
            MethodBase method => new IlMethodRef(IlInstanceBuilder.GetMethod(method)),
            CustomAttributeTypedArgument attr => ResolveConstant(attr.Value),
            ReadOnlyCollection<CustomAttributeTypedArgument> coll => ResolveArrayConst(coll),
            _ => null
        };
        if (resultConst == null && (obj.GetType().IsArray || obj is IEnumerable)) return ResolveArrayConst(obj);
        return resultConst ?? throw new Exception("unexpected const of type " + obj.GetType().Name);
    }

    private static IlArrayConst ResolveArrayConst(object obj)
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

    public static IlExpr WithTypeEnsured(this IlExpr expr, IlType expectedType)
    {
        // TODO constants optimisations
        if (Equals(expr.Type, expectedType)) return expr;
        return new IlConvCastExpr(expectedType, expr);
    }
}
