using TACBuilder.ILReflection;

namespace TACBuilder.Exprs;

public interface IlExpr
{
    IlType Type { get; }

    public string ToString();
}

public interface IlValue : IlExpr;

public interface IlSimpleValue : IlValue;

public interface IlComplexValue : IlValue;

public interface IlLocal : IlSimpleValue;

public interface IlVar : IlLocal
{
    public new IlType Type { get; }
    public IlExpr? Value { get; }
}