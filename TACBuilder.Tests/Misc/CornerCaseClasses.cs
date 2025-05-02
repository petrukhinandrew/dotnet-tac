namespace TACBuilder.Tests.Misc;

public class Assignments
{
    [Fact]
    public void MutexToObject()
    {
        object mutex = new Mutex();
        var objType = typeof(object);
        var mutexType = typeof(Mutex);
        Assert.True(objType.IsAssignableFrom(mutexType));
    }
}

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

[AttributeUsage(AttributeTargets.All)]
public class GenericAttr<T> : Attribute
{
    public T Value { get; set; }
}

[GenericAttr<int>(Value = 5)]
public class GenericAttrUsage
{
    [GenericAttr<string>(Value = "value")]
    public void Method(int value)
    {
        
    }
}
