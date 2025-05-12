using TACBuilder.ILReflection;
using TACBuilder.Utils;

namespace TACBuilder.Tests;

public class TypeMergeTest
{
    class BaseClass;

    class BaseChild1 : BaseClass;

    class BaseChild2 : BaseClass;

    class BaseChild2Child : BaseChild2;

    interface BaseInterface;

    struct StructWithInterface1 : BaseInterface;

    class ClassWithInterface1 : BaseInterface;

    struct StructWithInterface2 : BaseInterface;

    class ClassWithInterface2 : BaseInterface;

    
    [Fact]
    public void MergeSame()
    {
        var t1 = new IlType(typeof(BaseChild1));
        var t2 = new IlType(typeof(BaseChild1));
        var merged = TypingUtil.Merge([t1, t2]);
        Assert.Equal(t1, merged);
        Assert.Equal(t2, merged);
    }

    [Fact]
    public void MergeBaseAndChild()
    {
        var childClass = new IlType(typeof(BaseChild1));
        var baseClass = new IlType(typeof(BaseClass));
        var merged = TypingUtil.Merge([baseClass, childClass]);
        Assert.Equal(baseClass, merged);
        Assert.NotEqual(childClass, merged);
    }
    
    [Fact]
    public void MergeChildren()
    {
        var child1 = new IlType(typeof(BaseChild1));
        var child2 = new IlType(typeof(BaseChild2));
        var merged = TypingUtil.Merge([child1, child2]);
        Assert.NotEqual(child1, merged);
        Assert.NotEqual(child2, merged);
        var parent = new IlType(typeof(BaseClass));
        Assert.Equal(parent, merged);
    }
    
    [Fact]
    public void MergeChildAndGrandChild()
    {
        var child = new IlType(typeof(BaseChild1));
        var grandchild = new IlType(typeof(BaseChild2Child));
        var merged = TypingUtil.Merge([child, grandchild]);
        Assert.NotEqual(child, merged);
        Assert.NotEqual(grandchild, merged);
        var parent = new IlType(typeof(BaseClass));
        Assert.Equal(parent, merged);
    }
    
    [Fact]
    public void MergeStructsToInterface()
    {
        var s1 = new IlType(typeof(StructWithInterface1));
        var s2 = new IlType(typeof(StructWithInterface2));
        var merged = TypingUtil.Merge([s1, s2]);
        Assert.NotEqual(s1, merged);
        Assert.NotEqual(s2, merged);
        Assert.NotEqual(new IlType(typeof(BaseInterface)), merged);
    }
    
    [Fact]
    public void MergeClassToInterface()
    {
        var c1 = new IlType(typeof(ClassWithInterface1));
        var c2 = new IlType(typeof(ClassWithInterface2));
        var merged = TypingUtil.Merge([c1, c2]);
        Assert.NotEqual(c1, merged);
        Assert.NotEqual(c2, merged);
        Assert.Equal(new IlType(typeof(BaseInterface)), merged);
    }
    
    [Fact]
    public void MergePrimitives()
    {
        var ilInt = new IlType(typeof(int));
        var ilDouble = new IlType(typeof(double));
        var merged = TypingUtil.Merge([ilInt, ilDouble]);
        Assert.NotEqual(ilInt, merged);
        Assert.Equal(ilDouble, merged);
    }
    
    [Fact]
    public void MergeFloats()
    {
        var ilFloat = new IlType(typeof(float));
        var ilDouble = new IlType(typeof(double));
        var merged = TypingUtil.Merge([ilFloat, ilDouble]);
        Assert.NotEqual(ilFloat, merged);
        Assert.Equal(ilDouble, merged);
    }
    
    [Fact]
    public void MergeIntegers()
    {
        var ilByte = new IlType(typeof(byte));
        var ilInt = new IlType(typeof(int));
        var merged = TypingUtil.Merge([ilByte, ilInt]);
        Assert.NotEqual(ilByte, merged);
        Assert.Equal(ilInt, merged);
    }
}
