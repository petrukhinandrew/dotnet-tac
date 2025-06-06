using Xunit.Abstractions;

namespace TACBuilder.Tests.Issues;

public struct MyStruct
{
    public int z;
    public int y;
}

public class RomaCases(ITestOutputHelper testOutputHelper)
{
    public struct MyStruct
    {
        public int x;
        public int y;
    }

    public int ZeroForNull(int? v)
    {
        return v ?? 0;
    }

    public void Minus2()
    {
        var a = -2;
        var b = a + 1;
    }
    
    public void BoxNullable()
    {
        int? i = null;
        int? j = 5;
        int ifoo = ZeroForNull(i);
        int jfoo = ZeroForNull(j);
    }
    public unsafe int ArrayStore(int[] a, int i)
    {
        a[i] = 5;
        return a[i];
    }
    public unsafe int SymbolicStructWrite(int i)
    {
        var array = new MyStruct[2];
        array[0] = new MyStruct() { x = 5 };
        array[1] = new MyStruct() { y = 1 };
        fixed (MyStruct* ptr = &array[0])
        {
            var casted = (byte*)ptr;
            *(int*)(casted + i) = 500;
            if (array[0].y == 500 && i != 4)
            {
                return -1;
            }

            return 0;
        }
    }


    public unsafe int ArgWrite(int a)
    {
        var ptr = &a;
        *ptr = 422;
        if (a != 422) return -1;
        return 0;
    }

    public unsafe int StackUnsafe1(int a, int i)
    {
        var initValue = a;
        var ptr = &a;
        var casted = (byte*)ptr;
        *(int*)(casted + i) = 322;
        if (i == 0 && a != 322)
        {
            return -1;
        }

        return 0;
    }

    public unsafe int StackUnsafe2(int a, int i)
    {
        var initValue = a;
        var ptr = &a;
        var casted = (byte*)ptr;
        *(int*)(casted + i) = 322;
        if (i == 2 && initValue == 5 && a != 21102597)
        {
            return -1;
        }

        return 0;
    }

    class A
    {
        public int Hui = 2;
        public int V = 1;
    }

    class B
    {
        public int V = 2;
    }

    [Fact]
    public unsafe void ClassPtrCasts()
    {
        A a = new A();
        var b = (B*)(&a);
        testOutputHelper.WriteLine((*b).V.ToString());
    }

    public int Ge(int a, int b, bool flag)
    {
        if (flag) return 0;
        return a >= b ? 1 : 0;
    }

    public int Shl(int a, int b)
    {
        if (a != 0 || b >= 0)
        {
            return a << b;
        }

        return 0;
    }

    public unsafe int InconsistentPtr(int a, int i)
    {
        var ptr = &a;
        var casted = (byte*)ptr;
        *(int*)(casted + i) = 322;
        return 0;
    }

    class ManagedInstance
    {
    }

    [Fact]
    public unsafe void RawPointerToManagedInstance()
    {
        ManagedInstance i = new ManagedInstance();
        var ptr = &i;
        ref var m = ref i;
        byte b = 0;
        ref var managedBytePtr = ref b;
        var x = managedBytePtr + 1;
        managedBytePtr += 1;
    }
}