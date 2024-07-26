using System.Runtime.CompilerServices;

namespace Usvm.IL.TypeSystem;

interface ILType { }

interface ILExpr
{
    ILType Type { get; }
    public string ToString();
}

interface ILValue : ILExpr { public string Name { get; } }

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