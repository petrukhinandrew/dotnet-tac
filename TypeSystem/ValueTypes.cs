namespace Usvm.IL.TypeSystem;

interface ILValueType : ILType { }

interface ILPrimitiveType : ILValueType { }

class ILBoolean : ILPrimitiveType { }
class ILChar : ILPrimitiveType { }

class ILUInt8 : ILPrimitiveType { }
class UInt16 : ILPrimitiveType { }
class ILInt32 : ILPrimitiveType { }
class ILInt64 : ILPrimitiveType { }
class ILNativeInt : ILPrimitiveType { }

class ILFloat32 : ILPrimitiveType { }
class ILFloat64 : ILPrimitiveType { }

interface ILEnumType : ILValueType { }

interface ILStructType : ILValueType { }

// ? interface ILTupleType : ILValueType { }
