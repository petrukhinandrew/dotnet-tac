using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;
using TACBuilder.BodyBuilder;
using TACBuilder.Exprs;
using TACBuilder.ILTAC.TypeSystem;
using TACBuilder.Utils;

namespace TACBuilder.ILReflection;

public class IlMethod(MethodBase methodBase) : IlMember(methodBase)
{
    private readonly MethodBase _methodBase = methodBase;

    public interface IParameter
    {
        public string? Name { get; }
        public int Position { get; }
        public IlType Type { get; }
        public List<IlType> Attributes { get; }
        public object? DefaultValue { get; }
    }

    public class Parameter(ParameterInfo parameterInfo, int index) : IlCacheable, IParameter
    {
        public string Name { get; private set; } = parameterInfo.Name ?? NamingUtil.ArgVar(index);
        public int Position => index;
        public string FullName => fullName();
        public IlType Type { get; private set; }
        public List<IlType> Attributes { get; } = new();
        public object? DefaultValue { get; private set; }

        private bool IsOut => parameterInfo.IsOut;
        private bool IsIn => parameterInfo.IsIn;
        private bool IsRetVal => parameterInfo.IsRetval;

        public override void Construct()
        {
            Type = IlInstanceBuilder.GetType(parameterInfo.ParameterType);
            DefaultValue = parameterInfo.DefaultValue;
            var attributes = parameterInfo.CustomAttributes;
            foreach (var attribute in attributes)
            {
                Attributes.Add(IlInstanceBuilder.GetType(attribute.AttributeType));
            }
        }

        private string fullName()
        {
            if (IsOut) return $"out {Type} {Name}";
            if (IsIn) return $"in {Type} {Name}";
            if (IsRetVal) return $"retval {Type} {Name}";
            return $"{Type} {Name}";
        }

        public override string ToString()
        {
            return FullName;
        }

        public override int GetHashCode()
        {
            return parameterInfo.GetHashCode();
        }
    }

    public class This(IlType ilType) : IlCacheable, IParameter
    {
        public string Name => "this";
        public int Position => 0;
        public IlType Type => ilType;
        public List<IlType> Attributes { get; } = new();
        public object? DefaultValue => null;

        public override void Construct()
        {
        }

        public override string ToString()
        {
            return $"{Type} {Name}";
        }

        public override int GetHashCode()
        {
            return Type.GetHashCode() + Name.GetHashCode();
        }
    }

    public class IlBody(IlMethod method) : IlCacheable
    {
        private ILBodyParser _bodyParser;
        private CFG _cfg;

        public ILInstr Instructions => _bodyParser.Instructions;
        public List<ehClause> EhClauses => _bodyParser.EhClauses;
        public List<IlBasicBlock> BasicBlocks => _cfg.BasicBlocks;
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

            method._tacBody = IlInstanceBuilder.GetMethodTacBody(method);
        }
    }

    public class TacBody(IlMethod method) : IlCacheable
    {
        public List<IlStmt> Lines { get; private set; }

        public override void Construct()
        {
            var builder = new MethodBuilder(method);
            Lines = builder.Build();
        }
    }

    public IlType? DeclaringType { get; private set; }
    public List<IlType> Attributes { get; } = new();
    public List<IlType> GenericArgs { get; } = new();
    public IlType? ReturnType { get; private set; }
    public List<IParameter> Parameters { get; } = new();
    public bool HasMethodBody { get; private set; }
    public bool HasThis { get; private set; }
    public new string Name => _methodBase.Name;
    public bool IsGeneric => _methodBase.IsGenericMethod;
    public bool IsStatic => _methodBase.IsStatic;
    public new bool IsConstructed = false;

    public TacBody? Body => _tacBody;

    public List<IlBasicBlock> BasicBlocks => _ilBody.BasicBlocks;
    public List<int> StartBlocksIndices => _ilBody.StartBlocksIndices;
    public ILInstr FirstInstruction => _ilBody.Instructions;
    public List<ehClause> EhClauses => _ilBody.EhClauses;

    public List<IlLocalVar> LocalVars = new();
    public Dictionary<int, IlTempVar> Temps = new();
    public List<IlLocalVar> Errs = new();
    public List<EHScope> Scopes = new();

    private IlBody _ilBody;
    private TacBody? _tacBody;

    private CFG _cfg;
    private ILBodyParser _bodyParser;
    public int ModuleToken => _methodBase.MetadataToken;
    public int MetadataToken => _methodBase.MetadataToken;

    public override void Construct()
    {
        DeclaringType = IlInstanceBuilder.GetType((_methodBase.ReflectedType ?? _methodBase.DeclaringType)!);
        Logger.LogInformation("Constructing {Type} {Name}", DeclaringType.Name, Name);
        var attributes = _methodBase.CustomAttributes;
        foreach (var attribute in attributes)
        {
            Attributes.Add(IlInstanceBuilder.GetType(attribute.AttributeType));
        }

        if (_methodBase.IsGenericMethod)
        {
            var genericArgs = _methodBase.GetGenericArguments();
            foreach (var arg in genericArgs)
            {
                GenericArgs.Add(IlInstanceBuilder.GetType(arg));
            }
        }

        if (_methodBase is MethodInfo methodInfo && methodInfo.ReturnType != typeof(void))
            ReturnType = IlInstanceBuilder.GetType(methodInfo.ReturnType);
        Debug.Assert(Parameters.Count == 0);
        if (!_methodBase.IsStatic)
        {
            Parameters.Add(IlInstanceBuilder.GetThisParameter(DeclaringType));
        }

        HasThis = Parameters.Count == 1;
        var methodParams = _methodBase.GetParameters()
            .OrderBy(parameter => parameter.Position);

        foreach (var methodParam in methodParams)
        {
            Parameters.Add(IlInstanceBuilder.GetMethodParameter(methodParam, Parameters.Count));
        }

        DeclaringType.EnsureMethodAttached(this);
        if (IlInstanceBuilder.MethodFilters.Any(f => !f(_methodBase))) return;
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
            // TODO new instance for local var type
            Logger.LogDebug("Resolving body of {Type} {Name}", DeclaringType.Name, Name);
            foreach (var locVar in _methodBase.GetMethodBody().LocalVariables.OrderBy(localVar => localVar.LocalIndex))
            {
                LocalVars.Add(new IlLocalVar(IlInstanceBuilder.GetType(locVar.LocalType), locVar.LocalIndex,
                    locVar.IsPinned));
            }

            _ilBody = IlInstanceBuilder.GetMethodIlBody(this);
        }

        IsConstructed = true;
    }

    public override bool Equals(object? obj)
    {
        return obj is IlMethod other && _methodBase == other._methodBase;
    }

    public override int GetHashCode()
    {
        return _methodBase.GetHashCode();
    }
}

public class IlTempVar(int index, IlExpr value) : IlValue
{
    public int Index => index;
    public IlExpr Value => value;
    public IlType Type => value.Type;

    public override string ToString()
    {
        return NamingUtil.TempVar(index);
    }
}
