using System.Reflection;
using Microsoft.Extensions.Logging;
using TACBuilder.ILMeta.ILBodyParser;

namespace TACBuilder.ILMeta;

public class MethodMeta(MethodBase methodBase) : MemberMeta(methodBase)
{
    private readonly MethodBase _methodBase = methodBase;
    private ILBodyParser.ILBodyParser _bodyParser;
    private List<BasicBlockMeta> _basicBlocks = new();

    public MethodBase MethodBase => _methodBase;
    public TypeMeta? DeclaringType { get; private set; }
    public List<TypeMeta> Attributes { get; } = new();
    public List<TypeMeta> GenericArgs { get; } = new();
    public TypeMeta? ReturnType { get; private set; }
    public List<TypeMeta> ParametersType { get; } = new();
    public bool HasMethodBody { get; private set; }
    public bool HasThis { get; private set; }
    public new string Name => _methodBase.Name;
    public new bool IsConstructed = false;

    public List<BasicBlockMeta> BasicBlocks => _basicBlocks;
    public List<int> StartBlocksIndices => Cfg.StartBlocksIndices;
    public ILInstr FirstInstruction => _bodyParser.Instructions;
    public List<ehClause> EhClauses => _bodyParser.EhClauses;

    // TODO cfg must be private
    public CFG.CFG Cfg;

    public override void Construct()
    {
        DeclaringType = MetaBuilder.GetType((_methodBase.ReflectedType ?? _methodBase.DeclaringType)!);
        Logger.LogInformation("Constructing {Type} {Name}", DeclaringType.Name, Name);
        var attributes = _methodBase.CustomAttributes;
        foreach (var attribute in attributes)
        {
            Attributes.Add(MetaBuilder.GetType(attribute.AttributeType));
        }

        if (_methodBase.IsGenericMethod)
        {
            var genericArgs = _methodBase.GetGenericArguments();
            foreach (var arg in genericArgs)
            {
                GenericArgs.Add(MetaBuilder.GetType(arg));
            }
        }

        if (_methodBase is MethodInfo methodInfo)
            ReturnType = MetaBuilder.GetType(methodInfo.ReturnType);

        if (_methodBase.CallingConvention.HasFlag(CallingConventions.HasThis))
            ParametersType.Add(DeclaringType);

        HasThis = ParametersType.Count == 1;
        var methodParams = _methodBase.GetParameters()
            .OrderBy(parameter => parameter.Position + ParametersType.Count);

        foreach (var methodParam in methodParams)
        {
            ParametersType.Add(MetaBuilder.GetType(methodParam.ParameterType));
        }

        // TODO introduce new type for parameter
        DeclaringType.EnsureMethodAttached(this);
        if (MetaBuilder.MethodFilters.All(f => !f(_methodBase))) return;
        try
        {
            HasMethodBody = _methodBase.GetMethodBody() != null;
        }
        catch
        {
            HasMethodBody = false;
        }

        if (HasMethodBody)
        {
            Logger.LogDebug("Resolving body of {Type} {Name}", DeclaringType.Name, Name);
            Resolve();
        }

        IsConstructed = true;
    }

    private void Resolve()
    {
        _bodyParser = new ILBodyParser.ILBodyParser(_methodBase);
        _bodyParser.Parse();

        Cfg = new CFG.CFG(_bodyParser.Instructions, _bodyParser.EhClauses);
        _basicBlocks = Cfg.BasicBlocks;
        foreach (var block in _basicBlocks) block.AttachToMethod(this);
    }

    public override bool Equals(object? obj)
    {
        return obj is MethodMeta other && _methodBase == other._methodBase;
    }

    public override int GetHashCode()
    {
        return _methodBase.GetHashCode();
    }
}
