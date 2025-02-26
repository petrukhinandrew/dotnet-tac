using TACBuilder.ILReflection;
using TACBuilder.Utils;

namespace TACBuilder.Exprs;

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

public class IlMerged(string name) : IlSimpleValue
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