using System.Reflection;
using TACBuilder.ILMeta.ILBodyParser;

namespace TACBuilder.ILMeta;

public class MethodMeta : MemberMeta
{
    public AssemblyMeta DeclaringAssembly { get; }
    private MethodBase _methodBase;
    private List<BasicBlockMeta> _basicBlocks = new();
    private bool _resolved = false;
    private ILBodyParser.ILBodyParser _bodyParser;
    private TypeMeta _typeMeta;

    public MethodBase MethodBase => _methodBase;
    public List<BasicBlockMeta> BasicBlocks => _basicBlocks;
    public List<int> StartBlocksIndices => Cfg.StartBlocksIndices;
    public ILInstr FirstInstruction => _bodyParser.Instructions;
    public List<ehClause> EhClauses => _bodyParser.EhClauses;

    public int MetadataToken => _methodBase.MetadataToken;
    // TODO cfg must be private
    public CFG Cfg;


    public MethodMeta(TypeMeta typeMeta, MethodBase methodBase): base(methodBase)
    {
        _typeMeta = typeMeta;
        DeclaringAssembly = _typeMeta.DeclaringAssembly;
        _methodBase = methodBase;
    }

    public void Resolve()
    {
        var hasMethodBody = false;
        try
        {
            hasMethodBody = _methodBase.GetMethodBody() != null;
        }
        catch
        {
            Console.WriteLine(_methodBase.Name + " has no body");
            hasMethodBody = false;
        }

        if (!hasMethodBody) return;
        if (_resolved) return;
        _resolved = true;

        if (_methodBase.GetMethodBody() == null) return;

        try
        {
            _bodyParser = new ILBodyParser.ILBodyParser(_methodBase);
            _bodyParser.Parse();

            Cfg = new CFG(_bodyParser.Instructions, _bodyParser.EhClauses);
            _basicBlocks = Cfg.BasicBlocks;
            foreach (var block in _basicBlocks) block.AttachToMethod(this);
        }
        catch (Exception e)
        {
            Console.WriteLine("MethodMeta error at " + (_methodBase.ReflectedType ?? _methodBase.DeclaringType)!.Name +
                              " " + _methodBase.Name + " " + e);
        }
    }

    public string DeclaringTypeName => _typeMeta.Name;
    public string Name => _methodBase.Name;
    public Type? ReturnType => (_methodBase as MethodInfo)?.ReturnType;

    public override int GetHashCode()
    {
        return _methodBase.GetHashCode();
    }
}

// TODO use instead of MethodInfo pass
public class MethodParameterMeta(ParameterInfo parameterInfo)
{
    private ParameterInfo _parameterInfo = parameterInfo;
}
