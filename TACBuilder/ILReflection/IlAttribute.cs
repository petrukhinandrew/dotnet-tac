using System.Reflection;

namespace TACBuilder.ILReflection;

public class IlAttribute(CustomAttributeData attribute) : IlCacheable
{
    public class Argument(IlType ilType, object? value)
    {
        public IlType IlType => ilType;
        public object? Value => value;
    }

    private readonly CustomAttributeData _attribute = attribute;
    public IlType? Type { get; private set; }
    public List<IlType> GenericArgs { get; } = new();
    public List<Argument> ConstructorArguments { get; } = new();

    public override void Construct()
    {
        Type = IlInstanceBuilder.GetType(_attribute.AttributeType);
        foreach (var arg in _attribute.AttributeType.GetGenericArguments())
        {
            GenericArgs.Add(IlInstanceBuilder.GetType(arg));
        }

        foreach (var arg in _attribute.ConstructorArguments)
        {
            ConstructorArguments.Add(new Argument(IlInstanceBuilder.GetType(arg.ArgumentType), arg.Value));
        }
    }
}
