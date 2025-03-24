using Xunit.Abstractions;

namespace TACBuilder.Tests.Misc;

public class AParam;

public class BParam : AParam;

public class A(ITestOutputHelper helper)
{
    public AParam Kek(BParam p)
    {
        helper.WriteLine("Akek");
        return new AParam();
    }
}

public class B(ITestOutputHelper helper) : A(helper)
{
    public new virtual BParam Kek(BParam p)
    {
        return new BParam();
    }
}

class SastConfigUtils
{
    public static string Source()
    {
        return "Source";
    }

    public static void Sink(string s)
    {
        Console.WriteLine(s);
    }
}

abstract class AContext
{
    public virtual string Get()
    {
        return SastConfigUtils.Source();
    }

    public abstract string ActualGet();
}

class ContextNoVuln1 : AContext
{
    public override string ActualGet()
    {
        return "";
    }
}

class CallsiteClassResolve
{
    void Resolve(ContextNoVuln1 kek)
    {
        var data = kek.ActualGet();
        SastConfigUtils.Sink(data);
    }
}

public class VirtualCallResolve(ITestOutputHelper helper)
{
    [Fact]
    public void Check()
    {
        var a = new A(helper);
        var b = new B(helper);
        A ab = new B(helper);
        Assert.Equal(typeof(A).GetMethod("Kek")!.GetBaseDefinition(), typeof(B).GetMethod("Kek")!.GetBaseDefinition());
        // a.Kek();
        // b.Kek();
        // ((A)b).Kek();
        // ab.Kek();
    }
}