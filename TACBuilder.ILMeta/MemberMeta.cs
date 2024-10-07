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
    public UnknownMemberMeta(MethodBase source) : base(null)
    {
        UnknownMetaUtil.DisplayMessage(source);
    }

    public override void Construct()
    {
    }
}

public class UnknownTypeMeta : TypeMeta
{
    public UnknownTypeMeta(MethodBase source) : base(null)
    {
        UnknownMetaUtil.DisplayMessage(source);
    }

    public override void Construct()
    {
    }
}

public class UnknownMethodMeta : MethodMeta
{
    public UnknownMethodMeta(MethodBase source) : base(null)
    {
        UnknownMetaUtil.DisplayMessage(source);
    }

    public override void Construct()
    {
    }
}

public class UnknownFieldMeta : FieldMeta
{
    public UnknownFieldMeta(MethodBase source) : base(null)
    {
        UnknownMetaUtil.DisplayMessage(source);
    }

    public override void Construct()
    {
    }
}

internal class UnknownMetaUtil
{
    public static void DisplayMessage(MethodBase source)
    {
        Console.Error.WriteLine("Unknown MemberMeta at " + source.Name);
    }
}
