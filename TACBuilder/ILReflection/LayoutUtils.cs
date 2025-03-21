using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace TACBuilder.ILReflection;

public static unsafe class LayoutUtils
{
    [StructLayout(LayoutKind.Explicit)]
    private struct FieldDesc
    {
        [FieldOffset(0)] private readonly void* m_pMTOfEnclosingClass;

        // unsigned m_mb                   : 24;
        // unsigned m_isStatic             : 1;
        // unsigned m_isThreadLocal        : 1;
        // unsigned m_isRVA                : 1;
        // unsigned m_prot                 : 3;
        // unsigned m_requiresFullMbValue  : 1;
        [FieldOffset(8)] private readonly uint m_dword1;

        // unsigned m_dwOffset                : 27;
        // unsigned m_type                    : 5;
        [FieldOffset(12)] private readonly uint m_dword2;

        /// <summary>
        ///     Offset in memory
        /// </summary>
        public int Offset => (int)(m_dword2 & 0x7FFFFFF);
    }

    private static FieldDesc* GetFieldDescForFieldInfo(FieldInfo fi)
    {
        if (fi.IsLiteral)
        {
            // throw new Exception("Const field");
            Debug.WriteLine("Const field");
            return null;
        }

        FieldDesc* fd = (FieldDesc*)fi.FieldHandle.Value;
        return fd;
    }

    public static int CalculateOffsetOf(FieldInfo fi)
    {
        var desc = GetFieldDescForFieldInfo(fi);
        if (desc == null) return -1;
        return desc->Offset;
    }

    // Works correctly only for class (value types are boxed)
    public static int ClassSize(Type t)
    {
        return Marshal.ReadInt32(t.TypeHandle.Value, 4);
    }

    public static int SizeOf(Type type)
    {
        if (type == typeof(IntPtr) || type == typeof(UIntPtr) || type.IsByRef) return IntPtr.Size;
        if (type.IsNumeric()) return SizeOfPrimitive(type);
        if (type.ContainsGenericParameters) return 0;
        if (type.IsEnum) return SizeOfPrimitive(type.GetEnumUnderlyingType());
        if (type == typeof(void)) return 1;
        return SizeOfInternal(type);
    }

    private static int SizeOfInternal(Type type)
    {
        var m = new DynamicMethod("GetManagedSizeImpl", typeof(uint), null);
        var gen = m.GetILGenerator();
        gen.Emit(OpCodes.Sizeof, type);
        gen.Emit(OpCodes.Ret);
        var sz = (uint)m.Invoke(null, null)!;
        return (int)sz!;
    }

    private static int SizeOfPrimitive(Type type)
    {
        return type switch
        {
            _ when type == typeof(byte) => sizeof(byte),
            _ when type == typeof(sbyte) => sizeof(sbyte),
            _ when type == typeof(short) => sizeof(short),
            _ when type == typeof(ushort) => sizeof(ushort),
            _ when type == typeof(int) => sizeof(int),
            _ when type == typeof(uint) => sizeof(uint),
            _ when type == typeof(long) => sizeof(long),
            _ when type == typeof(ulong) => sizeof(ulong),
            _ when type == typeof(char) => sizeof(char),
            _ when type == typeof(IntPtr) => sizeof(IntPtr),
            _ when type == typeof(UIntPtr) => sizeof(UIntPtr),
            _ when type == typeof(float) => sizeof(float),
            _ when type == typeof(double) => sizeof(double),
            _ => throw new ArgumentException("not a primitive type")
        };
    }

    public static bool IsNumeric(this Type type)
    {
        return integralTypes.Contains(type) || realTypes.Contains(type);
    }

    private static HashSet<Type> integralTypes =
    [
        typeof(byte),
        typeof(sbyte),
        typeof(short),
        typeof(ushort),
        typeof(int),
        typeof(uint),
        typeof(long),
        typeof(ulong),
        typeof(char),
        typeof(IntPtr),
        typeof(UIntPtr)
    ];

    private static HashSet<Type> realTypes = [typeof(float), typeof(double)];
}