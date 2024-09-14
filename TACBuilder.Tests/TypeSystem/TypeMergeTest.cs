using Usvm.IL.TypeSystem;

namespace TACBuilder.Tests;

public class TypeMergeTest
{
    class BaseClass;

    class BaseChild1 : BaseClass;

    class BaseChild2 : BaseClass;

    class BaseChild2Child : BaseChild2;

    interface BaseInterface;

    struct StructWithInterface1 : BaseInterface;

    struct StructWithInterface2 : BaseInterface;

    [Fact]
    public void MergeSame()
    {
        var t1 = TypingUtil.ILTypeFrom(typeof(BaseChild1));
        var t2 = TypingUtil.ILTypeFrom(typeof(BaseChild1));
        var merged = TypingUtil.Merge([t1, t2]);
        Assert.Equal(t1, merged);
        Assert.Equal(t2, merged);
    }

    [Fact]
    public void MergeBaseAndChild()
    {
        var childClass = TypingUtil.ILTypeFrom(typeof(BaseChild1));
        var baseClass = TypingUtil.ILTypeFrom(typeof(BaseClass));
        var merged = TypingUtil.Merge([baseClass, childClass]);
        Assert.Equal(baseClass, merged);
        Assert.NotEqual(childClass, merged);
    }

    [Fact]
    public void MergeChildren()
    {
        var child1 = TypingUtil.ILTypeFrom(typeof(BaseChild1));
        var child2 = TypingUtil.ILTypeFrom(typeof(BaseChild2));
        var merged = TypingUtil.Merge([child1, child2]);
        Assert.NotEqual(child1, merged);
        Assert.NotEqual(child2, merged);
        var parent = TypingUtil.ILTypeFrom(typeof(BaseClass));
        Assert.Equal(parent, merged);
    }

    [Fact]
    public void MergeChildAndGrandChild()
    {
        var child = TypingUtil.ILTypeFrom(typeof(BaseChild1));
        var grandchild = TypingUtil.ILTypeFrom(typeof(BaseChild2Child));
        var merged = TypingUtil.Merge([child, grandchild]);
        Assert.NotEqual(child, merged);
        Assert.NotEqual(grandchild, merged);
        var parent = TypingUtil.ILTypeFrom(typeof(BaseClass));
        Assert.Equal(parent, merged);
    }

    [Fact]
    public void MergeStructsFromInterface()
    {
        var s1 = TypingUtil.ILTypeFrom(typeof(StructWithInterface1));
        var s2 = TypingUtil.ILTypeFrom(typeof(StructWithInterface2));
        var merged = TypingUtil.Merge([s1, s2]);
        Assert.NotEqual(s1, merged);
        Assert.NotEqual(s2, merged);
        Assert.NotEqual(TypingUtil.ILTypeFrom(typeof(ValueType)), merged);
    }

    [Fact]
    public void MergePrimitives()
    {
        var ilInt = TypingUtil.ILTypeFrom(typeof(int));
        var ilDouble = TypingUtil.ILTypeFrom(typeof(double));
        var merged = TypingUtil.Merge([ilInt, ilDouble]);
        Assert.NotEqual(ilInt, merged);
        Assert.NotEqual(ilDouble, merged);
        Assert.NotEqual(TypingUtil.ILTypeFrom(typeof(ValueType)), merged);
    }
}