namespace Usvm.IL.TypeSystem;
abstract class ILStmt(int index)
{
    public int Index = index;
    public abstract new string ToString();
    public override bool Equals(object? obj)
    {
        return obj != null && obj is ILStmt stmt && stmt.Index == Index;
    }
    public override int GetHashCode()
    {
        return Index;
    }
}
class ILStmtMark(string mark) : ILStmt(-1)
{
    public override string ToString()
    {
        return mark;
    }
}
class ILAssignStmt(int index, ILLValue lhs, ILExpr rhs) : ILStmt(index)
{

    public readonly ILLValue Lhs = lhs;
    public readonly ILExpr Rhs = rhs;
    public override string ToString()
    {
        return Lhs.ToString() + " = " + Rhs.ToString();
    }
}
class ILCallStmt(int index, ILCallExpr expr) : ILStmt(index)
{
    public ILCallExpr Call = expr;
    public override string ToString()
    {
        return Call.ToString();
    }
}

class ILReturnStmt(int index, ILExpr? retVal) : ILStmt(index)
{
    public ILExpr? RetVal => retVal;
    public override string ToString()
    {
        string arg = retVal?.ToString() ?? "";
        return "return " + arg;
    }
}
abstract class ILBranchStmt(int index) : ILStmt(index) { }

class ILGotoStmt(int index) : ILBranchStmt(index)
{
    public override string ToString()
    {
        return "goto";
    }
}

class ILIfStmt(int index, ILExpr cond) : ILBranchStmt(index)
{
    public override string ToString()
    {
        return string.Format("if {0} goto ", cond.ToString());
    }
}

class ILEHStmt(int index, string value, ILExpr thrown) : ILStmt(index)
{
    public ILEHStmt(int index, string value) : this(index, value, new ILNullValue()) { }
    private string _value = value;
    private ILExpr _thrown = thrown;
    public override string ToString()
    {
        if (_thrown is ILNullValue)
            return _value;
        else
            return string.Format("{0} {1}", _value, _thrown.ToString());
    }
}