namespace Usvm.IL.TypeSystem;

// impl 86-87
class ILUnaryOperation(ILExpr operand) : ILExpr
{
    public ILExpr Operand => operand;

    public ILType Type => operand.Type;
    public new string ToString()
    {
        return "unOp " + operand.ToString();
    }
}

class ILBinaryOperation(ILExpr lhs, ILExpr rhs) : ILExpr
{
    public ILType Type => lhs.Type;

    public ILExpr Lhs => lhs;
    public ILExpr Rhs => rhs;
    public new string ToString() => lhs.ToString() + " binOp " + rhs.ToString();
}

class ILNewExpr(ILType type, ILExpr[] args) : ILExpr
{
    public ILType Type => type;
    public ILExpr[] Args = args;
    public override string ToString()
    {
        return "new " + Type.ToString() + " (" + string.Join(", ", Args.Select(a => a.Type)) + ")";
    }
}

class ILTypeOfExpr : ILExpr
{
    public ILType Type => throw new NotImplementedException();
    public override string ToString()
    {
        return "typeof ";
    }
}

class ILNewArrayExpr(ILType type, ILExpr size) : ILExpr
{
    public ILType Type => type;
    public ILExpr Size => size;
    public override string ToString()
    {
        return "new " + Type.ToString() + "[" + Size.ToString() + "]";
    }
}

interface ILCastExpr : ILExpr { }

// class ILBoxCastExpr : ILCastExpr
// {
//     public ILType Type => throw new NotImplementedException();
// }
// class ILUnboxCastExpr : ILCastExpr
// {
//     public ILType Type => throw new NotImplementedException();
// }
// class ILIsInstCastExpr : ILCastExpr
// {
//     public ILType Type => throw new NotImplementedException();
// }

class ILCallExpr : ILExpr
{
    public ILType Type => throw new NotImplementedException();
    public override string ToString()
    {
        return "invoke ";
    }
}