using TACBuilder.ReflectionUtils;
using Xunit.Abstractions;

namespace TACBuilder.Tests.Issues;

public class GenericBase<T>
{
    public T Field;

    public class Nested<T>;

    public class NonGeneric;
}

public class GenericInheritor<T> : GenericBase<T>
{
}

public class GenericDoubleBase<T, G>
{
    public class Nested;
}

public class DoubleInheritor<T> : GenericDoubleBase<int, T>;

public class Naming(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void Test()
    {
        var baseType = typeof(GenericInheritor<int>).GetGenericTypeDefinition().BaseType!.GetGenericArguments().First();//.MakePointerType();
        testOutputHelper.WriteLine(baseType.FullName ?? "null");
        var listT = typeof(List<>).GetGenericArguments().First();
        testOutputHelper.WriteLine(listT.FullName ?? "null");
        // Assert.Null(baseType.FullName);
        // Assert.NotEqual(baseType.GetGenericTypeDefinition(), baseType);
        // var kekType = typeof(DoubleInheritor<>).BaseType!;
        // Assert.Null(kekType.FullName);
        // Assert.NotNull(kekType.GetGenericTypeDefinition().FullName);
        // testOutputHelper.WriteLine(kekType.GetGenericTypeDefinition().FullName);
    }

    [Fact]
    public void Test2()
    {
        var listInt = typeof(List<int>).MakeArrayType().MakePointerType().GetElementType().GetElementType().GetGenericTypeDefinition();
        testOutputHelper.WriteLine(listInt.FullName ?? "null");
    }

    [Fact]
    public void Test3()
    {
        var kek = typeof(GenericBase<int>.Nested<double>).GetGenericTypeDefinition().FullName;
        testOutputHelper.WriteLine(kek);
    }
    [Fact]
    public void Test4()
    {
        var kek1 = typeof(GenericBase<int>.Nested<double>).MakePointerType();
        var kek2 = typeof(GenericBase<double>.Nested<double>).MakePointerType();
        Assert.NotEqual(kek1, kek2);
        Assert.NotEqual(kek1.GetElementType(), kek2.GetElementType());
        testOutputHelper.WriteLine(kek1.IsGenericType.ToString());
    }

    [Fact]
    public void Test5()
    {
        List<Type> types = [
            typeof(byte).MakePointerType(),
            typeof(List<int>),
            typeof(List<string>).MakeArrayType().MakePointerType(),
            typeof(List<>),
            typeof(Dictionary<,>).GetGenericArguments().First().MakePointerType(),
            typeof(GenericBase<>.NonGeneric).MakeArrayType(), 
            typeof(GenericBase<>).GetGenericArguments()[0],
            typeof(GenericBase<>.Nested<>).GetGenericArguments()[0],
            typeof(GenericBase<>.Nested<>).GetGenericArguments()[1],
        ];
        Assert.Equal(
            typeof(GenericBase<>.Nested<>).GetGenericArguments()[0].DeclaringType,
            typeof(GenericBase<>.Nested<>).GetGenericArguments()[1].DeclaringType);
        // Assert.Equal(
        //     typeof(GenericBase<>.Nested<>).GetGenericArguments()[0],
        //     typeof(GenericBase<>).GetGenericArguments()[0]);
        foreach (var type in types)
        {
            testOutputHelper.WriteLine(type.ConstructFullName());
        }
    }

    [Fact]
    public void Test6()
    {
        var baseType = typeof(GenericInheritor<>);
        var paramType = typeof(List<>).GetGenericArguments().First();
        var res = baseType.MakeGenericType(paramType);
        Assert.NotNull(res);
    }

    [Fact]
    public void Test7()
    {
        var t = typeof(GenericInheritor<>.Nested<>);
        testOutputHelper.WriteLine(t.GetGenericArguments()[0].ReflectedType.FullName ?? "null");
        testOutputHelper.WriteLine(t.GetGenericArguments()[1].ReflectedType.FullName ?? "null");
    }
}