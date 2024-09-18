using System.Reflection;

namespace TACBuilder.ILMeta;

public class TypeMeta(Type type)
{
    private Type _type = type;
    private List<MethodMeta> _methods = type.GetMethods().Select(methodInfo => new MethodMeta(methodInfo)).ToList();
    private List<FieldMeta> _fields = type.GetFields().Select(fieldInfo => new FieldMeta(fieldInfo)).ToList();
    
    // TODO anon namespaces and is it true fully qualified name == ns + name
    public string Name => _type.Name;
    public string Namespace => _type.Namespace ?? "";
}

public class FieldMeta(FieldInfo fieldInfo)
{
}