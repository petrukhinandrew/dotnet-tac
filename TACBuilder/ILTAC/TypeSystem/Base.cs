using TACBuilder.ILReflection;
using TACBuilder.Utils;

namespace TACBuilder.ILTAC.TypeSystem;

public interface ILExpr
{
    ILType Type { get; }

    public string ToString();
}

public interface ILValue : ILExpr
{
}

public interface ILLValue : ILValue
{
}

public class ILNullValue : ILValue
{
    private static ILType _instance = new(typeof(void));
    public ILType Type => _instance;

    public override string ToString() => "null";

    public override bool Equals(object? obj)
    {
        return obj is ILNullValue;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}

public class ILArgumentHandle(ILMethod method) : ILValue
{
    public ILType Type => new ILType(typeof(RuntimeArgumentHandle));
    public override string ToString() => method.Name + " arglist";
}

public class ILLocal(ILType type, string name) : ILLValue
{
    public ILType Type => type;

    public new string ToString() => name;

    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return obj is ILLocal loc && loc.ToString() == ToString();
    }
}

public class ILMerged(string name) : ILLValue
{
    private string _name = name;
    private ILType? _type;
    public ILType Type => _type;

    public void MergeOf(List<ILExpr> exprs)
    {
        _type = TypingUtil.Merge(exprs.Select(e => e.Type).ToList());
    }

    public void MakeTemp(string newName)
    {
        _name = newName;
    }

    public override string ToString()
    {
        return _name;
    }

    public override bool Equals(object? obj)
    {
        return obj is ILMerged m && m.ToString() == ToString();
    }

    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }
}

public class ILMemberToken(ILMember value) : ILValue
{
    public ILType Type { get; } = new ILType(typeof(int));
    public ILMember Value => value;

    public override string ToString()
    {
        return Value.ToString();
    }
}

public class ILFieldHandle(ILField field, object? obj) : ILValue
{
    public object? Object = obj;

    public ILType Type => field.Type;

    public override string ToString()
    {
        return Type + " obj";
    }
}

public class ILLiteral(ILType type, string value) : ILValue
{
    public ILType Type => type;

    public new string ToString() => value;

    public override bool Equals(object? obj)
    {
        return obj is ILLiteral literal && literal.ToString() == ToString();
    }

    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }
}

// TODO remove MethodInfo access
public class ILCall(ILMethod meta) : ILExpr
{
    public ILCall(ILMethod meta, ILExpr receiver) : this(meta)
    {
        Receiver = receiver;
    }

    private readonly ILMethod _meta = meta;
    private ILExpr Receiver = new ILNullValue();

    public void LoadArgs(Func<ILExpr> pop)
    {
        for (int i = 0; i < _meta.Parameters.Count; i++)
        {
            Args.Add(pop());
        }

        if (_meta.HasThis)
        {
            Receiver = Args.Last();
            Args.RemoveAt(Args.Count - 1);
        }

        Args.Reverse();
    }

    public ILType DeclaringType => meta.DeclaringType;
    public string Name => meta.Name;
    public ILType ReturnType => meta.ReturnType;
    public List<ILExpr> Args = new();
    public ILType Type => ReturnType;

    public override string ToString()
    {
        string genericExtra =
            _meta.IsGeneric ? $"<{string.Join(", ", _meta.GenericArgs.Select(a => a.ToString()))}>" : "";

        if (_meta.IsStatic)
            return string.Format("{0} {1}{3}({2})", ReturnType, Name,
                string.Join(", ", Args.Select(p => p.ToString())), genericExtra);

        return string.Format("{0}.{1}{3}({2})", Receiver.ToString(), Name,
            string.Join(", ", Args.Select(p => p.ToString())), genericExtra);
    }

    public bool IsInitializeArray()
    {
        return DeclaringType.Name.Contains("RuntimeHelpers") && Name == "InitializeArray";
    }

    public bool Returns()
    {
        return ReturnType is not null;
    }

    public override bool Equals(object? obj)
    {
        return obj is ILCall m && m._meta == _meta;
    }

    public override int GetHashCode()
    {
        return _meta.GetHashCode();
    }
}

public class ILFieldAccess(ILField field, ILExpr? instance = null) : ILLValue
{
    private ILField _field = field;
    public ILType DeclaringType => field.DeclaringType;
    public string Name => field.Name;
    public bool IsStatic => field.IsStatic;
    public ILExpr? Receiver => instance;
    public ILType Type => field.Type;

    public override string ToString()
    {
        if (!IsStatic && Receiver == null) throw new Exception("instance ilField with null receiver");
        return IsStatic switch
        {
            true => $"{DeclaringType.Name}.{Name}",
            false => $"{Receiver!.ToString()}.{Name}"
        };
    }

    public override bool Equals(object? obj)
    {
        return obj is ILFieldAccess f && _field == f._field;
    }

    public override int GetHashCode()
    {
        return _field.GetHashCode();
    }
}
