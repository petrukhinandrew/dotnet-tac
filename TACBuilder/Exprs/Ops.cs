using TACBuilder.ILReflection;

namespace TACBuilder.Exprs;

public abstract class IlUnaryOperation(IlExpr operand) : IlExpr
{
    public IlExpr Operand => operand;

    public IlType Type => operand.Type;

    public new string ToString()
    {
        return "unOp " + operand.ToString();
    }
}

class IlNegOp(IlExpr operand) : IlUnaryOperation(operand);

class IlNotOp(IlExpr operand) : IlUnaryOperation(operand);

public abstract class IlBinaryOperation(IlExpr lhs, IlExpr rhs, bool isChecked = false, bool isUnsigned = false)
    : IlExpr
{
    public abstract IlType Type { get; }

    public IlExpr Lhs => lhs;
    public IlExpr Rhs => rhs;
    public bool IsChecked => isChecked;
    public bool IsUnsigned => isUnsigned;
    public new string ToString() => $"{Lhs.ToString()} binop {Rhs.ToString()}";
}

class IlAddOp(IlExpr lhs, IlExpr rhs, bool isChecked = false, bool isUnsigned = false)
    : IlBinaryOperation(lhs, rhs, isChecked, isUnsigned)
{
    public override IlType Type => Lhs.Type.NumericBinOpType(Rhs.Type);
}

class IlSubOp(IlExpr lhs, IlExpr rhs, bool isChecked = false, bool isUnsigned = false)
    : IlBinaryOperation(lhs, rhs, isChecked, isUnsigned)
{
    public override IlType Type => Lhs.Type.NumericBinOpType(Rhs.Type);
}

class IlMulOp(IlExpr lhs, IlExpr rhs, bool isChecked = false, bool isUnsigned = false)
    : IlBinaryOperation(lhs, rhs, isChecked, isUnsigned)
{
    public override IlType Type => Lhs.Type.NumericBinOpType(Rhs.Type);
}

class IlDivOp(IlExpr lhs, IlExpr rhs, bool isUnsigned = false)
    : IlBinaryOperation(lhs, rhs, isChecked: false, isUnsigned)
{
    public override IlType Type => Lhs.Type.NumericBinOpType(Rhs.Type);
}

class IlRemOp(IlExpr lhs, IlExpr rhs, bool isUnsigned = false)
    : IlBinaryOperation(lhs, rhs, isChecked: false, isUnsigned)
{
    public override IlType Type => Lhs.Type.NumericBinOpType(Rhs.Type);
}

class IlAndOp(IlExpr lhs, IlExpr rhs)
    : IlBinaryOperation(lhs, rhs, isChecked: false, isUnsigned: false)
{
    public override IlType Type => Lhs.Type.NumericBinOpType(Rhs.Type);
}

class IlOrOp(IlExpr lhs, IlExpr rhs)
    : IlBinaryOperation(lhs, rhs, isChecked: false, isUnsigned: false)
{
    public override IlType Type => Lhs.Type.NumericBinOpType(Rhs.Type);
}

class IlXorOp(IlExpr lhs, IlExpr rhs)
    : IlBinaryOperation(lhs, rhs, isChecked: false, isUnsigned: false)
{
    public override IlType Type => Lhs.Type.NumericBinOpType(Rhs.Type);
}

class IlShlOp(IlExpr lhs, IlExpr rhs)
    : IlBinaryOperation(lhs, rhs, isChecked: false, isUnsigned: false)
{
    public override IlType Type => Lhs.Type.NumericBinOpType(Rhs.Type);
}

class IlShrOp(IlExpr lhs, IlExpr rhs, bool isUnsigned = false)
    : IlBinaryOperation(lhs, rhs, isChecked: false, isUnsigned)
{
    public override IlType Type => Lhs.Type.NumericBinOpType(Rhs.Type);
}

class IlCeqOp(IlExpr lhs, IlExpr rhs)
    : IlBinaryOperation(lhs, rhs, isChecked: false, isUnsigned: false)
{
    public override IlType Type => IlInstanceBuilder.GetType(typeof(int));
}

class IlCneOp(IlExpr lhs, IlExpr rhs)
    : IlBinaryOperation(lhs, rhs, isChecked: false, isUnsigned: true)
{
    public override IlType Type => IlInstanceBuilder.GetType(typeof(int));
}

class IlCgtOp(IlExpr lhs, IlExpr rhs, bool isUnsigned = false)
    : IlBinaryOperation(lhs, rhs, isChecked: false, isUnsigned)
{
    public override IlType Type => IlInstanceBuilder.GetType(typeof(int));
}

class IlCgeOp(IlExpr lhs, IlExpr rhs, bool isUnsigned = false)
    : IlBinaryOperation(lhs, rhs, isChecked: false, isUnsigned)
{
    public override IlType Type => IlInstanceBuilder.GetType(typeof(int));
}

class IlCltOp(IlExpr lhs, IlExpr rhs, bool isUnsigned = false)
    : IlBinaryOperation(lhs, rhs, isChecked: false, isUnsigned)
{
    public override IlType Type => IlInstanceBuilder.GetType(typeof(int));
}

class IlCleOp(IlExpr lhs, IlExpr rhs, bool isUnsigned = false)
    : IlBinaryOperation(lhs, rhs, isChecked: false, isUnsigned)
{
    public override IlType Type => IlInstanceBuilder.GetType(typeof(int));
}