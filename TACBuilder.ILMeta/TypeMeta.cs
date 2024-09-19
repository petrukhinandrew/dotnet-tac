using System.Reflection;

namespace TACBuilder.ILMeta;

public class TypeMeta(Type type)
{
    private Type _type = type;

    private const BindingFlags BindingFlags =
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;


    private List<FieldMeta> _fields = type.GetFields().Select(fieldInfo => new FieldMeta(fieldInfo)).ToList();

    public List<MethodMeta> Methods { get; } = type.GetMethods(BindingFlags).Where(methodInfo => methodInfo.GetMethodBody() != null).Select(methodInfo => new MethodMeta(methodInfo)).ToList();

    public string Name => _type.Name;
    public string Namespace => _type.Namespace ?? "";
}

public class FieldMeta(FieldInfo fieldInfo)
{
}