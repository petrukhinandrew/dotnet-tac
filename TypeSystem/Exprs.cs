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


interface ILCallExpr : ILExpr { }
class ILStaticCall : ILCallExpr
{
    public ILType Type => throw new NotImplementedException();
}
class ILInstanceCall : ILCallExpr
{
    public ILType Type => throw new NotImplementedException();
}