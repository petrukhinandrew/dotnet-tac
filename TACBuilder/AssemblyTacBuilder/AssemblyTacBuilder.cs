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
                typeBuilder.Build()); // we may use selected strategy here (passed by setter)  
        return new TACAssembly(builtTypes);
    }
    /*
     * notes on lazy strategy API design
     *
     * Assembly and types cannot be lazy
     * lazy method building
     * =>
     *
     * any InstBuilder returns TacInst as a container
     * either add assembly tac builder the knowledge of which methods should be resolved
     * or make Build return void so that further there may be a request like resolve this in here
     *
     *
     * mb it'll be more verbose to use setters to show what is to be resolved
     *
     * do we need to dump unreachable stuff?
     * dump reachable methods for given method only?
     *
     */
}