using System.Reflection;

namespace TACBuilder.ILMeta;

public class MethodMeta
{
    private MethodInfo _methodInfo;
    private List<BasicBlockMeta> _basicBlocks = new();

    public MethodMeta(MethodInfo methodInfo, bool resolveImmediately = true)
    {
        _methodInfo = methodInfo;
        if (resolveImmediately) Resolve();
    }

    public void Resolve()
    {
        // TODO what to do if method body is null
        if (_methodInfo.GetMethodBody() == null) return;
        var bodyParser = new ILBodyParser.ILBodyParser(_methodInfo.GetMethodBody()!);
        bodyParser.Parse();

        var cfg = new CFG(bodyParser.Instructions, bodyParser.EhClauses);
        cfg.MarkBasicBlocks();
        _basicBlocks = cfg.BasicBlocksMarkup.Select(bbLocation => new BasicBlockMeta(bbLocation)).ToList();
    }
}