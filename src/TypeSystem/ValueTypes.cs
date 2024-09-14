namespace Usvm.IL.TypeSystem;

interface ILValueType : ILType
{
}

abstract class ILPrimitiveType : ILValueType
{
    public abstract Type ReflectedType { get; }

    public override bool Equals(object? obj)
    {
        return obj is ILPrimitiveType pt && ReflectedType == pt.ReflectedType;
    }

    public override int GetHashCode()
    {
        return ReflectedType.GetHashCode();
    }
}

class ILBool : ILPrimitiveType
{
    public override Type ReflectedType => typeof(bool);

    public override string ToString()
    {
        return "bool";
    }
}

class ILChar : ILPrimitiveType
{
    public override Type ReflectedType => typeof(char);

    public override string ToString()
    {
        return "char";
    }
}

class ILUInt8 : ILPrimitiveType
{
    public override Type ReflectedType => typeof(byte);

    public override string ToString()
    {
        return "uint8";
    }
}

class ILUInt16 : ILPrimitiveType
{
    public override Type ReflectedType => typeof(ushort);

    public override string ToString()
    {
        return "uint16";
    }
}

class ILUInt32 : ILPrimitiveType
{
    public override Type ReflectedType => typeof(ushort);

    public override string ToString()
    {
        return "uint32";
    }
}

class ILInt32 : ILPrimitiveType
{
    public override Type ReflectedType => typeof(int);

    public override string ToString()
    {
        return "int32";
    }
}

class ILInt64 : ILPrimitiveType
{
    public override Type ReflectedType => typeof(long);

    public override string ToString()
    {
        return "int64";
    }
}

class ILNativeInt : ILPrimitiveType
{
    public override Type ReflectedType => typeof(nint);
    public override string ToString() => "nint";
}

class ILNativeFloat : ILPrimitiveType
{
    public override Type ReflectedType => typeof(float);
    public override string ToString() => "nfloat";
}

class ILFloat32 : ILPrimitiveType
{
    public override Type ReflectedType => typeof(float);

    public override string ToString()
    {
        return "float32";
    }
}

class ILFloat64 : ILPrimitiveType
{
    public override Type ReflectedType => typeof(double);

    public override string ToString()
    {
        return "float64";
    }
}

class ILEnumType(Type reflectedType, string qName) : ILValueType
{
    private string QualifiedName = qName;
    public Type ReflectedType => reflectedType;

    public override string ToString()
    {
        return "enum " + QualifiedName;
    }
}

class ILStructType(Type reflectedType, string qName) : ILValueType
{
    private string QualifiedName = qName;
    public Type ReflectedType => reflectedType;

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