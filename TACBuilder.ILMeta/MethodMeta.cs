using System.Reflection;
using TACBuilder.ILMeta.ILBodyParser;

namespace TACBuilder.ILMeta;

public class MethodMeta
{
    private MethodInfo _methodInfo;
    public MethodInfo MethodInfo => _methodInfo;

    private List<BasicBlockMeta> _basicBlocks = new();
    private ILInstr _firstInstruction;
    public ILInstr FirstInstruction => _firstInstruction;
    private List<ehClause> _ehClauses = new();
    public List<ehClause> EhClauses => _ehClauses;

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
        _firstInstruction = bodyParser.Instructions;
        _ehClauses = bodyParser.EhClauses;
        var cfg = new CFG(bodyParser.Instructions, bodyParser.EhClauses);
        cfg.MarkBasicBlocks();
        _basicBlocks = cfg.BasicBlocksMarkup.Select(bbLocation => new BasicBlockMeta(bbLocation)).ToList();
    }
}