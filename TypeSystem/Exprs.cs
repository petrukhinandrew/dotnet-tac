namespace Usvm.IL.TypeSystem;

// impl 86-87
abstract class ILUnaryOperation(ILExpr operand) : ILExpr
{
    public ILExpr Operand => operand;

    public ILType Type => operand.Type;
    public abstract new string ToString();
}
class ILUnaryNot(ILExpr operand) : ILUnaryOperation(operand)
{
    public override string ToString()
    {
        return "!" + Operand.ToString();
    }
}
class ILUnaryMinus(ILExpr operand) : ILUnaryOperation(operand)
{
    public override string ToString()
    {
        return "-" + Operand.ToString();
    }
}

abstract class ILBinaryOperation(ILExpr lhs, ILExpr rhs) : ILExpr
{
    public ILType Type => lhs.Type;

    public ILExpr Lhs => lhs;
    public ILExpr Rhs => rhs;
    public abstract new string ToString();
}

class ILAddOp(ILExpr lhs, ILExpr rhs) : ILBinaryOperation(lhs, rhs)
{
    public override string ToString()
    {
        return Lhs.ToString() + " + " + Rhs.ToString();
    }
}
class ILSubOp(ILExpr lhs, ILExpr rhs) : ILBinaryOperation(lhs, rhs)
{
    public override string ToString()
    {
        return Lhs.ToString() + " - " + Rhs.ToString();
    }
}
class ILMulOp(ILExpr lhs, ILExpr rhs) : ILBinaryOperation(lhs, rhs)
{
    public override string ToString()
    {
        return Lhs.ToString() + " * " + Rhs.ToString();
    }
}
class ILDivOp(ILExpr lhs, ILExpr rhs) : ILBinaryOperation(lhs, rhs)
{
    public override string ToString()
    {
        return Lhs.ToString() + " / " + Rhs.ToString();
    }
}
class ILRemOp(ILExpr lhs, ILExpr rhs) : ILBinaryOperation(lhs, rhs)
{
    public override string ToString()
    {
        return Lhs.ToString() + " % " + Rhs.ToString();
    }
}
class ILShrOp(ILExpr lhs, ILExpr rhs) : ILBinaryOperation(lhs, rhs)
{
    public override string ToString()
    {
        return Lhs.ToString() + " >> " + Rhs.ToString();
    }
}
class ILShlOp(ILExpr lhs, ILExpr rhs) : ILBinaryOperation(lhs, rhs)
{
    public override string ToString()
    {
        return Lhs.ToString() + " << " + Rhs.ToString();
    }
}

class ILAndOp(ILExpr lhs, ILExpr rhs) : ILBinaryOperation(lhs, rhs)
{
    public override string ToString()
    {
        return Lhs.ToString() + " & " + Rhs.ToString();
    }
}
class ILOrOp(ILExpr lhs, ILExpr rhs) : ILBinaryOperation(lhs, rhs)
{
    public override string ToString()
    {
        return Lhs.ToString() + " | " + Rhs.ToString();
    }
}
class ILXorOp(ILExpr lhs, ILExpr rhs) : ILBinaryOperation(lhs, rhs)
{
    public override string ToString()
    {
        return Lhs.ToString() + " ^ " + Rhs.ToString();
    }
}

class ILCeqOp(ILExpr lhs, ILExpr rhs) : ILBinaryOperation(lhs, rhs)
{
    public override string ToString()
    {
        return Lhs.ToString() + " == " + Rhs.ToString();
    }
}

class ILCgtOp(ILExpr lhs, ILExpr rhs) : ILBinaryOperation(lhs, rhs)
{
    public override string ToString()
    {
        return Lhs.ToString() + " > " + Rhs.ToString();
    }
}
class ILCltOp(ILExpr lhs, ILExpr rhs) : ILBinaryOperation(lhs, rhs)
{
    public override string ToString()
    {
        return Lhs.ToString() + " < " + Rhs.ToString();
    }
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