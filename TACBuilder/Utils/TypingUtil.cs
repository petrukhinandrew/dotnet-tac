using System.Diagnostics;
using TACBuilder.Exprs;
using TACBuilder.ILReflection;

namespace TACBuilder.Utils;

public static class TypingUtil
{
    public static IlType Merge(List<IlType> types)
    {
        var res = types.First();
        foreach (var type in types.Skip(1))
        {
            res = res.MeetWith(type);
        }

        return res;
    }

    public static bool IsImplicitPrimitiveConvertibleTo(this Type src, Type target)
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

    internal static IlExpr WithTypeEnsured(this IlExpr expr, IlType expectedType)
    {
        // TODO constants optimisations
        return Equals(expr.Type, expectedType) ? expr : new IlConvCastExpr(expectedType, expr);
    }

    internal static IlExpr Coerced(this IlExpr expr)
    {
        if (expr.Type == null)
        {
            Console.Error.WriteLine("null il type");
        }
        // Debug.Assert(expr.Type != null);
        var coercedType = expr.Type!.ExpectedStackType();
        return Equals(expr.Type, coercedType) ? expr : new IlConvCastExpr(coercedType, expr);
    }
}