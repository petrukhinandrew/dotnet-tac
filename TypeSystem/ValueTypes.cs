namespace Usvm.IL.TypeSystem;

interface ILValueType : ILType { }

interface ILPrimitiveType : ILValueType { }

class ILBool : ILPrimitiveType { }
class ILChar : ILPrimitiveType { }

class ILUInt8 : ILPrimitiveType { }
class ILUInt16 : ILPrimitiveType { }
class ILInt32 : ILPrimitiveType { }
class ILInt64 : ILPrimitiveType { }

// TODO find use case 
// class ILNativeInt : ILPrimitiveType { }

class ILFloat32 : ILPrimitiveType { }
class ILFloat64 : ILPrimitiveType { }

class ILEnumType : ILValueType { }

class ILStructType : ILValueType { }