namespace Usvm.IL.TypeSystem;

// impl 86-87
interface ILUnaryOperation : ILExpr { }
interface ILBinaryOperation : ILExpr { }

class ILNewExpr : ILExpr
{
    public ILType Type => throw new NotImplementedException();
}

class ILTypeOfExpr : ILExpr
{
    public ILType Type => throw new NotImplementedException();
}

class ILNewArrayExpr : ILExpr
{
    public ILType Type => throw new NotImplementedException();
}

class ILCastExpr : ILExpr
{
    public ILType Type => throw new NotImplementedException();
}

