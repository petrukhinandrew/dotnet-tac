namespace Usvm.IL.TypeSystem;

interface ILValueType : ILType { }

abstract class ILPrimitiveType : ILValueType
{
    public abstract Type BaseType { get; }
    public override bool Equals(object? obj)
    {
        return obj is ILPrimitiveType pt && BaseType == pt.BaseType;
    }

    public override int GetHashCode()
    {
        return BaseType.GetHashCode();
    }
}

class ILBool : ILPrimitiveType
{
    public override Type BaseType => typeof(bool);

    public override string ToString()
    {
        return "bool";
    }
}
class ILChar : ILPrimitiveType
{
    public override Type BaseType => typeof(char);
    public override string ToString()
    {
        return "char";
    }
}

class ILUInt8 : ILPrimitiveType
{
    public override Type BaseType => typeof(byte);
    public override string ToString()
    {
        return "uint8";
    }
}
class ILUInt16 : ILPrimitiveType
{
    public override Type BaseType => typeof(ushort);
    public override string ToString()
    {
        return "uint16";
    }
}
class ILUInt32 : ILPrimitiveType
{
    public override Type BaseType => typeof(ushort);
    public override string ToString()
    {
        return "uint32";
    }
}
class ILInt32 : ILPrimitiveType
{
    public override Type BaseType => typeof(int);
    public override string ToString()
    {
        return "int32";
    }
}
class ILInt64 : ILPrimitiveType
{
    public override Type BaseType => typeof(long);
    public override string ToString()
    {
        return "int64";
    }
}

class ILNativeInt : ILPrimitiveType
{
    public override Type BaseType => typeof(nint);
    public override string ToString() => "nint";
}
class ILNativeFloat : ILPrimitiveType
{
    public override Type BaseType => typeof(float);
    public override string ToString() => "nfloat";
}
class ILFloat32 : ILPrimitiveType
{
    public override Type BaseType => typeof(float);
    public override string ToString()
    {
        return "float32";
    }
}
class ILFloat64 : ILPrimitiveType
{
    public override Type BaseType => typeof(double);
    public override string ToString()
    {
        return "float64";
    }
}

class ILEnumType(string qName) : ILValueType
{
    private string QualifiedName = qName;
    public override string ToString()
    {
        return "enum " + QualifiedName;
    }
}

class ILStructType(string qName) : ILValueType
{
    private string QualifiedName = qName;
    public override string ToString()
    {
        return "struct " + QualifiedName;
    }
    public override bool Equals(object? obj)
    {
        return obj != null && obj is ILStructType s && QualifiedName == s.QualifiedName;
    }

    public override int GetHashCode()
    {
        return QualifiedName.GetHashCode();
    }
}