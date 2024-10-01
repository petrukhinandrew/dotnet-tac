using System.Runtime.InteropServices;

namespace TACBuilder.ILTAC.TypeSystem;

public interface ILRefType : ILType
{
}

public class ILClassOrInterfaceType(Type reflectedType, string qName) : ILRefType
{
    private string QualifiedName = qName;
    public Type ReflectedType => reflectedType;

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

public class ILArray(Type reflectedType, ILType elemType) : ILRefType
{
    public ILType ElemType => elemType;
    public Type ReflectedType => reflectedType;

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

public class ILObject : ILRefType
{
    public Type ReflectedType => typeof(object);

    public bool Equals(ILType? other)
    {
        return other is ILObject;
    }

    public override string ToString()
    {
        return "object";
    }
}

public class ILVoid : ILRefType
{
    public Type ReflectedType => typeof(void);

    public bool Equals(ILType? other)
    {
        return other is ILVoid;
    }

    public override string ToString()
    {
        return "void";
    }
}

public class ILNull : ILRefType
{
    public Type ReflectedType => typeof(void);

    public bool Equals(ILType? other)
    {
        return other is ILNull;
    }

    public override string ToString()
    {
        return "null";
    }
}

public class ILString : ILRefType
{
    public Type ReflectedType => typeof(string);

    public bool Equals(ILType? other)
    {
        return other is ILString;
    }

    public override string ToString()
    {
        return "string";
    }
}

public class ILHandleRef : ILRefType
{
    public Type ReflectedType => typeof(HandleRef);

    public bool Equals(ILType? other)
    {
        return other is ILHandleRef;
    }
}