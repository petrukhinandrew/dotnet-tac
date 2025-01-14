namespace TACBuilder.Tests.InMemoryIlHierarchy;

class SingleParamBase<T>;

class SingleParamAny<G> : SingleParamBase<G>;

class SingleParamClass<T> : SingleParamBase<T> where T : class;

class SingleParamStruct<T> : SingleParamBase<T> where T : struct;

class DefaultCtorTestBase<T> where T : new();

class DefaultCtorStruct<T> : DefaultCtorTestBase<T> where T : struct;

class DefaultCtorClass<T> : DefaultCtorTestBase<T> where T : class, new();

interface DefaultCtorInterface<T> where T : new();

class DefaultCtorTypeParam
{
    public DefaultCtorTypeParam()
    {
    }
}

class Check
{
    void VoidCheck()
    {
        DefaultCtorClass<DefaultCtorTypeParam> value = new();
    }
}