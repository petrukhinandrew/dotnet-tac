using System.Net.Http.Headers;

namespace Usvm.IL.TypeSystem;
interface ILStmt
{
    public ILStmtLocation Location { get; }
    public string ToString();
}

class ILStmtLocation(int index)
{
    public int Index = index;
    public override string ToString()
    {
        return "TAC_" + Index.ToString() + " ";
    }
}
class ILStmtTargetLocation(int index, int ilIndex) : ILStmtLocation(index)
{
    public int ILIndex = ilIndex;
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
        return Location.ToString() + Lhs.ToString() + " = " + Rhs.ToString();
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
        return Location.ToString() + "invoke " + Call.ToString();
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
        if (retVal == null) return Location.ToString() + "return;";
        return Location.ToString() + "return " + retVal.ToString() + ";";
    }
}
interface ILBranchStmt : ILStmt { }

class ILGotoStmt(ILStmtLocation location, ILStmtTargetLocation target) : ILBranchStmt
{
    public ILStmtLocation Location => location;
    public ILStmtTargetLocation Target = target;
    public new string ToString()
    {
        return Location.ToString() + "goto " + Target.Index.ToString();
    }
}

class ILIfStmt(ILStmtLocation location, ILExpr cond, ILStmtTargetLocation target) : ILBranchStmt
{
    public ILStmtLocation Location => location;
    public ILStmtTargetLocation Target = target;
    public new string ToString()
    {
        return Location.ToString() + "if " + cond.ToString() + " goto " + Target.Index.ToString();
    }
}
