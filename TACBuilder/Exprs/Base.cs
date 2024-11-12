using System.Reflection;
using System.Runtime.CompilerServices;
using JetBrains.Lifetimes;
using TACBuilder.ILReflection;
using TACBuilder.Utils;

namespace TACBuilder.Exprs;

public interface IlExpr
{
    IlType Type { get; }

    public string ToString();
}

public interface IlValue : IlExpr;

public interface IlLocal : IlValue;

public interface IlVar : IlLocal
{
    public IlType Type { get; }
    public IlExpr? Value { get; }
}

public class IlLocalVar(IlType type, int index, bool isPinned, IlExpr? value = null) : IlVar
{
    public new IlType Type => type;
    public IlExpr? Value { get; set; } = value;
    public int Index => index;
    public new string ToString() => NamingUtil.LocalVar(index);
    public bool IsPinned => isPinned;

    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return obj is IlLocalVar loc && loc.ToString() == ToString();
    }
}

public class IlTempVar(int index, IlExpr value) : IlVar
{
    public int Index => index;
    public IlExpr Value => value;
    public IlType Type => value.Type;

    public override string ToString()
    {
        return NamingUtil.TempVar(index);
    }
}

public class IlErrVar(IlType type, int index) : IlVar
{
    public int Index => index;
    public new IlType Type => type;
    public IlExpr? Value { get; }

    public override string ToString()
    {
        return NamingUtil.ErrVar(index);
    }
}

public class IlArgument(IlMethod.IParameter parameter) : IlLocal
{
    public IlType Type => parameter.Type;
    public int Index => parameter.Position;
    public new string ToString() => parameter.Name ?? NamingUtil.ArgVar(parameter.Position);
}

public class IlMerged(string name) : IlValue
{
    private string _name = name;
    private IlType? _type;
    public int Index = -1;
    public IlType Type => _type;

    public void MergeOf(List<IlExpr> exprs)
    {
        _type = TypingUtil.Merge(exprs.Select(e => e.Type).ToList());
    }

    public void MakeTemp(IlTempVar ilTemp)
    {
        _name = ilTemp.ToString();
        Index = ilTemp.Index;
    }

    public override string ToString()
    {
        return _name;
    }

    public override bool Equals(object? obj)
    {
        return obj is IlMerged m && m.ToString() == ToString();
    }

    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }
}

public interface IlConstant : IlValue;

public interface IlNumericConstant : IlConstant;

public class IlByteConst(byte value) : IlNumericConstant
{
    public IlType Type => IlInstanceBuilder.GetType(typeof(byte));
    public byte Value => value;
}

public class IlIntConst(int value) : IlNumericConstant
{
    public IlType Type => IlInstanceBuilder.GetType(typeof(int));
    public int Value => value;
}

public class IlLongConst(long value) : IlNumericConstant
{
    public IlType Type => IlInstanceBuilder.GetType(typeof(long));
    public long Value => value;
}

public class IlFloatConst(float value) : IlNumericConstant
{
    public IlType Type => IlInstanceBuilder.GetType(typeof(float));
    public float Value => value;
}

public class IlDoubleConst(double value) : IlNumericConstant
{
    public IlType Type => IlInstanceBuilder.GetType(typeof(double));
    public double Value => value;
}

public class IlStringConst(string value) : IlConstant
{
    public IlType Type => IlInstanceBuilder.GetType(typeof(string));
    public string Value => value;
}

public class IlNullConst : IlConstant
{
    public IlType Type => IlInstanceBuilder.GetType(typeof(object));
}

public class IlBoolConst(bool value) : IlConstant
{
    public IlType Type => IlInstanceBuilder.GetType(typeof(bool));
    public bool Value => value;
}

public class IlEnumConst(IlEnumType enumType, IlConstant underlyingValue) : IlConstant
{
    public IlType Type => enumType;
    public IlConstant Value => underlyingValue;
}

public class IlArrayConst(IlArrayType arrayType, IEnumerable<IlConstant> values) : IlConstant
{
    public IlType Type => arrayType;
    public List<IlConstant> Values => values.ToList();
}

public class IlTypeRef(IlType type) : IlConstant
{
    public IlType ReferencedType => type;
    public IlType Type => IlInstanceBuilder.GetType(typeof(Type));
}

public class IlFieldRef(IlField field) : IlConstant
{
    public IlField Field { get; } = field;
    public IlType Type => IlInstanceBuilder.GetType(typeof(FieldInfo));
}

public class IlArgListRef(IlMethod method) : IlConstant
{
    public IlType Type => IlInstanceBuilder.GetType(typeof(RuntimeArgumentHandle));
}

// TODO check receiver used
public class IlMethodRef(IlMethod method, IlExpr? receiver = null) : IlConstant
{
    public IlType Type => IlInstanceBuilder.GetType(typeof(MethodBase));
    public IlMethod Method => method;
}

public class IlCallIndirect(IlSignature signature, IlExpr ftn, List<IlExpr> args) : IlExpr
{
    public IlType? Type => signature.ReturnType;
    public IlExpr Callee => ftn;
    public List<IlExpr> Arguments => args;
    public override string ToString()
    {
        return $"calli {ftn.ToString()} ({string.Join(",", Arguments.Select(a => a.ToString()))})";
    }
}

public class IlCall(IlMethod method) : IlExpr
{
    public IlMethod Method => method;

    public void LoadArgs(Func<IlExpr> pop)
    {
        foreach (var t in Method.Parameters)
        {
            Args.Add(pop().WithTypeEnsured(t.Type));
            // RuntimeHelpers.InitializeArray();
        }

        Args.Reverse();
    }

    public string Name => Method.Name;

    // TODO ctor has no return type
    public IlType ReturnType => Method.ReturnType ?? IlInstanceBuilder.GetType(typeof(void));
    public List<IlExpr> Args = new();
    public IlType Type => ReturnType;

    public override string ToString()
    {
        string genericExtra =
            Method.IsGeneric ? $"<{string.Join(", ", Method.GenericArgs.Select(a => a.ToString()))}>" : "";

        if (Method.IsStatic)
            return string.Format("{0} {1}{3}({2})", ReturnType, Name,
                string.Join(", ", Args.Select(p => p.ToString())), genericExtra);

        return string.Format("{0}{1}({2})", Name, genericExtra,
            string.Join(", ", Args.Select(p => p.ToString())));
    }

    public bool IsInitializeArray()
    {
        return method.DeclaringType.Name.Contains("RuntimeHelpers") && Name == "InitializeArray";
    }

    public bool Returns()
    {
        return !Equals(ReturnType, IlInstanceBuilder.GetType(typeof(void)));
    }

    public override bool Equals(object? obj)
    {
        return obj is IlCall m && m.Method == Method;
    }

    public override int GetHashCode()
    {
        return Method.GetHashCode();
    }
}
