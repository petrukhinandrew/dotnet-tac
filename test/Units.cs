namespace Usvm.IL.Test.Units;
class ClassTest
{
    private int _x;
    public ClassTest(int x)
    {
        _x = x;
    }
    public void inc()
    {
        _x += 1;
    }
}
public class SampleClass
{

    public static void newInstTest()
    {
        ClassTest ct = new ClassTest(1);
        ct.inc();
    }

    public static string concat(string s1, string s2, string s3)
    {
        string res = s1 + s2;
        res += s3;
        return res;
    }
    public static int switchExample()
    {
        string s = concat("a", "b", "c");
        switch (s)
        {
            case "a": return 1;
            case "b": return 2;
            default: return 3;
        }
    }
    public static void Comps()
    {
        int a = 1;
        a = a + 1 + 2;
        int b = 2;
        int c = 1;
        c = a + b;
        if (b > a)
        {
            if (a == c)
            {
                a += 1;
            }
        }
    }
    public static int ifExample()
    {
        string s = concat("a", "b", "c");
        if (s == "a")
            return 1;
        else if (s == "b")
            return 2;
        return 3;
    }
    public static void lambda()
    {
        Func<int, string, string> x = (int n, string s) =>
        {
            string res = "";
            for (int i = 0; i < n; i++) res += s;
            return res;
        };
        string res = x(3, "abc");
    }
    public static int calculations()
    {
        int x = 1;
        int y = 2;
        int z = 3;
        int res = (x * y) + (y * z);
        return res;
    }
    public static (int, int) tuple()
    {
        return new(0, 0);
    }
    public static int addOne(int x)
    {
        return x + 1;
    }
    public static double addTwo(double x)
    {
        return x + 2;
    }
    public static void testEHC()
    {
        try
        {
            int x = 1;
            x /= 0;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.StackTrace);
        }
        finally
        {
            Console.WriteLine("finally");
        }
    }

    enum TestEnum
    {
        A = 1,
        B = 2,
        C = 3
    }
    struct TestStruct
    {

        public int A;
        public int B;
        public float C;
    }
    class NVExp { }
    public static void ByValue()
    {
        TestEnum te = TestEnum.A;
        TestStruct ts = new TestStruct { A = 1, B = 2, C = 3 };
        (int, int) tt = (1, 1);
        ts.A += tt.Item1;
        ts.C += tt.Item2;
        (int, NVExp) tnv = (1, new NVExp());
    }
    public static void Literals()
    {
        string lol = "abc";
        int[] kek = { 1, 2, 3 };
        NVExp[] wtf = [new NVExp(), new NVExp()];
    }
    public static void Conv()
    {
        int x = 1;
        byte y = (byte)x;
        short z = y;
        uint yx = (uint)x;
    }
}
