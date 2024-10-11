using System.Reflection;

namespace TACBuilder.ILReflection;

public abstract class ILMember(MemberInfo memberInfo) : ILCacheable
{
    private MemberInfo _memberInfo = memberInfo;
    public MemberInfo MemberInfo => _memberInfo;
    public string Name => _memberInfo.Name;

    public override string ToString()
    {
        return Name;
    }
}

public class UnknownIlMember : ILMember
{
    public UnknownIlMember(MethodBase source) : base(null)
    {
        UnknownIlUtil.DisplayMessage(source);
    }

    public override void Construct()
    {
    }
}

public class UnknownIlType : ILType
{
    public UnknownIlType(MethodBase source) : base(null)
    {
        UnknownIlUtil.DisplayMessage(source);
    }

    public override void Construct()
    {
    }
}

public class UnknownIlMethod : ILMethod
{
    public UnknownIlMethod(MethodBase source) : base(null)
    {
        UnknownIlUtil.DisplayMessage(source);
    }

    public override void Construct()
    {
    }
}

public class UnknownIlField : ILField
{
    public UnknownIlField(MethodBase source) : base(null)
    {
        UnknownIlUtil.DisplayMessage(source);
    }

    public override void Construct()
    {
    }
}

internal abstract class UnknownIlUtil
{
    public static void DisplayMessage(MethodBase source)
    {
        Console.Error.WriteLine("Unknown ILMember at " + source.Name);
    }
}
