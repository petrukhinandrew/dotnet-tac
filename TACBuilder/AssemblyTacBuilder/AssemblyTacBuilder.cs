using TACBuilder.ILMeta;
using TACBuilder.ILTAC;

namespace Usvm.TACBuilder.AssemblyTacBuilder;

public class AssemblyTacBuilder(AssemblyMeta meta)
{
    private AssemblyMeta _meta = meta;

    private List<TypeTacBuilder.TypeTacBuilder> _typeBuilders =
        meta.Types.Select(typeMeta => new TypeTacBuilder.TypeTacBuilder(typeMeta)).ToList();

    public TACAssembly Build()
    {
        var builtTypes =
            _typeBuilders.Select(typeBuilder =>
                typeBuilder.Build());  
        return new TACAssembly(builtTypes);
    }
}