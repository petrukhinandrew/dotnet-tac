
public class SlavaCases()
{
    public string Source()
    {
        return "unsafe data";
    }

    public string Kek(string arg)
    {
        return "";
    }

    public void Sink(string arg)
    {
    }

    public string Id(string arg)
    {
        var tmp = arg;
        arg = "";
        Kek(arg);
        return tmp;
    }

    public void Test()
    {
        var data = Source();

        var result = Id(data);

        Sink(result);
    }

    public void StrRef(out string arg)
    {
        arg = "kek";
    }

    public struct NullCheckStruct
    {
        public int X;
    }

    public class NullCheckClass;

    public enum NullCheckEnum
    {
        Kek,
        Lol
    }

    [Fact]
    public int NullabilityCheck()
    {
        NullCheckClass unsound = new NullCheckClass();
        NullCheckStruct? mbNull = null;
        mbNull = null;
        mbNull = new NullCheckStruct();
        int[] kek = null;
        return mbNull.Value.X;
    }

    public int TestField = 1;

    public int FieldWorkaround(int a, int b)
    {
        TestField = a + b * 2;
        a = TestField - 1;
        b += a;
        TestField = b - a;
        return TestField;
    }

    public struct StorageStruct(string v)
    {
        public string V = v;
        public string VV = v + "kek";
        public string Get() => V;
    }

    public void WriteStructRef(ref StorageStruct s, string str)
    {
        s.V = str;
        var t = 1;
        ref var x = ref t;
        ManagedAdd(ref x);
        x += 1;
    }

    public void UnexpectedThis()
    {
        var s = new StorageStruct("");
        WriteStructRef(ref s, Source());
        Sink(s.Get());
    }

    public void ManagedAdd(ref int x)
    {
        var xXx = x + 1;
        x += 1;
    }


    [AttributeUsage(AttributeTargets.Method)]
    public class SastMethodTest() : Attribute
    {
        public bool ExpectedVulnerability;
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class SastClassTest : Attribute;

    [SastClassTest]
    public class SaastOverviewTest
    {
        [SastMethodTest(ExpectedVulnerability = true)]
        public void MyFirstTest()
        {
        }

        [SastMethodTest(ExpectedVulnerability = false)]
        public void MySecondTest()
        {
        }
    }


    interface IVuln
    {
        string Get();
    }

    class Vuln : IVuln
    {
        public string Get()
        {
            return "SastConfigUtils.Source()";
        }
    }
}