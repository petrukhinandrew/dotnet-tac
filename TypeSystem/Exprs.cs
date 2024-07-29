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

class ILNewArrayExpr(ILArrayRef type, ILExpr size) : ILExpr
{
    public ILType Type => type.ElemType;
    public ILExpr Size => size;
    public override string ToString()
    {
        return "new " + Type.ToString() + "[" + Size.ToString() + "]";
    }
}
class ILNewStaticArrayExpr(ILArrayRef type, ILExpr value) : ILExpr
{
    public ILType Type => type.ElemType;
    public ILExpr Value => value;
    public override string ToString()
    {
        return "new " + Type.ToString() + "[" + Value.ToString() + "]";
    }
}

class ILArrayAccess : ILLValue
{
    public ILArrayAccess(ILExpr arrRef, ILExpr idx)
    {
        _arrRef = arrRef;
        _idx = idx;
    }
    ILExpr _arrRef;
    ILExpr _idx;
    public ILType Type => _arrRef.Type;
    public ILExpr Index => _idx;

    public string Name => ToString();

    public override string ToString()
    {
        return _arrRef.ToString() + "[" + _idx.ToString() + "]";
    }
}
interface ILCastExpr : ILExpr { }

class ILCallExpr(ILMethod method) : ILExpr
{
    private ILMethod _method = method;
    public ILType Type => _method.ReturnType;
    public override string ToString()
    {
        return "invoke " + _method.ToString();
    }
}