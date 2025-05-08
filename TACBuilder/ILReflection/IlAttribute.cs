using System.Reflection;
using TACBuilder.Exprs;

namespace TACBuilder.ILReflection;

public class IlAttribute(CustomAttributeData attribute) : IlCacheable
{
    public class Argument(IlType ilType, IlConstant value)
    {
        public IlType IlType => ilType;
        public IlConstant Value => value;
    }

    public readonly IlType Type = IlInstanceBuilder.GetType(attribute.AttributeType);
    public List<IlType> GenericArgs { get; } = attribute.AttributeType.GetGenericArguments().Select(IlInstanceBuilder.GetType).ToList();
    public Dictionary<string, Argument> NamedArguments { get; } = new();
    public List<Argument> ConstructorArguments { get; } = new();

    public override void Construct()
    {
        foreach (var arg in attribute.ConstructorArguments)
        {
            if (arg.Value == null) continue;
            ConstructorArguments.Add(new Argument(IlInstanceBuilder.GetType(arg.ArgumentType),
                IlConstant.From(arg.Value)));
        }

        foreach (var arg in attribute.NamedArguments)
        {
            var typed = arg.TypedValue;
            if (typed.Value == null) continue;
            NamedArguments.Add(arg.MemberName, new Argument(IlInstanceBuilder.GetType(typed.ArgumentType),
                IlConstant.From(typed.Value)));
        }
    }
}