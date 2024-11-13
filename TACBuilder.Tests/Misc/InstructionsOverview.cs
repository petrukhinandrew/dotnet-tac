using System.Numerics;
using System.Runtime.CompilerServices;

#pragma warning disable CS0219
#pragma warning disable CS8500
#pragma warning disable

namespace Usvm.IL.Test.Instructions;

[AttributeUsage(AttributeTargets.All)]
public class CustomAttribute(string value) : Attribute
{
    public string Value { get; } = value;
    public int[] Array { get; set; }
}

[AttributeUsage(AttributeTargets.Method)]
public class ApproxAttribute(string typeName, string methodName) : Attribute
{
    public string MethodName { get; } = methodName;
    public string TypeName { get; } = typeName;
}

public enum ForAttr
{
    None = 0,
    Some = 1,
    All = 2
}

[AttributeUsage(AttributeTargets.All)]
public class WithEnumAttribute(ForAttr forAttr, Type approxType) : Attribute
{
    public ForAttr Another { get; set; }
}

[Custom("lolkek", Array = [1, 2])]
[WithEnum(ForAttr.Some, typeof(WithEnumAttribute), Another = ForAttr.All)]
public class CustomAttrUsage
{
    [Approx("lolType", "kekMethod")]
    public void MethodExample()
    {
        var e = ForAttr.Some;
        if (Enum.IsDefined(typeof(ForAttr), e))
        {
            var another = ForAttr.All;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryConvertFromSaturating<TOther>(TOther value, out int result)
        where TOther : INumberBase<TOther>
    {
        if (typeof(TOther) == typeof(double))
        {
            double num = (double)(object)value;
            result = num >= (double)int.MaxValue
                ? int.MaxValue
                : (num <= (double)int.MinValue ? int.MinValue : (int)num);
            return true;
        }

        result = 0;
        return false;
    }
}

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

    class CastClassC : CastClassB
    {
    }

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

interface ConditionTestInterface
{
    void Do();
}

struct ConditionTestStruct1 : ConditionTestInterface
{
    public void Do()
    {
    }
}

class ConditionTestClass1 : ConditionTestInterface
{
    public void Do()
    {
    }
}

struct ConditionTestStruct2 : ConditionTestInterface
{
    public void Do()
    {
    }
}

class ConditionTestClass2 : ConditionTestInterface
{
    public void Do()
    {
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

    public static void TernaryOp()
    {
        int a = 1;
        int b = 2;
        string s = (a + b < a * b) ? "true" : "false";
    }

    public static void TernaryOpWithComplexRessult()
    {
        int a = 1;
        int b = 2;
        string s = "string";
        int c = s.Length < a + b ? b - a : a - b;
    }

    public static void TernaryOpWithInterfaceResult()
    {
        int a = 1;
        int b = 2;
        ConditionTestInterface structRes = a > b ? new ConditionTestStruct1() : new ConditionTestStruct2();
        structRes.Do();
        ConditionTestInterface classRes = a > b ? new ConditionTestClass1() : new ConditionTestClass2();
        classRes.Do();
    }

    public static void Loops()
    {
        int i = 1 + 4;
        int n = i + 2;
        while (++i < 5)
        {
            n += i;
        }

        for (int j = 0; j < n; j++)
        {
            i--;
        }
    }

    public static int MultipleReturnsWithLoops()
    {
        int i = 1 + 4;
        int n = i + 2;
        while (++i < 5)
        {
            n += i;
            if (n == -1) return 0;
        }

        if (n > 30) return 2;
        for (int j = 0; j < n; j++)
        {
            i--;
            if (i < 0) return -1;
        }

        return 1;
    }

    public static int SwitchTable()
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

    public static void CharMethod(char x)
    {
        char z = 'a';
        z = (char)(x + z);
        Console.WriteLine(z);
    }

    public static string SwitchExpr()
    {
        string s = "abc";
        string caps = s switch
        {
            "cde" => "EDC",
            "cba" => "ABC",
            _ => "OTHER"
        };
        return caps;
    }

    public static int Comps()
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
                return a;
            }

            return b;
        }

        return c;
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
    public static void Test1()
    {
        TestStruct instance;
    }

    public static void ByValue()
    {
        TestEnum[] te = [TestEnum.A, TestEnum.B, TestEnum.C, TestEnum.C];
        var teValue = te[0];
        TestStruct ts = new() { A = 1, B = 2, C = 3, E = teValue };
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
        TestEnum[] enumArr = [TestEnum.A, TestEnum.B, TestEnum.C];
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
    unsafe struct LocalUnsafeStruct;

    public static void kek<T>()
    {
    }

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

    public class ArgRefTestStruct
    {
        public int x = 1;
    }

    public static void ArgRefUsage()
    {
        var s = new ArgRefTestStruct();
        var b = ArgRef(1, out var res, "lolkek", s);
        var bb = ArgRef(1, out s.x, "lolkek", s);
    }

    public static bool ArgRef(int x, out int res, in string kek, in ArgRefTestStruct arts)
    {
        x += 1;
        x += (arts.x += 1);
        res = x + kek.Length;
        return res % 2 == 0;
    }

    public static void StructArrayRef(ref TestStruct[] arr)
    {
        arr[1] = new TestStruct() { A = 5 };
    }

    public class LdelemaClass(LdelemaClass? a = null)
    {
        public LdelemaClass? Value => a;
    }

    public static void LdelemA(ref LdelemaClass[] table, int minLength)
    {
        LdelemaClass[] newTable = new LdelemaClass[minLength];
        LdelemaClass locker = new LdelemaClass();
        //
        // The lock is necessary to avoid a race with ThreadLocal.Dispose. GrowTable has to point all
        // LinkedSlot instances referenced in the old table to reference the new table. Without locking,
        // Dispose could use a stale SlotArray reference and clear out a slot in the old array only, while
        // the value continues to be referenced from the new (larger) array.
        //
        lock (locker)
        {
            for (int i = 0; i < table.Length; i++)
            {
                LdelemaClass? linkedSlot = table[i].Value;
                // if (linkedSlot != null && linkedSlot._slotArray != null)
                // {
                //     linkedSlot._slotArray = newTable;
                newTable[i] = table[i];
                // }
            }
        }

        table = newTable;
    }

    public static void objArr(ref object?[] args)
    {
        foreach (var o in args)
        {
            Console.WriteLine(o);
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
    public static void LeaveFromTry()
    {
        try
        {
            throw new Exception("Lolkek");
        }
        catch (Exception)
        {
            int a = 1;
            return;
        }
        finally
        {
            Console.WriteLine("finally");
        }
    }

    public static void ThrowRethrow()
    {
        try
        {
            throw new NullReferenceException("text");
        }
        catch (NullReferenceException)
        {
            throw;
        }
        catch (Exception)
        {
        }
    }

    public static void NestedTryCatch()
    {
        int t = 1;
        try
        {
            string s = "try";
            if (s != "try") throw new Exception("e");
        }
        catch (NullReferenceException nre)
        {
            string s = "catch 1 with " + nre;
            try
            {
                string ss = "try in catch";
                int zero = 0;
                float x = 7 / zero;
            }
            catch (DivideByZeroException dbze)
            {
                string ss = "catch in catch with " + dbze;
                int x = 1;
            }

            return;
        }
        catch (Exception e) when (t == 1)
        {
            string s = "filter";
        }
        finally
        {
            string s = "finally";
        }
    }

    public static void Filter()
    {
        int a = 1;
        try
        {
            int zero = 0;
            int res = 7 / zero;
        }
        catch (DivideByZeroException e) when (a == 1)
        {
            Console.WriteLine(e.Message);
        }
    }

    public static void NoCatch()
    {
        try
        {
        }
        finally
        {
        }
    }

    public static void CatchExceptionUsage()
    {
        try
        {
            int zero = 0;
            int res = 7 / zero;
        }
        catch (DivideByZeroException e)
        {
            Console.WriteLine(e.Message);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.StackTrace);
        }
    }

    public static void MultipleFinally()
    {
        try
        {
            try
            {
                string tt = "try in try";
                return;
                string dc = "dead code";
            }
            catch (Exception)
            {
                string c = "catch";
                return;
            }
            finally
            {
                string ff = "first finally";
            }
        }
        finally
        {
            string sf = "second finally";
        }
    }

    public static void TryEndingWithCatch()
    {
        try
        {
            int a = 1;
            try
            {
                int b = 2;
            }
            catch (Exception)
            {
                int c = 3;
            }
        }
        finally
        {
        }
    }

    public static void Fault()
    {
        try
        {
            string s = "try";
        }
        catch (Exception e)
        {
            string c = "catch";
        }
        catch
        {
            string f = "fault";
        }
    }

    public static void BranchInFinally()
    {
        try
        {
        }
        catch
        {
            return;
        }
        finally
        {
        }
    }
}

static unsafe class Misc
{
    public static void Lambda()
    {
        int[] arr = { 1, 2, 3 };
        string[] arr_s = arr.Select(e => e > 2 ? e.ToString() : "0").ToArray();
    }

    public static void FuncPtrs()
    {
    }

    public static (int, int) TupleRet()
    {
        (int, int) tuple = (0, 1);
        return tuple;
    }

    public static void RawGotoExample()
    {
        int a = 1;
        goto TargetX;

        int b = 2;
        TargetX:
        int c = 3;
    }

    public static void ByteAndInt()
    {
        byte b = 2;
        int a = 1 + b;
        long c = 2 + a;
    }

    public static void FinallyGoto()
    {
        try
        {
            throw new Exception("lolkek");
        }
        catch (Exception)
        {
            goto LabelX;
        }
        finally
        {
            Console.WriteLine("another kek");
        }

        int a = 1;
        LabelX:
        int b = 1;
    }

    public static int SampleFunc()
    {
        return 1;
    }

    public static void SizeOf()
    {
        int i = sizeof(Instance);
        int d = sizeof(double);
        int b = sizeof(bool);
        int s = sizeof(TestStruct);
    }

    public static void VarArg(params int[] kek)
    {
        int x = 0;
        foreach (var lol in kek)
        {
            x += lol;
        }
    }

    public static void VarArgCall()
    {
        VarArg(1, 2, 3, 4);
    }

    public delegate void TestDelegate();

    public static void Ldvirtftn()
    {
        InstanceChild child = new();
        TestDelegate testDelegate = child.Do;
        testDelegate();
    }

    public static void CallIndirect(Action action, Func<int, Func<double, int>> complexMethod)
    {
        action();
        int res = complexMethod(1)(2.0);
    }
}

static unsafe class Fields
{
    class Sample
    {
        public static int A = 1;
        public int B = 1;
    }

    static void StaticFieldLoad()
    {
        int x = Sample.A + 1;
        ref int Aref = ref Sample.A;
        Aref += 1;
    }

    static void InstanceFieldLoad()
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

    static void FieldStore()
    {
        Sample s = new Sample();
        s.B += 1;
        Sample.A += 2;
    }
}

static class CallTests
{
    internal class CallTestInstance
    {
        public int IntMethod(int x)
        {
            return x;
        }

        public void VoidMethod(string s)
        {
        }

        public static void StaticMethod(CallTestInstance instance, int a, string b)
        {
        }

        public static int ManyArgs(int a1, int a2, int a3, int a4, string s)
        {
            return a1 + a2 + a3 + a4 + s.Length;
        }
    }

    public static void DifferentCalls()
    {
        var instance = new CallTestInstance();
        instance.IntMethod(1);
        instance.VoidMethod("void");
        CallTestInstance.StaticMethod(instance, 1, "void");
        CallTestInstance.ManyArgs(1, 2, 3, 4, "1234");
    }
}

static class GenericUsage
{
    public static void ListTest()
    {
        List<int> list = new();
        list.Add(1);
        list.AddRange([1, 2]);
        List<List<int>> list2 = new();
        list2.Add(list);
    }
}

public class Kek
{
    private int lol;

    public Kek(int x)
    {
        lol = x;
    }
}
