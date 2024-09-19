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
    private bool _resolved = false;

    public MethodMeta(MethodInfo methodInfo, bool resolveImmediately = true)
    {
        _methodInfo = methodInfo;
        if (resolveImmediately) Resolve();
    }

    public void Resolve()
    {
        // TODO what to do if method body is null
        if (_resolved) return;
        _resolved = true;
        if (_methodInfo.GetMethodBody() == null) return;

        try
        {
            var bodyParser = new ILBodyParser.ILBodyParser(_methodInfo.GetMethodBody()!);
            bodyParser.Parse();
            _firstInstruction = bodyParser.Instructions;
            _ehClauses = bodyParser.EhClauses;
        }
        catch (Exception e)
        {
            Console.WriteLine("error at " + _methodInfo.Name + " " + e);
        }

        // var cfg = new CFG(bodyParser.Instructions, bodyParser.EhClauses);
        // cfg.MarkBasicBlocks();
        // _basicBlocks = cfg.BasicBlocksMarkup.Select(bbLocation => new BasicBlockMeta(bbLocation)).ToList();
    }
}