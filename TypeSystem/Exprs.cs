namespace Usvm.IL.TypeSystem;

// impl 86-87
class ILUnaryOperation(ILExpr operand) : ILExpr
{
    public ILExpr Operand => operand;

    public ILType Type => operand.Type;
    public new string ToString()
    {
        return "o " + operand.ToString();
    }
}

class ILBinaryOperation(ILExpr lhs, ILExpr rhs) : ILExpr
{
    public ILType Type => lhs.Type;

    public ILExpr Lhs => lhs;
    public ILExpr Rhs => rhs;
    public new string ToString() => lhs.ToString() + " o " + rhs.ToString();
}

class ILNewExpr : ILExpr
{
    public ILType Type => throw new NotImplementedException();
    public override string ToString()
    {
        return "new ";
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

class ILNewArrayExpr : ILExpr
{
    public ILType Type => throw new NotImplementedException();
    public override string ToString()
    {
        return "new[]";
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