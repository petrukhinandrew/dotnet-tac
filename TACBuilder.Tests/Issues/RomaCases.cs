using Xunit.Abstractions;

namespace TACBuilder.Tests.Issues;

public class RomaCases(ITestOutputHelper testOutputHelper)
{
    public struct MyStruct
    {
        public int Age;
        public string Name;
    }

    public int WriteStructConcrete()
    {
        var s = new MyStruct { Age = 1, Name = null };
        var age = s.Age;
        var name = s.Name;
        s.Name = name;
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