namespace Usvm.IL.TypeSystem;

interface ILValueType : ILType { }

interface ILPrimitiveType : ILValueType { }

class ILBool : ILPrimitiveType {
    public override string ToString()
    {
        return "bool";
    }
}
class ILChar : ILPrimitiveType { 
    public override string ToString()
    {
        return "char";
    }
}

class ILUInt8 : ILPrimitiveType {
    public override string ToString()
    {
        return "uint8";
    }
 }
class ILUInt16 : ILPrimitiveType { 
    public override string ToString()
    {
        return "uint16";
    }
}
class ILInt32 : ILPrimitiveType {
    public override string ToString()
    {
        return "int32";
    }
 }
class ILInt64 : ILPrimitiveType {
    public override string ToString()
    {
        return "int64";
    }
 }

// TODO find use case 
// class ILNativeInt : ILPrimitiveType { }

class ILFloat32 : ILPrimitiveType {
    public override string ToString()
    {
        return "float32";
    }
 }
class ILFloat64 : ILPrimitiveType {
    public override string ToString()
    {
        return "float64";
    }
 }

class ILEnumType : ILValueType {
    public override string ToString()
    {
        return "enum";
    }
 }

class ILStructType : ILValueType { 
    public override string ToString()
    {
        return "struct";
    }
}