using TACBuilder.ILMeta;

namespace TACBuilder;

public class AppTacBuilder
{
    public AppTacBuilder(string rootAssemblyPath)
    {
        MetaBuilder builder = new();
        builder.BuildFrom(rootAssemblyPath);
    }
}
