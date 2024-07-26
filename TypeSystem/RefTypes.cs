namespace Usvm.IL.TypeSystem;
interface ILRefType : ILType { }
class ILInterfaceType : ILRefType { }

interface ILObjectRefType : ILRefType { }
class ILObjectRef : ILObjectRefType { }
class ILNullRef : ILObjectRef { }
class ILThisRef : ILObjectRef { }
class ILArrayAccessRef : ILObjectRefType, ILLValue
{
    public ILType Type => throw new NotImplementedException();

    public string Name => throw new NotImplementedException();
}
class ILStringRef : ILObjectRefType, ILLValue
{
    public ILType Type => throw new NotImplementedException();

    public string Name => throw new NotImplementedException();
}

class ILTypeRef : ILObjectRefType { }

class ILFieldRef : ILRefType, ILLValue
{
    public ILType Type => throw new NotImplementedException();

    public string Name => throw new NotImplementedException();
}

interface ILRuntimePointerType : ILRefType { }
class ILManagedPointerType : ILRuntimePointerType { }
class ILUnmanagedPointerType : ILRuntimePointerType { }