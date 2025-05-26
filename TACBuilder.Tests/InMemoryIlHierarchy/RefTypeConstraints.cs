namespace TACBuilder.Tests.InMemoryIlHierarchy;

public class RefTypeTestBase<T> where T : class;

public class RefTypeClass : RefTypeTestBase<string>;

public class NotNullValueTypeTestBase<T> where T : struct;

public class NotNullValueTypeClass: NotNullValueTypeTestBase<int>;

public class MultiParamBase<T1, T2>; 

public class MultiParamImpl: MultiParamBase<int, string>;