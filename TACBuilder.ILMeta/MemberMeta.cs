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
