namespace TACBuilder.Tests.InMemoryIlHierarchy;

public class Simple
{
    [InMemoryHierarchyTestEntry([typeof(DirectChild), typeof(IndirectChild)])]
    public class Base;

    public class DirectChild : Base;

    [InMemoryHierarchyTestEntry([])]
    public class IndirectChild : DirectChild;
}