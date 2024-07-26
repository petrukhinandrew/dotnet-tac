namespace Usvm.IL.TypeSystem;
static class TypeSolver
{
    public static ILType Resolve(Type type)
    {
        if (type.IsValueType)
        {
            if (type.IsEnum) return new ILEnumType();
            if (type.IsClass) return new ILStructType();
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
            }
        }
        else if (type == typeof(string))
        {
            return new ILStringRef();
        }
        else if (type.IsArray)
        {
            return new ILArrayAccessRef();
        }
        else if (type.IsTypeDefinition)
        {
            return new ILTypeRef();
        }
        else if (type.IsInterface)
        {
            return new ILInterfaceType();
        }
        else if (type.IsClass)
        {
            return new ILObjectRef();
        }
        else if (type.IsPointer)
        {
            return new ILUnmanagedPointerType();
        }
        else if (type.IsByRef)
        {
            return new ILManagedPointerType();
        }
        throw new Exception("unhandled type " + type.ToString());
    }
}