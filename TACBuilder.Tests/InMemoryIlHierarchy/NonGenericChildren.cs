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

    [InMemoryHierarchyTestEntry([
        typeof(GenericClassInterfaceChild),
        typeof(GenericClassAbstractChild),
        typeof(GenericClassCommonChild)
    ])]
    public class GenericClassBase<T> where T : ITypeParamBase;

    public interface ITypeParamBase;

    public abstract class TypeParamBase : ITypeParamBase;

    public class TypeParamChild : TypeParamBase;

    public class GenericClassInterfaceChild : GenericClassBase<ITypeParamBase>;

    public class GenericClassAbstractChild : GenericClassBase<TypeParamBase>;

    public class GenericClassCommonChild : GenericClassBase<TypeParamChild>;
}