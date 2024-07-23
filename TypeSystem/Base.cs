namespace Usvm.IL.TypeSystem;

interface ILType { }

interface ILEntity
{
    ILType Type { get; }
}

interface ILExpr : ILEntity { }
interface ILValue : ILEntity { }

interface ILStmt
{
    public ILStmtLocation Location { get; }
}
class ILStmtLocation;


interface ILLValue : ILEntity { }
class ILLocal : ILEntity
{
    public ILType Type => throw new NotImplementedException();
}