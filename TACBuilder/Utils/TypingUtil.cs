using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using TACBuilder.Exprs;
using TACBuilder.ILReflection;
using TACBuilder.ILTAC.TypeSystem;

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

    public static IlConstant ResolveConstant(object? obj, Type? baseEnum = null)
    {
        if (obj == null || obj is DBNull) return new IlNullConst();
        if (baseEnum is { IsEnum: true })
        {
            return new IlEnumConst((IlEnumType)IlInstanceBuilder.GetType(baseEnum), ResolveConstant(obj));
        }

        if (obj.GetType().IsEnum)
        {
            return new IlEnumConst((IlEnumType)IlInstanceBuilder.GetType(obj.GetType()),
                ResolveConstant(Convert.ChangeType(obj, obj.GetType().GetEnumUnderlyingType())));
        }

        var resultConst = obj switch
        {
            string strValue => new IlStringConst(strValue),
            bool boolValue => new IlBoolConst(boolValue),
            sbyte sbyteValue => new IlByteConst((byte)sbyteValue),
            byte byteValue => new IlByteConst(byteValue),
            char charValue => new IlIntConst(charValue),
            short shortValue => new IlIntConst(shortValue),
            ushort ushortValue => new IlIntConst((short)ushortValue), // TODO
            int intValue => new IlIntConst(intValue),
            uint uintValue => new IlIntConst((int)uintValue), // TODO
            long longValue => new IlLongConst(longValue),
            ulong ulongValue => new IlLongConst((long)ulongValue),
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
        var a = obj as ICollection ?? throw new Exception("expected collection, got " + obj.GetType());
        var values = new List<IlConstant>();
        foreach (var v in a)
        {
            values.Add(ResolveConstant(v));
        }

        if (a == null) throw new Exception("unexpected array constant " + obj);
        return new IlArrayConst((IlInstanceBuilder.GetType(obj.GetType()) as IlArrayType)!, values);
    }

    public static IlExpr WithTypeEnsured(this IlExpr expr, IlType expectedType)
    {
        // TODO constants optimisations
        if (Equals(expr.Type, expectedType)) return expr;
        return new IlConvCastExpr(expectedType, expr);
    }

    public static IlExpr Coerced(this IlExpr expr)
    {
        if (expr.Type == null)
            Console.WriteLine("lolekke");
        var coercedType = expr.Type.ExpectedStackType();
        return expr.WithTypeEnsured(coercedType);
    }

    // TODO same thing for brtrue and so on
    public static IlExpr GetBrFalseConst(IlExpr expr)
    {
        var t = expr.Type;
        if (t is IlEnumType enumType) t = enumType.UnderlyingType;
        if (t is IlReferenceType or IlPointerType) return new IlNullConst();
        if (t.Equals(IlInstanceBuilder.GetType(typeof(bool)))) return new IlBoolConst(false);
        if (t.Equals(IlInstanceBuilder.GetType(typeof(int)))) return new IlIntConst(0);
        if (t.Equals(IlInstanceBuilder.GetType(typeof(long)))) return new IlLongConst(0);
        throw new Exception("unexpected brfalse type " + t);
    }
}
