using TACBuilder.ILMeta;
using TACBuilder.ILTAC;

namespace TACBuilder;

public class TypeTacBuilder(TypeMeta meta)
{
    private TypeMeta _meta = meta;

    private List<MethodTacBuilder> _methodBuilders =
        meta.Methods.Select(methodMeta => new MethodTacBuilder(methodMeta)).ToList();

    public TACType Build()
    {
        var builtMethods = new List<TACMethod>();
        foreach (var methodBuilder in _methodBuilders)
        {
            try
            {
                builtMethods.Add(methodBuilder.Build());
            }
            catch (Exception e)
            {
                Console.WriteLine("TypeTacBuilder with " + e);
            }
        }

        return new TACType(builtMethods);
    }
}
