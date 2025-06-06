using TACBuilder.ILReflection;
using TACBuilder.Utils;

namespace TACBuilder.Exprs;

public class IlCallIndirect(IlSignature signature, IlExpr ftn, List<IlExpr> args) : IlExpr
{
    public IlSignature Signature => signature;
    public IlType Type => Signature.ReturnType;
    public IlExpr Callee => ftn;
    public List<IlExpr> Arguments => args;

    public override string ToString()
    {
        return $"calli {ftn.ToString()} ({string.Join(",", Arguments.Select(a => a.ToString()))})";
    }
}

public class IlCall(IlMethod method, List<IlExpr> args) : IlExpr
{
    public class Argument(IlMethod.IParameter parameter) : IlLocal
    {
        public IlType Type => parameter.Type;
        public int Index => parameter.Position;
        public new string ToString() => parameter.Name ?? NamingUtil.ArgVar(parameter.Position);
    }

    public IlMethod Method => method;

    public string Name => Method.Name;
    public IlType ReturnType => Method.ReturnType!;
    public List<IlExpr> Args => args;
    public IlType Type => ReturnType;

    public override string ToString()
    {
        string genericExtra =
            Method.IsGeneric ? $"<{string.Join(", ", Method.GenericArgs.Select(a => a.ToString()))}>" : "";

        if (Method.IsStatic)
            return $"{ReturnType} {Name}{genericExtra}({string.Join(", ", Args.Select(p => p.ToString()))})";

        return $"{Name}{genericExtra}({string.Join(", ", Args.Select(p => p.ToString()))})";
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