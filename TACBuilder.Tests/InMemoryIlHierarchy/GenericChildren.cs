namespace TACBuilder.Tests.InMemoryIlHierarchy;

class SingleParamBase<T>;

class SingleParamAny<G> : SingleParamBase<G>;

class SingleParamClass<T> : SingleParamBase<T> where T : class;

class SingleParamStruct<T> : SingleParamBase<T> where T : struct;
// class TwoParamBase<T1, T2>;
//
// class TwoParamChildOptimistic<T1, T2> : TwoParamBase<T2, T1>;
//
// class TwoParamChildClass<T1, T2>: 