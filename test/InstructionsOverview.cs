#pragma warning disable CS0219


using System.Runtime.InteropServices;

namespace Usvm.IL.Test.Instructions;

public class OpsTest
{
    public static int Calculations()
    {
        int x = 1;
        int y = 2;
        int z = 3;
        int res = (x * y) + (y * z);
        return res;
    }
    public static int AddOne(int x)
    {
        return x + 1;
    }
    public static double AddTwo(double x)
    {
        return x + 2;
    }
    public static bool Neg(bool v) => !v;
    public static int Not(int v) => -v;
}

static class CastTests
{
    public static void Conv()
    {
        int x = 1;
        byte y = (byte)x;
        short z = y;
        uint yx = (uint)x;
    }
    abstract class CastClassA
    {
        public string value = "abc";
    }
    class CastClassB : CastClassA
    {
        public new string value = "123";
    }
    class CastClassC : CastClassB { }

    public static void CastClass()
    {
        CastClassB b = new();
        CastClassC c = (CastClassC)b;
        CastClassC? nc = b as CastClassC;
        bool bIsA = b is CastClassC;
    }
    public static void Boxing()
    {
        int a = 1;
        object b = a;
        int c = (int)b;
    }
}

static class ConditionsTests
{
    public static string Concat(string s1, string s2, string s3)
    {
        string res = s1 + s2;
        res += s3;
        return res;
    }
    public static int SwitchExample()
    {
        string s = Concat("a", "b", "c");
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
    public static int IfExample()
    {
        string s = Concat("a", "b", "c");
        if (s == "a")
            return 1;
        else if (s == "b")
            return 2;
        return 3;
    }
}

static class NewInstTests
{
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
    class Instance(int tx = 1)
    {
        private int x = tx;

        public void Do()
        {
            x += 1;
        }
    }
    public static void ByValue()
    {
        TestEnum te = TestEnum.A;
        TestStruct ts = new() { A = 1, B = 2, C = 3 };
        (int, int) tt = (1, 1);
        ts.A += tt.Item1;
        ts.C += tt.Item2;
        (int, Instance) tnv = (1, new Instance());
    }
    public static void Literals()
    {
        string lol = "abc";
        int[] kek = { 1, 2, 3 };

        Instance[] wtf = [new Instance(), new Instance(2), new Instance(kek[1])];
        int kl = kek.Length;
    }
    public static (int, int) Tuple()
    {
        return new(0, 0);
    }
    public static void NewInstTest()
    {
        Instance inst = new(1);
        inst.Do();
    }
}
static class UnsafeTest
{
    public static void InitBlk()
    {
        Marshal.AllocHGlobal(1123);
    }
    struct CpBlkStruct { int a; int b; };

    public static void CpBlk()
    {
        CpBlkStruct v = new();
        CpBlkStruct v_copy = v;

        // unsafe
        // {
        //     var c = new ExplicitClass(1, 2);
        //     byte result;
        //     fixed (int* ptr = &c.x)
        //     {
        //         var ptr2 = (byte*)ptr;
        //         var ptr3 = ptr2 + i;
        //         result = *ptr3;
        //     }
        //     bool res = (i == -1 && result != 0);
        // }
    }
    public static void Deref()
    {
        unsafe
        {

            int x = 1;
            int* rx = &x;
            int y = *rx;
            ref int kek = ref x;
            int t = kek;
        }
    }
}