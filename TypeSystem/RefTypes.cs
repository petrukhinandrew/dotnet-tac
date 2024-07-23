namespace Usvm.IL.TypeSystem;
interface ILRefType : ILType { }
// interface ILInterfaceType : ILRefType { }

interface ILObjectRefType : ILRefType { }
class ILObjectRef : ILObjectRefType { }
class ILNullRef : ILObjectRef { }
class ILArrayAccessRef : ILObjectRefType, ILLValue
{
    public ILType Type => throw new NotImplementedException();
}
class ILTypeRef : ILObjectRefType { }

interface ILFieldRef : ILRefType, ILLValue { }

class ILStaticFieldRef : ILFieldRef
{
    public ILType Type => throw new NotImplementedException();
}
class ILInstanceFieldRef : ILFieldRef
{
    public ILType Type => throw new NotImplementedException();
}

interface ILRuntimePointerType : ILRefType { }
interface ILManagedPointerType : ILRuntimePointerType { } // -> ILValue
interface ILUnmanagedPointerType : ILRuntimePointerType { } // ?? 