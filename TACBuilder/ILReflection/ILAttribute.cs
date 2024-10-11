using System.Reflection;

namespace TACBuilder.ILReflection;

public class ILAttribute(CustomAttributeData attribute) : ILCacheable
{
    public class Argument(ILType ilType, object? value)
    {
        public ILType IlType => ilType;
        public object? Value => value;
    }

    private readonly CustomAttributeData _attribute = attribute;
    public ILType? Type { get; private set; }
    public List<ILType> GenericArgs { get; } = new();
    public List<Argument> ConstructorArguments { get; } = new();

    public override void Construct()
    {
        Type = ILInstanceBuilder.GetType(_attribute.AttributeType);
        foreach (var arg in _attribute.AttributeType.GetGenericArguments())
        {
            GenericArgs.Add(ILInstanceBuilder.GetType(arg));
        }

        foreach (var arg in _attribute.ConstructorArguments)
        {
            ConstructorArguments.Add(new Argument(ILInstanceBuilder.GetType(arg.ArgumentType), arg.Value));
        }
    }
}
