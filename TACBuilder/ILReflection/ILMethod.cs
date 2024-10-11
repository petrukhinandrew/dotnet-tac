using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;
using TACBuilder.BodyBuilder;
using TACBuilder.ILTAC.TypeSystem;
using TACBuilder.Utils;

namespace TACBuilder.ILReflection;

public class ILMethod(MethodBase methodBase) : ILMember(methodBase)
{
    private readonly MethodBase _methodBase = methodBase;

    public interface IParameter
    {
        public string? Name { get; }
        public ILType IlType { get; }
        public List<ILType> Attributes { get; }
        public object? DefaultValue { get; }
    }

    public class Parameter(ParameterInfo parameterInfo, int index) : ILCacheable, IParameter
    {
        private readonly ParameterInfo _parameterInfo = parameterInfo;
        public string Name { get; private set; } = parameterInfo.Name ?? NamingUtil.ArgVar(index);
        public string FullName => fullName();
        public ILType IlType { get; private set; }
        public List<ILType> Attributes { get; } = new();
        public object? DefaultValue { get; private set; }

        private bool IsOut => parameterInfo.IsOut;
        private bool IsIn => parameterInfo.IsIn;
        private bool IsRetVal => parameterInfo.IsRetval;

        public override void Construct()
        {
            IlType = ILInstanceBuilder.GetType(_parameterInfo.ParameterType);
            DefaultValue = _parameterInfo.DefaultValue;
            var attributes = _parameterInfo.CustomAttributes;
            foreach (var attribute in attributes)
            {
                Attributes.Add(ILInstanceBuilder.GetType(attribute.AttributeType));
            }
        }

        private string fullName()
        {
            if (IsOut) return $"out {IlType} {Name}";
            if (IsIn) return $"in {IlType} {Name}";
            if (IsRetVal) return $"retval {IlType} {Name}";
            return $"{IlType} {Name}";
        }

        public override string ToString()
        {
            return FullName;
        }
    }

    public class This(ILType ilType) : ILCacheable, IParameter
    {
        public string? Name => "this";
        public ILType IlType => ilType;
        public List<ILType> Attributes { get; } = new();
        public object? DefaultValue => null;

        public override void Construct()
        {
        }

        public override string ToString()
        {
            return $"{IlType} {Name}";
        }
    }

    public class ILBody(ILMethod method) : ILCacheable
    {
        private ILBodyParser _bodyParser;
        private CFG _cfg;

        public ILInstr Instructions => _bodyParser.Instructions;
        public List<ehClause> EhClauses => _bodyParser.EhClauses;
        public List<ILBasicBlock> BasicBlocks => _cfg.BasicBlocks;
        public List<int> StartBlocksIndices => _cfg.StartBlocksIndices;

        public override void Construct()
        {
            _bodyParser = new ILBodyParser(method._methodBase);
            _bodyParser.Parse();
            _cfg = new CFG(_bodyParser.Instructions, _bodyParser.EhClauses);

            foreach (var bb in _cfg.BasicBlocks)
            {
                bb.AttachToMethod(method);
            }

            method._tacBody = ILInstanceBuilder.GetMethodTacBody(method);
        }
    }

    public class TACBody(ILMethod method) : ILCacheable
    {
        public List<ILIndexedStmt> Lines { get; private set; }

        public override void Construct()
        {
            method.Locals.AddRange(
                method.LocalVarsType.Select((lvt, idx) => new ILLocal(lvt, NamingUtil.LocalVar(idx))));
            var builder = new MethodBuilder(method);
            Lines = builder.Build();
        }
    }

    public ILType? DeclaringType { get; private set; }
    public List<ILType> Attributes { get; } = new();
    public List<ILType> GenericArgs { get; } = new();
    public ILType? ReturnType { get; private set; }
    public List<IParameter> Parameters { get; } = new();
    public List<ILType> LocalVarsType { get; } = new();
    public bool HasMethodBody { get; private set; }
    public bool HasThis { get; private set; }
    public new string Name => _methodBase.Name;
    public bool IsGeneric => _methodBase.IsGenericMethod;
    public new bool IsConstructed = false;

    public TACBody? Body => _tacBody;

    public List<ILBasicBlock> BasicBlocks => _ilBody.BasicBlocks;
    public List<int> StartBlocksIndices => _ilBody.StartBlocksIndices;
    public ILInstr FirstInstruction => _ilBody.Instructions;
    public List<ehClause> EhClauses => _ilBody.EhClauses;

    public List<ILLocal> Locals = new();
    public Dictionary<int, TempVar> Temps = new();
    public List<ILExpr> Errs = new();
    public List<EHScope> Scopes = new();

    private ILBody _ilBody;
    private TACBody? _tacBody;

    private CFG _cfg;
    private ILBodyParser _bodyParser;

    public override void Construct()
    {
        DeclaringType = ILInstanceBuilder.GetType((_methodBase.ReflectedType ?? _methodBase.DeclaringType)!);
        Logger.LogInformation("Constructing {Type} {Name}", DeclaringType.Name, Name);
        var attributes = _methodBase.CustomAttributes;
        foreach (var attribute in attributes)
        {
            Attributes.Add(ILInstanceBuilder.GetType(attribute.AttributeType));
        }

        if (_methodBase.IsGenericMethod)
        {
            var genericArgs = _methodBase.GetGenericArguments();
            foreach (var arg in genericArgs)
            {
                GenericArgs.Add(ILInstanceBuilder.GetType(arg));
            }
        }

        if (_methodBase is MethodInfo methodInfo && methodInfo.ReturnType != typeof(void))
            ReturnType = ILInstanceBuilder.GetType(methodInfo.ReturnType);
        Debug.Assert(Parameters.Count == 0);
        if (!_methodBase.IsStatic)
        {
            Parameters.Add(ILInstanceBuilder.GetThisParameter(DeclaringType));
        }

        HasThis = Parameters.Count == 1;
        var methodParams = _methodBase.GetParameters()
            .OrderBy(parameter => parameter.Position);

        foreach (var methodParam in methodParams)
        {
            Parameters.Add(ILInstanceBuilder.GetMethodParameter(methodParam, Parameters.Count));
        }

        DeclaringType.EnsureMethodAttached(this);
        if (ILInstanceBuilder.MethodFilters.Any(f => !f(_methodBase))) return;
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
                LocalVarsType.Add(ILInstanceBuilder.GetType(locVar.LocalType));
            }

            _ilBody = ILInstanceBuilder.GetMethodIlBody(this);
        }

        IsConstructed = true;
    }

    public override bool Equals(object? obj)
    {
        return obj is ILMethod other && _methodBase == other._methodBase;
    }

    public override int GetHashCode()
    {
        return _methodBase.GetHashCode();
    }
}

// public partial class ILMethod
// {
//
// }

public class TempVar(int index, ILExpr value) : ILLValue
{
    public int Index => index;
    public ILExpr Value { get; private set; } = value;
    public ILType Type { get; private set; } = value.Type;
    private List<int> _accessors = new();
    private bool _isMerged = false;

    public void AccessFrom(int bb)
    {
        _accessors.Add(bb);
    }

    public override string ToString()
    {
        return NamingUtil.TempVar(index);
    }
}
