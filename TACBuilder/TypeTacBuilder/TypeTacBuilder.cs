using TACBuilder.ILMeta;
using TACBuilder.ILTAC;

namespace Usvm.TACBuilder.TypeTacBuilder;

public class TypeTacBuilder(TypeMeta meta)
{
    private TypeMeta _meta = meta;

    public TACType Build()
    {
        var methodBuilders = _meta.Methods.Select(methodMeta => new MethodTacBuilder.MethodTacBuilder(methodMeta));
        var builtMethods = methodBuilders.Select(methodBuilder => methodBuilder.Build());
        return new TACType(builtMethods);
    }
    /*
     * notes on lazy API
     *
     * instead of built TAC methods we store builders (that also may have cache)
     * and introduce methods like BuildSelected(needed info or method identifiers) and BuildAll that for all stored builders call build
     * initially every builder should do lazy resolving (no body parsing)
     */
}