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

    public static short SourceShort()
    {
        return 123;
    }
    
    public static void Sink(string s)
    {
        Console.WriteLine(s);
    }

    public static void SinkShort(short s)
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

    public new string Get()
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

class PrimitiveArgCoercion
{
    public short Source()
    {
        return 123;
    }

    public void Sink(int i)
    {
        Console.WriteLine(i);
    }
    
    public void Check()
    {
        var s = Source();
        Sink(s);
    }
}



public class StructToInterfaceCastTest
{
    public interface TestInterfafce;

    public struct TestStruct : TestInterfafce;

    public void Call(TestInterfafce i)
    {
        
    }
    
    [Fact]
    public void SimpleCast()
    {
        var s = new TestStruct();
        Call(s);
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

public interface InterfaceMethodLookupBase
{
    public void Base();
}

public interface InterfaceMethodLookup : InterfaceMethodLookupBase
{
    public void ChildMethod();
}

public interface InterfaceAnotherBranch
{
    public void AnotherBranch();
}

public class TestExample : InterfaceMethodLookup, InterfaceAnotherBranch
{
    public void Base()
    {
        
    }

    public void ChildMethod()
    {
        
    }

    public void AnotherBranch()
    {
    }
}