using org.jacodb.api.net.generated.models;
using TACBuilder.Exprs;
using TACBuilder.ILReflection;

namespace TACBuilder.ILTAC.TypeSystem;

public abstract class IlStmt()
{
    private int Index = Indexer++;
    private static int Indexer = 0;
    public new abstract string ToString();

    public override bool Equals(object? obj)
    {
        return obj != null && obj is IlStmt stmt && stmt.Index == Index;
    }

    public override int GetHashCode()
    {
        return Index;
    }
}

public class ILAssignStmt(IlValue lhs, IlExpr rhs) : IlStmt()
{
    public readonly IlValue Lhs = lhs;
    public readonly IlExpr Rhs = rhs;

    public override string ToString()
    {
        return Lhs.ToString() + " = " + Rhs.ToString();
    }
}

public class IlCalliStmt(IlCallIndirect expr) : IlStmt
{
    public IlCallIndirect Call => expr;

    public override string ToString()
    {
        return expr.ToString();
    }
}

public class IlCallStmt(IlCall expr) : IlStmt()
{
    public IlCall Call => expr;

    public override string ToString()
    {
        return expr.ToString();
    }
}

public class IlReturnStmt(IlExpr? retVal) : IlStmt()
{
    public IlExpr? RetVal => retVal;

    public override string ToString()
    {
        string arg = retVal?.ToString() ?? "";
        return "return " + arg;
    }
}

public abstract class IlBranchStmt(int target) : IlStmt()
{
    public int Target = target;
}

/*
 * for debug purposes, should not be sent into jacodb
 */
public class IlLeaveStmt(int target) : IlBranchStmt(target)
{
    public override string ToString()
    {
        return $"leave {Target}";
    }
}

public class IlGotoStmt(int target) : IlBranchStmt(target)
{
    public override string ToString()
    {
        return $"goto {Target}";
    }
}

public class IlIfStmt(IlExpr cond, int target) : IlBranchStmt(target)
{
    public IlExpr Condition => cond;

    public override string ToString()
    {
        return $"if {cond.ToString()} goto {Target}";
    }
}

public abstract class IlEhStmt : IlStmt;

public class IlThrowStmt(IlExpr value) : IlEhStmt
{
    public IlExpr Value => value;

    public override string ToString()
    {
        return $"throw {Value.ToString()}";
    }
}

public class IlRethrowStmt : IlEhStmt
{
    public override string ToString()
    {
        return "rethrow";
    }
}

public class IlEndFilterStmt(IlExpr value) : IlEhStmt
{
    public IlExpr Value => value;

    public override string ToString()
    {
        return "endfilter";
    }
}

public class IlEndFinallyStmt : IlEhStmt
{
    public bool IsMutable = true;

    public override string ToString()
    {
        return "endfinally";
    }
}

public class IlEndFaultStmt : IlEhStmt
{
    public override string ToString()
    {
        return "endfault";
    }
}