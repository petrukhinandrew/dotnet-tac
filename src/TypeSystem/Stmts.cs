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
class ILStmtEHLocation() : ILStmtLocation(-1)
{
    public override string ToString()
    {
        return "";
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
        return Location.ToString() + Call.ToString();
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
        string arg = retVal?.ToString() ?? "";
        return Location.ToString() + "return " + arg;
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

class ILEHStmt(ILStmtLocation location, string value, ILExpr thrown) : ILStmt
{
    public ILEHStmt(ILStmtLocation location, string value) : this(location, value, new ILNullValue()) { }
    private string _value = value;
    private ILExpr _thrown = thrown;
    public ILStmtLocation Location => location;
    public new string ToString()
    {
        if (_thrown is ILNullValue)
            return string.Format("{0}{1}", Location.ToString(), _value);
        else
            return string.Format("{0}{1} {2}", Location.ToString(), _value, _thrown.ToString());
    }
}
class ILCatchStmt(ILStmtLocation location, ILType thrown) : ILStmt
{
    public ILStmtLocation Location => location;
    private ILType _thrown = thrown;
    public override string ToString()
    {
        return string.Format("{0}catch {1}", Location.ToString(), _thrown.ToString());
    }
}