namespace TACBuilder.Tests.InMemoryIlHierarchy;

public class Simple
{
    [InMemoryHierarchyTestEntry([typeof(DirectChild), typeof(IndirectChild)])]
    public class Base
    {
        public virtual int VirtualMethod(int a)
        {
            return a + 1;
        }
    }

    public class DirectChild : Base
    {
        public override int VirtualMethod(int a)
        {
            return base.VirtualMethod(a) + 1;
        }
    }

    [InMemoryHierarchyTestEntry([])]
    public class IndirectChild : DirectChild
    {
        public override int VirtualMethod(int a)
        {
            return base.VirtualMethod(a) + 3;
        }
    }
}