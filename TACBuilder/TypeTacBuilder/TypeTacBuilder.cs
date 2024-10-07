using TACBuilder.ILMeta;
using TACBuilder.ILTAC;

namespace TACBuilder;

public class TypeTacBuilder(TypeMeta meta)
{
    private TypeMeta _meta = meta;

    private readonly List<MethodTacBuilder> _methodBuilders =
        meta.Methods.Select(methodMeta => new MethodTacBuilder(methodMeta)).ToList();

    public TACType Build()
    {
        var builtMethods = new List<TACMethod>();
        foreach (var methodBuilder in _methodBuilders)
        {
            builtMethods.Add(methodBuilder.Build());
        }

        return new TACType(builtMethods);
    }
}
