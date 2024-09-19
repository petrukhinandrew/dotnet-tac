using TACBuilder.ILMeta;
using TACBuilder.ILTAC;

namespace Usvm.TACBuilder.TypeTacBuilder;

public class TypeTacBuilder(TypeMeta meta)
{
    private TypeMeta _meta = meta;

    private List<MethodTacBuilder.MethodTacBuilder> _methodBuilders =
        meta.Methods.Select(methodMeta => new MethodTacBuilder.MethodTacBuilder(methodMeta)).ToList();

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
                Console.WriteLine(_meta.Name + " " + methodBuilder.MethodInfo.Name + " " + e.Message);
            }
        }

        return new TACType(builtMethods);
    }
}