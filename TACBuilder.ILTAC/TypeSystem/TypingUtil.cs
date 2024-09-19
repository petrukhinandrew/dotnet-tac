namespace TACBuilder.ILTAC.TypeSystem;

public static class TypingUtil
{
    public static ILType ILTypeFrom(Type type)
    {
        if (type == typeof(void)) return new ILVoid();
        if (type == typeof(object)) return new ILObject();
        if (type.IsValueType)
        {
            if (type.IsPrimitive)
            {
                if (type == typeof(char)) return new ILChar();
                if (type == typeof(bool)) return new ILBool();
                if (type == typeof(byte)) return new ILUInt8();
                if (type == typeof(short) || type == typeof(ushort)) return new ILUInt16();
                if (type == typeof(int)) return new ILInt32();
                if (type == typeof(long)) return new ILInt64();
                if (type == typeof(float)) return new ILFloat32();
                if (type == typeof(double)) return new ILInt64();
                if (type == typeof(int)) return new ILInt32();
                if (type == typeof(nint)) return new ILNativeInt();
                if (type == typeof(uint)) return new ILUInt32();
            }
            else if (type.IsEnum)
            {
                return new ILEnumType(type, type.FullName ?? type.AssemblyQualifiedName ?? type.Name);
            }
            else if (type.IsValueType)
            {
                return new ILStructType(type, FormatObjectName(type));
            }
        }
        else if (type.IsFunctionPointer)
        {
            throw new Exception("funcptr");
        }
        else if (type.IsPointer)
        {
            return new ILUnmanagedPointer(type, ILTypeFrom(type.GetElementType()!));
        }
        else if (type.IsByRef)
        {
            return new ILManagedPointer(type, ILTypeFrom(type.GetElementType()!));
        }
        else if (type.IsByRefLike)
        {
            // always on stack
            // no casts provided
            // TODO introduce new type
        }
        else if (type == typeof(string))
        {
            return new ILString();
        }
        else if (type.IsArray)
        {
            Type? elemType = type.GetElementType();
            if (elemType != null)
                return new ILArray(type, ILTypeFrom(elemType));
            else
                throw new Exception("bad elem type for " + type.ToString());
        }
        else if (type.IsInterface || type.IsClass)
        {
            return new ILClassOrInterfaceType(type, FormatObjectName(type));
        }

        throw new Exception("unhandled type " + type.ToString());
    }

    public static ILType Merge(List<ILType> types)
    {
        var res = types.First().ReflectedType;
        foreach (var type in types.Skip(1))
        {
            res = meetTypes(res, type.ReflectedType);
        }

        return ILTypeFrom(res);
    }

    private static Type meetTypes(Type? left, Type? right)
    {
        if (left == null || right == null) return typeof(object);
        if (left.IsAssignableTo(right)) return right;
        if (right.IsAssignableTo(left)) return left;
        return meetTypes(left.BaseType, right.BaseType);
    }

    private static string FormatObjectName(Type type)
    {
        string nsName = type.Namespace ?? "ns";
        string rawName = type.Name;
        string[] tokens = rawName.Split('`');
        if (tokens.Length == 1)
        {
            return $"{nsName}.{rawName}";
        }

        string name = tokens[0];
        int paramsCnt = int.Parse(tokens[1]);
        return string.Format("{0}.{1}<{2}>", nsName, name,
            string.Join(",", type.GenericTypeArguments.Select(t => ILTypeFrom(t).ToString())));
    }

    class UnmanagedCheck<T> where T : unmanaged
    {
    }

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