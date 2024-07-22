namespace Usvm.IL.TypeSystem;
interface ILRefType : ILType { }
// interface ILInterfaceType : ILRefType { }

interface ILObjectRefType : ILRefType { }
class ILObjectRef : ILObjectRefType { }
class ILThisRef : ILObjectRefType { }
class ILArrayAccessRef : ILObjectRefType { }
class ILTypeRef : ILObjectRefType { }

interface ILFieldRef : ILRefType { }

class ILStaticFieldRef : ILFieldRef { }
class ILInstanceFieldRef : ILFieldRef { }

interface ILRuntimePointerType : ILRefType { }
interface ILManagedPointerType : ILRuntimePointerType { } // -> ILValue

interface ILUnmanagedPointerType : ILRuntimePointerType { } // ?? 

// null, string, {1, 2, 3}, parameter