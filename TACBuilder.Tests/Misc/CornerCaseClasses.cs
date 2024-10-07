namespace TACBuilder.Tests.Misc;

public class CornerCaseClasses
{
    public void MyVectorUsage()
    {
        var vector = new MyVector<int>();
        int x = vector[12];
        vector.Method(33);
    }

    public unsafe void MyVectorUsageUnsafe()
    {
        var vector = new MyVector<IntPtr>();
        int one = 1;
        int* ptr = &one;
        vector._data[1] = (IntPtr)ptr;
        var x = vector[12];
        vector.Method(33);
    }
}

public unsafe class MyVector<T> where T : unmanaged
{
    public T[] _data = new T[100];
    public T this[int index] => _data[index];

    public void Method(int index)
    {
        _data[index] = new T();
    }
}