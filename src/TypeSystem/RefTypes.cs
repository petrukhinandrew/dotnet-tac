using System.Security.Cryptography;

namespace Usvm.IL.TypeSystem;
interface ILRefType : ILType { }
class ILClassOrInterfaceType(string qName) : ILRefType
{
    private string QualifiedName = qName;
    public override string ToString()
    {
        return QualifiedName;
    }

}

class ILArrayRef(ILType elemType) : ILRefType
{
    public ILType ElemType => elemType;
}

class ILNull : ILRefType
{
    public override string ToString()
    {
        return "null";
    }
}
class ILString : ILRefType
{
    public override string ToString()
    {
        return "string";
    }
}

class ILHandleRef : ILRefType { }