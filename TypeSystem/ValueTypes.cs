namespace Usvm.IL.TypeSystem;

interface ILValueType : ILType { }

interface ILPrimitiveType : ILValueType
{
    public Type BaseType { get; }
}

class ILBool : ILPrimitiveType
{
    public Type BaseType => typeof(bool);

    public override string ToString()
    {
        return "bool";
    }
}
class ILChar : ILPrimitiveType
{
    public Type BaseType => typeof(char);
    public override string ToString()
    {
        return "char";
    }
}

class ILUInt8 : ILPrimitiveType
{
    public Type BaseType => typeof(byte);
    public override string ToString()
    {
        return "uint8";
    }
}
class ILUInt16 : ILPrimitiveType
{
    public Type BaseType => typeof(ushort);
    public override string ToString()
    {
        return "uint16";
    }
}
class ILInt32 : ILPrimitiveType
{
    public Type BaseType => typeof(int);
    public override string ToString()
    {
        return "int32";
    }
}
class ILInt64 : ILPrimitiveType
{
    public Type BaseType => typeof(long);
    public override string ToString()
    {
        return "int64";
    }
}

// TODO find use case 
// class ILNativeInt : ILPrimitiveType { }

class ILFloat32 : ILPrimitiveType
{
    public Type BaseType => typeof(float);
    public override string ToString()
    {
        return "float32";
    }
}
class ILFloat64 : ILPrimitiveType
{
    public Type BaseType => typeof(double);
    public override string ToString()
    {
        return "float64";
    }
}

class ILEnumType : ILValueType
{
    public override string ToString()
    {
        return "enum";
    }
}

class ILStructType : ILValueType
{
    public override string ToString()
    {
        return "struct";
    }
}