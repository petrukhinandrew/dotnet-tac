namespace TACBuilder.Tests.InMemoryIlHierarchy;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public class InMemoryHierarchyTestEntryAttribute(Type[] expectedChildren) : Attribute;