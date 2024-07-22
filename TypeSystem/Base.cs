namespace Usvm.IL.TypeSystem;

interface ILType { }

interface ILEntity
{
    ILType Type { get; }
}

interface ILExpr : ILEntity { }
interface ILValue : ILEntity { }