namespace Usvm.IL.TypeSystem;

interface ILType { }

interface ILExpr
{
    ILType Type { get; }
    public string ToString();
}

interface ILValue : ILExpr { }



class ILNullValue : ILValue
{
    public ILType Type => new ILNullRef();

    public override string ToString() => "null";

}

interface ILLValue : ILValue { }
class ILLocal(ILType type, string name) : ILLValue
{
    public ILType Type => type;

    public new string ToString() => name;

}

class ILLiteral(ILType type, string value) : ILValue
{
    public ILType Type => type;
    public new string ToString() => value;

}
class ILRuntimeHandle(ILExpr handle) : ILValue
{
    public ILType Type => handle.Type;
    public override string ToString()
    {
        return Type.ToString() + " handle";
    }
}

class ILMethodType : ILType { }

class ILMethod(ILType retType, string name, ILExpr[] args) : ILValue
{
    public ILType ReturnType = retType;
    public string Name = name;
    public ILExpr[] Args = args;

    public ILType Type => throw new NotImplementedException();

    // TODO declaringClass
    public override string ToString()
    {
        return string.Format("{0} {1}({2})", ReturnType.ToString(), Name, string.Join(", ", Args.Select(p => p.ToString())));
    }
}