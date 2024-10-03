using System.Reflection;

namespace TACBuilder.ILMeta;

public class AttributeMeta(CustomAttributeData attribute) : CacheableMeta
{
    private readonly CustomAttributeData _attribute = attribute;
    public TypeMeta? Type { get; private set; }
    public List<TypeMeta> GenericArgs { get; } = new();
    public List<AttributeArgumentMeta> ConstructorArguments { get; } = new();

    public override void Construct()
    {
        Type = MetaBuilder.GetType(_attribute.AttributeType);
        foreach (var arg in _attribute.AttributeType.GetGenericArguments())
        {
            GenericArgs.Add(MetaBuilder.GetType(arg));
        }

        foreach (var arg in _attribute.ConstructorArguments)
        {
            ConstructorArguments.Add(new AttributeArgumentMeta(MetaBuilder.GetType(arg.ArgumentType), arg.Value));
        }
    }
}

public class AttributeArgumentMeta(TypeMeta typeMeta, object? value)
{
    public TypeMeta Type => typeMeta;
    public object? Value => value;
}
