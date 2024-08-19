#pragma warning disable CS0219
#pragma warning disable CS8500

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

    public static void InitObj()
    {
        CastClassC c = new();
        TestStruct t = new();
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
        int x = 1 + 2 + 4;
        switch (x + 1)
        {
            case 7:
            case 8: return 1;
            case 10: return 2;
            case 13: return 3;
            default: return x;
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
    public TestEnum E;
}
class Instance(int tx = 1)
{
    private int x = tx;

    public virtual void Do()
    {
        x += 1;
    }
}
class InstanceChild() : Instance(2)
{
    public override void Do()
    {

    }
}
static class NewInstTests
{
    public static void ByValue()
    {
        TestEnum te = TestEnum.A;
        TestStruct ts = new() { A = 1, B = 2, C = 3, E = te };
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
static unsafe class UnsafeTest
{
    unsafe struct LocalUnsafeStruct { }
    public static void LdStObj()
    {
        LocalUnsafeStruct a, b;
        LocalUnsafeStruct* ptr;
        ptr = &b;
        *ptr = a;
        ref LocalUnsafeStruct r = ref a;
        b = r;
    }

    public static void SafeCopy(int[] source, int sourceOffset, int[] target,
            int targetOffset, int count)
    {
        for (int i = 0; i < count; i++)
        {
            target[targetOffset + i] = source[sourceOffset + i];
        }
    }

    public static void UnsafeCopy(int[] source, int sourceOffset, int[] target,
        int targetOffset, int count)
    {

        fixed (int* pSource = source, pTarget = target)
        {
            byte* pSourceByte = (byte*)pSource;
            byte* pTargetByte = (byte*)pTarget;
            var sourceOffsetByte = sourceOffset * sizeof(int);
            var targetOffsetByte = targetOffset * sizeof(int);
            // Copy the specified number of bytes from source to target.
            for (int i = 0; i < count * sizeof(int); i++)
            {
                pTargetByte[targetOffsetByte + i] = pSourceByte[sourceOffsetByte + i];
            }
        }

    }
    public static void PointerAndRef()
    {
        unsafe
        {
            int x = 1;
            int* x_ptr = &x;
            ref int x_ref = ref x;
            *x_ptr += 1;
            x_ref += 1;
            Instance i = new Instance();

            Instance* i_ptr = &i;
            i_ptr->Do();
        }
    }
    public static void ArrayRef()
    {
        unsafe
        {
            int[] arr = { 1, 2, 3 };
            ref int fst = ref arr[0];
            fst += 1;
        }
    }
    public static void StackAlloc()
    {
        int length = 10;
        Span<byte> bytes;
        byte* tmp = stackalloc byte[length];
        bytes = new Span<byte>(tmp, length);
        Span<byte> arg = stackalloc byte[8];
        void* arg1 = stackalloc byte[8];
    }
}

static class TryBlockTests
{
    public static void NestedTryCatch()
    {
        int t = 1;
        try
        {
            if (1 + 2 == 3) return;
        }
        catch (NullReferenceException)
        {
            try
            {
                int zero = 0;
                float x = 7 / zero;
            }
            catch (DivideByZeroException)
            {

            }
            return;
        }
        catch (Exception) when (t == 1)
        {
            return;
        }
        finally
        {

        }
    }
    public static void Filter()
    {
        int a = 1;
        try
        {
            int x = 1;
        }
        catch (DivideByZeroException)
        {
            int y = 1;
        }
        catch (Exception)
        {

        }
    }
}

static unsafe class Misc
{
    public static void SizeOf()
    {
        int i = sizeof(Instance);
        int d = sizeof(double);
        int b = sizeof(bool);
        int s = sizeof(TestStruct);
    }
    public static void ArgList(__arglist)
    {
        ArgIterator args = new ArgIterator(__arglist);
    }

    public delegate void TestDelegate();
    public static void Ldvirtftn()
    {
        InstanceChild child = new();
        TestDelegate testDelegate = child.Do;
        testDelegate();
    }
}

static unsafe class Fields
{
    class Sample
    {
        public static int A = 1;
        public int B = 1;
    }
    static void StaticField()
    {
        int x = Sample.A + 1;
        ref int Aref = ref Sample.A;
        Aref += 1;
    }
    static void InstanceField()
    {
        Sample s = new Sample();
        int x = 1;
        fixed (int* ptr = &s.B)
        {
            x += *ptr;
        }
        ref int r = ref s.B;
        x += r;
    }
}