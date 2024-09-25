using System.Reflection;
using TACBuilder.ILMeta.ILBodyParser;

namespace TACBuilder.ILMeta;

public class MethodMeta
{
    private MethodInfo _methodInfo;
    public MethodInfo MethodInfo => _methodInfo;

    private List<BasicBlockMeta> _basicBlocks = new();
    public List<BasicBlockMeta> BasicBlocks => _basicBlocks;
    public ILInstr FirstInstruction => _bodyParser.Instructions;
    public List<ehClause> EhClauses => _bodyParser.EhClauses;
    private bool _resolved = false;
    private readonly bool _hasMethodBody;
    public bool HasMethodBody => _hasMethodBody;
    public CFG Cfg;
    private ILBodyParser.ILBodyParser _bodyParser;
    public List<int> StartBlocksIndices => Cfg.StartBlocksIndices;

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
            _bodyParser = new ILBodyParser.ILBodyParser(_methodInfo.GetMethodBody()!);
            _bodyParser.Parse();
            Cfg = new CFG(_bodyParser.Instructions, _bodyParser.EhClauses);
            _basicBlocks = Cfg.BasicBlocks;
        }
        catch (Exception e)
        {
            Console.WriteLine("MethodMeta error at " + (_methodInfo.ReflectedType ?? _methodInfo.DeclaringType)!.Name + " " +_methodInfo.Name + " " + e);
        }
    }
}