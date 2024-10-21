using TACBuilder.ILReflection;

namespace TACBuilder.ILTAC.TypeSystem;

public abstract class ILStmt()
{
    private int Index = Indexer++;
    private static int Indexer = 0;
    public new abstract string ToString();

    public override bool Equals(object? obj)
    {
        return obj != null && obj is ILStmt stmt && stmt.Index == Index;
    }

    public override int GetHashCode()
    {
        return Index;
    }
}

public class ILIndexedStmt(int index, ILStmt stmt)
{
    public int Index = index;
    public ILStmt Stmt = stmt;

    public override string ToString()
    {
        return Index + " " + Stmt.ToString();
    }
}

public class ILStmtMark(string mark) : ILStmt()
{
    public override string ToString()
    {
        return mark;
    }
}

public class ILAssignStmt(ILLValue lhs, ILExpr rhs) : ILStmt()
{
    public readonly ILLValue Lhs = lhs;
    public readonly ILExpr Rhs = rhs;

    public override string ToString()
    {
        return Lhs.ToString() + " = " + Rhs.ToString();
    }
}

public class ILCallStmt(ILCall expr) : ILStmt()
{
    // public ILCall Call = expr;

    public override string ToString()
    {
        return expr.ToString();
    }
}

public class ILReturnStmt(ILExpr? retVal) : ILStmt()
{
    public ILExpr? RetVal => retVal;

    public override string ToString()
    {
        string arg = retVal?.ToString() ?? "";
        return "return " + arg;
    }
}

public abstract class ILBranchStmt(int target) : ILStmt()
{
    public int Target = target;
}

public class ILGotoStmt(int target) : ILBranchStmt(target)
{
    public override string ToString()
    {
        return "goto " + Target;
    }
}

public class ILIfStmt(ILExpr cond, int target) : ILBranchStmt(target)
{
    public override string ToString()
    {
        return string.Format("if {0} goto {1}", cond.ToString(), Target);
    }
}

public class ILEHStmt(string value, ILExpr thrown) : ILStmt()
{
    public ILEHStmt(string value) : this(value, new ILNullValue())
    {
    }

    public string Value => value;
    private ILExpr _thrown = thrown;

    public override string ToString()
    {
        if (_thrown is ILNullValue)
            return Value;
        else
            return $"{Value} {_thrown.ToString()}";
    }
}
