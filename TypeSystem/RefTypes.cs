namespace Usvm.IL.TypeSystem;
interface ILRefType : ILType { }
class ILInterfaceType : ILRefType { }

abstract class ILObjectRefType(ILType targetType) : ILRefType
{
    public ILType Type => targetType;
}
class ILObjectRef(ILType targetType) : ILObjectRefType(targetType) { }
class ILRefTerminator : ILType { }
class ILNullRef() : ILObjectRef(new ILRefTerminator()) { }
class ILThisRef(ILType targetType) : ILObjectRef(targetType) { }
class ILArrayRef(ILType itemType) : ILObjectRefType(itemType) // ?LValue
{


}
class ILArrayAccessRef(ILType itemType) : ILObjectRefType(itemType), ILLValue
{
    public override string ToString()
    {
        return base.ToString();
    }
}
class ILStringRef() : ILObjectRefType(new ILRefTerminator()), ILLValue
{
    public override string ToString()
    {
        return base.ToString();
    }
}

class ILTypeRef() : ILObjectRefType(new ILRefTerminator()) { }

class ILFieldRef(ILType fType) : ILRefType, ILLValue
{
    public ILType Type => fType;
    public override string ToString()
    {
        return Type?.ToString() ?? "null field";
    }
}

interface ILRuntimePointerType : ILRefType { }
class ILManagedPointerType : ILRuntimePointerType { }
class ILUnmanagedPointerType : ILRuntimePointerType { }