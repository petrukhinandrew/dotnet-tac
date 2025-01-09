namespace TACBuilder.Tests.InMemoryIlHierarchy;

public class GenericChildren
{
    [InMemoryHierarchyTestEntry([
        typeof(GenericClassBase<ITypeParamBase>), 
        typeof(GenericClassBase<TypeParamBase>),
        typeof(GenericClassBase<TypeParamChild>)
    ])]
    public class GenericClassBase<T> where T : ITypeParamBase;

    public interface ITypeParamBase;

    public abstract class TypeParamBase : ITypeParamBase;

    public class TypeParamChild : TypeParamBase;
}