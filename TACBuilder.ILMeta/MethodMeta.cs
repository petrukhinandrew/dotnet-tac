using System.Reflection;
using TACBuilder.ILMeta.ILBodyParser;

namespace TACBuilder.ILMeta;

public class MethodMeta(MethodBase methodBase) : MemberMeta(methodBase)
{
    private MethodBase _methodBase = methodBase;
    private ILBodyParser.ILBodyParser _bodyParser;
    private List<BasicBlockMeta> _basicBlocks = new();

    public MethodBase MethodBase => _methodBase;
    public TypeMeta DeclaringType { get; private set; }
    public TypeMeta? ReturnType { get; private set; }
    public List<TypeMeta> ParametersType { get; private set; }
    public bool HasMethodBody { get; private set; }
    public bool HasThis { get; private set; }
    public string Name => _methodBase.Name;

    public List<BasicBlockMeta> BasicBlocks => _basicBlocks;
    public List<int> StartBlocksIndices => Cfg.StartBlocksIndices;
    public ILInstr FirstInstruction => _bodyParser.Instructions;
    public List<ehClause> EhClauses => _bodyParser.EhClauses;

    // TODO cfg must be private
    public CFG Cfg;

    public override void Construct()
    {
        DeclaringType = MetaCache.GetType((_methodBase.ReflectedType ?? _methodBase.DeclaringType)!);

        if (_methodBase is MethodInfo methodInfo)
            ReturnType = MetaCache.GetType(methodInfo.ReturnType);

        List<TypeMeta> thisParam = _methodBase.CallingConvention.HasFlag(CallingConventions.HasThis)
            ? new() { DeclaringType }
            : new();
        HasThis = thisParam.Count == 1;
        ParametersType = thisParam.Concat(_methodBase.GetParameters()
                .OrderBy(parameter => parameter.Position + thisParam.Count)
                .Select(parameter => MetaCache.GetType(parameter.ParameterType)))
            .ToList();
        try
        {
            HasMethodBody = _methodBase.GetMethodBody() != null;
        }
        catch
        {
            HasMethodBody = false;
        }

        // if (HasMethodBody) Resolve();
    }

    private void Resolve()
    {
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
}
