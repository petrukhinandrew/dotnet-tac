namespace TACBuilder.Tests.InMemoryIlHierarchy;

public class Implementors
{
    [InMemoryHierarchyTestEntry(
    [
        typeof(IDirectImplementor), typeof(IIndirectImplementorA), typeof(IIndirectImplementorB),
        typeof(BottomImplementor)
    ])]
    public interface IBase;

    public interface IDirectImplementor : IBase;

    [InMemoryHierarchyTestEntry([typeof(BottomImplementor)])]
    public interface IIndirectImplementorA : IDirectImplementor;

    [InMemoryHierarchyTestEntry([typeof(BottomImplementor)])]
    public interface IIndirectImplementorB : IDirectImplementor;

    public class BottomImplementor : IIndirectImplementorA, IIndirectImplementorB;
}