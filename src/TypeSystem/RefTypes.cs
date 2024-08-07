namespace Usvm.IL.TypeSystem;
interface ILRefType : ILType { }
class ILClassOrInterfaceType(string qName) : ILRefType
{
    private string QualifiedName = qName;
    public override string ToString()
    {
        return QualifiedName;
    }
    public override bool Equals(object? obj)
    {
        return obj is ILClassOrInterfaceType another && another.QualifiedName == QualifiedName;
    }

    public override int GetHashCode()
    {
        return QualifiedName.GetHashCode();
    }
}
class ILArray(ILType elemType) : ILRefType
{
    public ILType ElemType => elemType;
    public override string ToString()
    {
        return elemType.ToString() + "[]";
    }
    public override bool Equals(object? obj)
    {
        return obj is ILArray arr && ElemType == arr.ElemType;
    }
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}

class ILObject : ILRefType
{
    public bool Equals(ILType? other)
    {
        return other is ILObject;
    }

    public override string ToString()
    {
        return "object";
    }
}
class ILNull : ILRefType
{
    public bool Equals(ILType? other)
    {
        return other is ILNull;
    }

    public override string ToString()
    {
        return "null";
    }
}
class ILString : ILRefType
{
    public bool Equals(ILType? other)
    {
        return other is ILString;
    }

    public override string ToString()
    {
        return "string";
    }
}

class ILHandleRef : ILRefType
{
    public bool Equals(ILType? other)
    {
        return other is ILHandleRef;
    }
}