namespace TACBuilder.Tests.InMemoryIlHierarchy;

[AttributeUsage(AttributeTargets.Class)]
public class InMemoryHierarchyTestEntryAttribute(Type[] expectedChildren) : Attribute;