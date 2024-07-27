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
    private ILType _itemType = itemType;
    public override string ToString()
    {
        return _itemType?.ToString() ?? "arrAccess ";
    }
}
class ILStringRef() : ILObjectRefType(new ILRefTerminator()), ILLValue
{
    public override string ToString()
    {
        return "string";
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

class ILHandleRef : ILRefType
{

}