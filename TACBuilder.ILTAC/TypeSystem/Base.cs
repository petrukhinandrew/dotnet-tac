using System.Reflection;
using TACBuilder.ILMeta;

namespace TACBuilder.ILTAC.TypeSystem;

public interface ILType
{
    public Type ReflectedType { get; }
}

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
    private static ILType _instance = new ILNull();
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

public class ILVarArgValue(string methodName) : ILValue
{
    private static readonly ILType _instance = new ILHandleRef();
    public ILType Type => _instance;
    public override string ToString() => methodName + ".__arglist";
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

public class ILMergedType : ILType
{
    private List<ILType> _types = new();
    private ILType? _cache;
    public Type ReflectedType => Merge().ReflectedType;

    public ILType Merge()
    {
        return _cache ??= TypingUtil.Merge(_types);
    }

    public void OfTypes(List<ILType> types)
    {
        _types = types;
        _cache = null;
    }
}

public class ILMerged(string name) : ILLValue
{
    private string _name = name;
    private readonly ILMergedType _type = new();
    public ILType Type => _type.Merge();

    public void MergeOf(List<ILExpr> exprs)
    {
        _type.OfTypes(exprs.Select(e => e.Type).ToList());
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

public class ILObjectLiteral(ILType type, object? obj) : ILValue
{
    public object? Object = obj;

    public ILType Type => type;

    public override string ToString()
    {
        return Type + " obj";
    }
}

public class ILLiteral(ILType type, string value) : ILValue
{
    public ILType Type => type;

    public new string ToString() => type switch
    {
        ILString => $"\"{value}\"",
        _ => value
    };

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
public class ILMethod(MethodMeta meta) : ILExpr
{
    private readonly MethodMeta _meta = meta;

    public ILExpr Receiver = new ILNullValue();
    public string DeclaringType = meta.DeclaringType.Name;
    public string Name = meta.Name;


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

    public bool IsGeneric => GenericArgs.Count > 0;
    public List<ILType> GenericArgs = new();
    public ILType ReturnType = TypingUtil.ILTypeFrom(meta.ReturnType?.Type);
    public List<ILExpr> Args = new List<ILExpr>();
    public ILType Type => ReturnType;

    public override string ToString()
    {
        string genericExtra =
            IsGeneric ? $"<{string.Join(", ", GenericArgs.Select(a => a.ToString()))}>" : "";
        if (Receiver is ILNullValue)
            return string.Format("{0} {1}{3}({2})", ReturnType.ToString(), Name,
                string.Join(", ", Args.Select(p => p.ToString())), genericExtra);
        return string.Format("{0} {1}.{2}{4}({3})", ReturnType.ToString(), Receiver.ToString(), Name,
            string.Join(", ", Args.Select(p => p.ToString())), genericExtra);
    }

    public bool IsInitializeArray()
    {
        return DeclaringType == "System.Runtime.CompilerServices.RuntimeHelpers" && Name == "InitializeArray";
    }

    public bool Returns()
    {
        return ReturnType is not ILVoid && ReturnType is not ILNull;
    }

    public override bool Equals(object? obj)
    {
        return obj is ILMethod m && m._meta == _meta;
    }

    public override int GetHashCode()
    {
        return _meta.GetHashCode();
    }
}

public class ILField(ILType type, string declType, string name, bool isStatic, int token) : ILLValue
{
    public static ILField Static(FieldMeta f)
    {
        return new ILField(TypingUtil.ILTypeFrom(f.Type!.BaseType), f.DeclaringType!.Name, f.Name, true,
            f.MetadataToken);
    }

    public static ILField Instance(FieldMeta f, ILExpr inst)
    {
        ILField field = new ILField(TypingUtil.ILTypeFrom(f.Type!.BaseType), f.DeclaringType!.Name, f.Name, false,
            f.MetadataToken)
        {
            Receiver = inst
        };
        return field;
    }

    public string DeclaringType = declType;
    public string Name = name;
    public bool IsStatic = isStatic;
    public ILExpr? Receiver;
    public ILType Type => type;
    private int _metadataToken = token;

    public override string ToString()
    {
        if (!IsStatic && Receiver == null) throw new Exception("instance field with null receiver");
        return IsStatic switch
        {
            true => $"{DeclaringType}.{Name}",
            false => $"{Receiver!.ToString()}.{Name}"
        };
    }

    public override bool Equals(object? obj)
    {
        return obj is ILField f && f._metadataToken == _metadataToken;
    }

    public override int GetHashCode()
    {
        return _metadataToken.GetHashCode();
    }
}
