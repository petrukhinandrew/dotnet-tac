using System.Net.Http.Headers;

namespace Usvm.IL.TypeSystem;
interface ILStmt
{
    public ILStmtLocation Location { get; }
    public string ToString();
}
class ILStmtLocation(int index)
{
    int Index = index;
}

class ILAssignStmt : ILStmt
{
    public readonly ILLValue Lhs;
    public readonly ILExpr Rhs;
    public ILStmtLocation Location => _location;
    private ILStmtLocation _location;
    public ILAssignStmt(ILStmtLocation location, ILLValue lhs, ILExpr rhs)
    {
        Lhs = lhs;
        Rhs = rhs;
        _location = location;
    }

    public override string ToString()
    {
        return Lhs.ToString() + " = " + Rhs.ToString();
    }
}
class ILCallStmt : ILStmt
{
    private ILStmtLocation _location;
    public readonly ILCallExpr Call;
    public ILCallStmt(ILStmtLocation location, ILCallExpr expr)
    {
        _location = location;
        Call = expr;
    }
    public ILStmtLocation Location => _location;
    public override string ToString()
    {
        // TODO separate into invokespecial invokestatic and so on
        return "invoke " + Call.ToString();
    }
}

// return, leave, endfinally
interface ILLeaveScopeStmt : ILStmt { }

class ILReturnStmt(ILStmtLocation location, ILExpr? retVal) : ILLeaveScopeStmt
{
    public ILExpr? RetVal => retVal;
    public ILStmtLocation Location => location;
    public override string ToString()
    {
        if (retVal == null) return "return;";
        return "return " + retVal.ToString() + ";";
    }
}
interface ILBranchStmt : ILStmt { }

class ILGotoStmt : ILBranchStmt
{
    public ILStmtLocation Location => throw new NotImplementedException();
    public new string ToString()
    {
        return "goto ";
    }
}

class ILIfStmt : ILBranchStmt
{
    public ILStmtLocation Location => throw new NotImplementedException();
    public new string ToString()
    {
        return "if ";
    }
}
