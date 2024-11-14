using TACBuilder.ILReflection;

namespace TACBuilder.Exprs;

public interface IlNumericConstant : IlConstant;

public class IlInt8Const(sbyte value) : IlNumericConstant
{
    public IlType Type => IlInstanceBuilder.GetType(typeof(sbyte));
    public sbyte Value => value;
}

public class IlUint8Const(byte value) : IlNumericConstant
{
    public IlType Type => IlInstanceBuilder.GetType(typeof(byte));
    public byte Value => value;
}

public class IlInt16Const(short value) : IlNumericConstant
{
    public IlType Type => IlInstanceBuilder.GetType(typeof(sbyte));
    public short Value => value;
}

public class IlUint16Const(ushort value) : IlNumericConstant
{
    public IlType Type => IlInstanceBuilder.GetType(typeof(byte));
    public ushort Value => value;
}

public class IlInt32Const(int value) : IlNumericConstant
{
    public IlType Type => IlInstanceBuilder.GetType(typeof(int));
    public int Value => value;
}

public class IlUint32Const(uint value) : IlNumericConstant
{
    public IlType Type => IlInstanceBuilder.GetType(typeof(uint));
    public uint Value => value;
}

public class IlInt64Const(long value) : IlNumericConstant
{
    public IlType Type => IlInstanceBuilder.GetType(typeof(long));
    public long Value => value;
}

public class IlUint64Const(ulong value) : IlNumericConstant
{
    public IlType Type => IlInstanceBuilder.GetType(typeof(ulong));
    public ulong Value => value;
}

public class IlFloatConst(float value) : IlNumericConstant
{
    public IlType Type => IlInstanceBuilder.GetType(typeof(float));
    public float Value => value;
}

public class IlDoubleConst(double value) : IlNumericConstant
{
    public IlType Type => IlInstanceBuilder.GetType(typeof(double));
    public double Value => value;
}

public class IlCharConst(char value) : IlNumericConstant
{
    public IlType Type => IlInstanceBuilder.GetType(typeof(char));
    public char Value => value;
}