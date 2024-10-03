using System.Reflection;

namespace TACBuilder.ILMeta;

public abstract class MemberMeta(MemberInfo memberInfo) : CacheableMeta
{
    // thing that may be resolved may resolve itself for cache
    private MemberInfo _memberInfo = memberInfo;
    public int MetadataToken => _memberInfo.MetadataToken;
    public string Name => _memberInfo.Name;
    public MemberMeta Value => this;
}

public class UnknownMemberMeta : MemberMeta
{
    public UnknownMemberMeta() : base(null)
    {
        Console.WriteLine("Unknown MemberMeta");
    }

    public override void Construct()
    {
    }
}

public class UnknownTypeMeta : TypeMeta
{
    public UnknownTypeMeta() : base(null)
    {
        Console.WriteLine("Unknown MemberMeta");
    }

    public override void Construct()
    {
    }
}

public class UnknownMethodMeta : MethodMeta
{
    public UnknownMethodMeta() : base(null)
    {
        Console.WriteLine("Unknown MemberMeta");
    }

    public override void Construct()
    {
    }
}

public class UnknownFieldMeta : FieldMeta
{
    public UnknownFieldMeta() : base(null)
    {
        Console.WriteLine("Unknown MemberMeta");
    }

    public override void Construct()
    {
    }
}
