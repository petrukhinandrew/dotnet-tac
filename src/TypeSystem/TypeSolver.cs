using System.Reflection;

namespace Usvm.IL.TypeSystem;
static class TypeSolver
{
    public static ILType Resolve(Type type)
    {
        if (type == typeof(void)) return new ILNull();
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
                return new ILStructType(type.FullName ?? type.AssemblyQualifiedName ?? type.Name);
            }
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
            return new ILClassOrInterfaceType(type.FullName ?? type.AssemblyQualifiedName ?? type.Name);
        }
        throw new Exception("unhandled type " + type.ToString());
    }
}