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
    // TODO CRITICAL
    public IlType Type { get; } = lhs.Type;

    public IlExpr Lhs => lhs;
    public IlExpr Rhs => rhs;
    public bool IsChecked => isChecked;
    public bool IsUnsigned => isUnsigned;
    public new string ToString() => $"{Lhs.ToString()} binop {Rhs.ToString()}";
}

class IlAddOp(IlExpr lhs, IlExpr rhs, bool isChecked = false, bool isUnsigned = false)
    : IlBinaryOperation(lhs, rhs, isChecked, isUnsigned);

class IlSubOp(IlExpr lhs, IlExpr rhs, bool isChecked = false, bool isUnsigned = false)
    : IlBinaryOperation(lhs, rhs, isChecked, isUnsigned);

class IlMulOp(IlExpr lhs, IlExpr rhs, bool isChecked = false, bool isUnsigned = false)
    : IlBinaryOperation(lhs, rhs, isChecked, isUnsigned);

class IlDivOp(IlExpr lhs, IlExpr rhs, bool isUnsigned = false)
    : IlBinaryOperation(lhs, rhs, isChecked: false, isUnsigned);

class IlRemOp(IlExpr lhs, IlExpr rhs, bool isUnsigned = false)
    : IlBinaryOperation(lhs, rhs, isChecked: false, isUnsigned);

class IlAndOp(IlExpr lhs, IlExpr rhs)
    : IlBinaryOperation(lhs, rhs, isChecked: false, isUnsigned: false);

class IlOrOp(IlExpr lhs, IlExpr rhs)
    : IlBinaryOperation(lhs, rhs, isChecked: false, isUnsigned: false);

class IlXorOp(IlExpr lhs, IlExpr rhs)
    : IlBinaryOperation(lhs, rhs, isChecked: false, isUnsigned: false);

class IlShlOp(IlExpr lhs, IlExpr rhs)
    : IlBinaryOperation(lhs, rhs, isChecked: false, isUnsigned: false);

class IlShrOp(IlExpr lhs, IlExpr rhs, bool isUnsigned = false)
    : IlBinaryOperation(lhs, rhs, isChecked: false, isUnsigned);

class IlCeqOp(IlExpr lhs, IlExpr rhs)
    : IlBinaryOperation(lhs, rhs, isChecked: false, isUnsigned: false);

class IlCneOp(IlExpr lhs, IlExpr rhs)
    : IlBinaryOperation(lhs, rhs, isChecked: false, isUnsigned: true);

class IlCgtOp(IlExpr lhs, IlExpr rhs, bool isUnsigned = false)
    : IlBinaryOperation(lhs, rhs, isChecked: false, isUnsigned);

class IlCgeOp(IlExpr lhs, IlExpr rhs, bool isUnsigned = false)
    : IlBinaryOperation(lhs, rhs, isChecked: false, isUnsigned);

class IlCltOp(IlExpr lhs, IlExpr rhs, bool isUnsigned = false)
    : IlBinaryOperation(lhs, rhs, isChecked: false, isUnsigned);

class IlCleOp(IlExpr lhs, IlExpr rhs, bool isUnsigned = false)
    : IlBinaryOperation(lhs, rhs, isChecked: false, isUnsigned);