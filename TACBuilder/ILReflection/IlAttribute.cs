using System.Reflection;
using TACBuilder.Exprs;
using TACBuilder.Utils;

namespace TACBuilder.ILReflection;

public class IlAttribute(CustomAttributeData attribute) : IlCacheable
{
    public class Argument(IlType ilType, IlConstant value)
    {
        public IlType IlType => ilType;
        public IlConstant Value => value;
    }

    public IlType? Type { get; private set; }
    public List<IlType> GenericArgs { get; } = new();
    public Dictionary<string, Argument> NamedArguments { get; } = new();
    public List<Argument> ConstructorArguments { get; } = new();

    public override void Construct()
    {
        Type = IlInstanceBuilder.GetType(attribute.AttributeType);
        foreach (var arg in attribute.AttributeType.GetGenericArguments())
        {
            GenericArgs.Add(IlInstanceBuilder.GetType(arg));
        }

        foreach (var arg in attribute.ConstructorArguments)
        {
            if (arg.Value == null) continue;
            ConstructorArguments.Add(new Argument(IlInstanceBuilder.GetType(arg.ArgumentType),
                TypingUtil.ResolveConstant(arg.Value, arg.ArgumentType.IsEnum ? arg.ArgumentType : null)));
        }

        foreach (var arg in attribute.NamedArguments)
        {
            var typed = arg.TypedValue;
            if (typed.Value == null) continue;
            NamedArguments.Add(arg.MemberName, new Argument(IlInstanceBuilder.GetType(typed.ArgumentType),
                TypingUtil.ResolveConstant(typed.Value, typed.ArgumentType.IsEnum ? typed.ArgumentType : null)));
        }
    }
}
