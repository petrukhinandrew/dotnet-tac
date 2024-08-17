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

class ILNewDefaultExpr(ILType type) : ILExpr
{
    public ILType Type => type;
    public override string ToString()
    {
        return string.Format("new {0}(default)", Type.ToString());
    }
}
class ILNewExpr(ILType type, ILExpr[] args) : ILExpr
{
    public ILType Type => type;
    public ILExpr[] Args = args;
    public override string ToString()
    {
        return "new " + Type.ToString() + " (" + string.Join(", ", Args.Select(a => a.ToString())) + ")";
    }
}

class ILSizeOfExpr(ILType type) : ILExpr
{
    public ILType Type => new ILUInt32();
    public ILType Arg => type;
    public override string ToString()
    {
        return "sizeof " + Arg.ToString();
    }
}


class ILNewArrayExpr(ILArray type, ILExpr size) : ILExpr
{
    public ILType Type => type.ElemType;
    public ILExpr Size => size;
    public override string ToString()
    {
        return "new " + Type.ToString() + "[" + Size.ToString() + "]";
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
class ILArrayLength(ILExpr arr) : ILExpr
{
    private ILExpr _arr = arr;
    private ILType _type = new ILInt32();
    public ILType Type => _type;
    public override string ToString()
    {
        return _arr.ToString() + ".Length";
    }
}
abstract class ILCastExpr(ILType targetType, ILExpr target) : ILExpr
{
    protected ILType _targetType = targetType;
    protected ILExpr _target = target;
    public ILType Type => _targetType;
    public override string ToString()
    {
        return string.Format("({0}) {1}", _targetType.ToString(), _target.ToString());
    }
}
class ILConvExpr(ILPrimitiveType targetType, ILExpr value) : ILCastExpr(targetType, value) { }
class ILBoxExpr(ILValue value) : ILCastExpr(new ILObject(), value) { }
class ILUnboxExpr(ILType targetType, ILExpr value) : ILCastExpr(targetType, value) { }
class ILCastClassExpr(ILType targetType, ILExpr value) : ILCastExpr(targetType, value) { }
class ILCondCastExpr(ILType targetType, ILExpr value) : ILCastExpr(targetType, value)
{
    public override string ToString()
    {
        return string.Format("{0} as {1}", _target.ToString(), _targetType.ToString());
    }
}
class ILCallExpr(ILMethod method) : ILExpr
{
    protected ILMethod _method = method;
    public ILType Type => _method.ReturnType;

    public override string ToString()
    {
        return "invoke " + _method.ToString();
    }
}
class ILInstanceCallExpr(ILExpr instance, ILMethod method) : ILCallExpr(method)
{
    private ILExpr _instance = instance;
    public override string ToString()
    {
        return "invoke " + _instance.ToString() + "." + _method.Name + "(" + string.Join(", ", _method.Args.Select(p => p.ToString())) + ")";
    }
}
interface ILRefExpr : ILExpr
{
    public ILExpr Value { get; }
}
interface ILDerefExpr : ILLValue { }

class PointerExprTypeResolver
{
    public static ILDerefExpr DerefAs(ILExpr instance, ILType type)
    {
        switch (instance.Type)
        {
            case ILManagedPointer:
                {
                    return new ILManagedDeref(instance, type);
                }
            case ILNativeInt:
            case ILUnmanagedPointer:
                {
                    return new ILUnmanagedDeref(instance, type);
                }
            default:
                throw new Exception("no way");
        }
    }
}

class ILManagedRef(ILExpr value) : ILRefExpr
{
    public ILExpr Value => value;

    public ILType Type => new ILManagedPointer(Value.Type);

    public override string ToString()
    {
        return "&" + Value.ToString();
    }
}

class ILUnmanagedRef(ILExpr value) : ILRefExpr
{
    public ILExpr Value => value;

    public ILType Type => new ILUnmanagedPointer(Value.Type);

    public override string ToString()
    {
        return "&" + Value.ToString();
    }
}

class ILManagedDeref(ILExpr byRefVal, ILType resType) : ILDerefExpr
{
    private ILExpr Value = byRefVal;
    public ILType Type => resType;
    public override string ToString()
    {
        return "*" + Value.ToString();
    }
}

class ILUnmanagedDeref(ILExpr pointedVal, ILType resType) : ILDerefExpr
{
    private ILExpr Value = pointedVal;
    public ILType Type => resType;
    public override string ToString()
    {
        return "*" + Value.ToString();
    }
}

class ILStackAlloc(ILExpr size) : ILExpr
{
    public ILType Type => new ILUnmanagedPointer(new ILNull());

    public ILExpr Size = size;

    public override string ToString()
    {
        return "stackalloc " + Size.ToString();
    }
}