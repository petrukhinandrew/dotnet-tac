using System.Globalization;
using System.Reflection;
using Usvm.IL.Utils;

namespace Usvm.IL.TypeSystem;
static class TypeSolver
{
    public static ILType Resolve(Type type)
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
                return new ILEnumType(type.FullName ?? type.AssemblyQualifiedName ?? type.Name);
            }
            else if (type.IsValueType)
            {
                return new ILStructType(FormatObjectName(type));
            }
        }
        else if (type.IsFunctionPointer) {
            throw new Exception("funcptr");
        }
        else if (type.IsPointer)
        {
            return new ILUnmanagedPointer(Resolve(type.GetElementType()!));
        }
        else if (type.IsByRef)
        {
            return new ILManagedPointer(Resolve(type.GetElementType()!));
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
                return new ILArray(Resolve(elemType));
            else
                throw new Exception("bad elem type for " + type.ToString());
        }
        else if (type.IsInterface || type.IsClass)
        {
            return new ILClassOrInterfaceType(FormatObjectName(type));
        }
        throw new Exception("unhandled type " + type.ToString());
    }

    private static string FormatObjectName(Type type)
    {
        string nsName = type.Namespace ?? "ns";
        string rawName = type.Name;
        string[] tokens = rawName.Split('`');
        if (tokens.Count() == 1)
        {
            return string.Format("{0}.{1}", nsName, rawName);
        }
        string name = tokens[0];
        int paramsCnt = int.Parse(tokens[1]);
        return string.Format("{0}.{1}<{2}>", nsName, name, string.Join(",", type.GenericTypeArguments.Select(t => Resolve(t).ToString())));
    }

    class UnmanagedCheck<T> where T : unmanaged { }
    public static bool IsUnmanaged(this Type t)
    {
        try { typeof(UnmanagedCheck<>).MakeGenericType(t); return true; }
        catch (Exception) { return false; }
    }
}