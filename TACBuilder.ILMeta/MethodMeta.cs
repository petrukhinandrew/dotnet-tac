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
    private readonly bool _hasMethodBody;
    public bool HasMethodBody => _hasMethodBody;
    public CFG Cfg;

    public MethodMeta(MethodInfo methodInfo, bool resolveImmediately = true)
    {
        _methodInfo = methodInfo;
        _hasMethodBody = methodInfo.GetMethodBody() != null;
        if (resolveImmediately) Resolve();
    }

    public void Resolve()
    {
        // TODO what to do if method body is null
        if (!_hasMethodBody) return;
        if (_resolved) return;
        _resolved = true;
        if (_methodInfo.GetMethodBody() == null) return;

        try
        {
            var bodyParser = new ILBodyParser.ILBodyParser(_methodInfo.GetMethodBody()!);
            bodyParser.Parse();
            _firstInstruction = bodyParser.Instructions;
            _ehClauses = bodyParser.EhClauses;
            Cfg = new CFG(bodyParser.Instructions, bodyParser.EhClauses);
            _basicBlocks = Cfg.BasicBlocks;
        }
        catch (Exception e)
        {
            Console.WriteLine("error at " + _methodInfo.Name + " " + e);
        }
    }
}