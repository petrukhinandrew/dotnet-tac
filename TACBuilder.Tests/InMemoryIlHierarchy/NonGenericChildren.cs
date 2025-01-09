namespace TACBuilder.Tests.InMemoryIlHierarchy;

public class NonGenericChildren
{
    [InMemoryHierarchyTestEntry([typeof(GenericChildString), typeof(GenericChildStringChild)])]
    public class GenericAnyBase<T>;

    public class GenericChildString : GenericAnyBase<string>;

    public class GenericChildStringChild : GenericChildString;

    [InMemoryHierarchyTestEntry([typeof(GenericChildStructInt), typeof(GenericChildStructIntChild)])]
    public class GenericBaseStruct<T> where T : struct;

    public class GenericChildStructInt : GenericBaseStruct<int>;

    public class GenericChildStructIntChild : GenericChildStructInt;
}