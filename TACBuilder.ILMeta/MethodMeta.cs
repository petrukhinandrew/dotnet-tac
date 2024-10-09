using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.CompilerServices;
using TACBuilder.ILMeta.ILBodyParser;
using TACBuilder.Utils;

namespace TACBuilder.ILMeta;

public class MethodMeta(MethodBase methodBase) : MemberMeta(methodBase)
{
    private readonly MethodBase _methodBase = methodBase;

    public interface ParameterMeta
    {
        public string? Name { get; }
        public TypeMeta Type { get; }
        public List<TypeMeta> Attributes { get; }
        public object? DefaultValue { get; }
    }

    public class Parameter(ParameterInfo parameterInfo, int index) : CacheableMeta, ParameterMeta
    {
        private readonly ParameterInfo _parameterInfo = parameterInfo;
        public string Name { get; } = parameterInfo.Name ?? NamingUtil.ArgVar(index);
        public TypeMeta Type { get; private set; }
        public List<TypeMeta> Attributes { get; } = new();
        public object? DefaultValue { get; private set; }

        public override void Construct()
        {
            Type = MetaBuilder.GetType(_parameterInfo.ParameterType);
            DefaultValue = _parameterInfo.DefaultValue;
            var attributes = _parameterInfo.CustomAttributes;
            foreach (var attribute in attributes)
            {
                Attributes.Add(MetaBuilder.GetType(attribute.AttributeType));
            }
        }
    }

    public class This(TypeMeta typeMeta) : CacheableMeta, ParameterMeta
    {
        public string? Name => "this";
        public TypeMeta Type => typeMeta;
        public List<TypeMeta> Attributes { get; } = new();
        public object? DefaultValue => null;

        public override void Construct()
        {
        }
    }

    public TypeMeta? DeclaringType { get; private set; }
    public List<TypeMeta> Attributes { get; } = new();
    public List<TypeMeta> GenericArgs { get; } = new();
    public TypeMeta? ReturnType { get; private set; }
    public List<ParameterMeta> Parameters { get; } = new();
    public List<TypeMeta> LocalVarsType { get; } = new();
    public bool HasMethodBody { get; private set; }
    public bool HasThis { get; private set; }
    public new string Name => _methodBase.Name;
    public new bool IsConstructed = false;

    public List<BasicBlockMeta> BasicBlocks => _cfg.BasicBlocks;
    public List<int> StartBlocksIndices => _cfg.StartBlocksIndices;
    public ILInstr FirstInstruction => _bodyParser.Instructions;
    public List<ehClause> EhClauses => _bodyParser.EhClauses;

    // TODO cfg must be private
    private CFG.CFG _cfg;
    private ILBodyParser.ILBodyParser _bodyParser;

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
        Debug.Assert(Parameters.Count == 0);
        if (!_methodBase.IsStatic)
        {
            Parameters.Add(MetaBuilder.GetThisParameter(DeclaringType));
        }

        HasThis = Parameters.Count == 1;
        var methodParams = _methodBase.GetParameters()
            .OrderBy(parameter => parameter.Position);

        foreach (var methodParam in methodParams)
        {
            Parameters.Add(MetaBuilder.GetMethodParameter(methodParam, Parameters.Count));
        }

        DeclaringType.EnsureMethodAttached(this);
        if (MetaBuilder.MethodFilters.Any(f => !f(_methodBase))) return;
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
            foreach (var locVar in _methodBase.GetMethodBody().LocalVariables.OrderBy(localVar => localVar.LocalIndex))
            {
                LocalVarsType.Add(MetaBuilder.GetType(locVar.LocalType));
            }

            Resolve();
        }

        IsConstructed = true;
    }

    private void Resolve()
    {
        _bodyParser = new ILBodyParser.ILBodyParser(_methodBase);
        _bodyParser.Parse();

        _cfg = new CFG.CFG(_bodyParser.Instructions, _bodyParser.EhClauses);

        foreach (var block in BasicBlocks) block.AttachToMethod(this);
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
