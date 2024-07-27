namespace Usvm.IL.TypeSystem;

interface ILType { }

interface ILExpr
{
    ILType Type { get; }
    public string ToString();
}

interface ILValue : ILExpr { public string Name { get; } }

class ILNullValue : ILValue
{
    public ILType Type => new ILNullRef();

    public string Name => "null";

    public override string ToString()
    {
        return "null";
    }
}

interface ILLValue : ILValue
{
    public new string ToString();
}
class ILLocal(ILType type, string name) : ILLValue
{
    public ILType Type => type;

    public string Name => name;
    public new string ToString()
    {
        return name;
    }
}

class ILLiteral(ILType type, string value) : ILValue
{
    public ILType Type => type;

    public string Name => value;
    public new string ToString()
    {
        return value;
    }
}