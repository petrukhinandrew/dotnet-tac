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
    public readonly MethodBase _methodBase = methodBase;

    public interface IParameter
    {
        public string? FullName { get; }
        public string? Name { get; }
        public int Position { get; }
        public IlType Type { get; }
        public List<IlAttribute> Attributes { get; }
        public IlConstant DefaultValue { get; }
    }

    public class Parameter(ParameterInfo parameterInfo, int index) : IlCacheable, IParameter
    {
        public string Name { get; private set; } = parameterInfo.Name ?? NamingUtil.ArgVar(index);
        public int Position => index;
        public string FullName => fullName();
        public IlType Type { get; } = IlInstanceBuilder.GetType(parameterInfo.ParameterType);

        public List<IlAttribute> Attributes { get; } =
            parameterInfo.CustomAttributes.Select(IlInstanceBuilder.GetAttribute).ToList();

        public IlConstant DefaultValue { get; } = IlConstant.From(parameterInfo.DefaultValue);
        public new bool IsConstructed = true;
        private bool IsOut => parameterInfo.IsOut;
        private bool IsIn => parameterInfo.IsIn;
        private bool IsRetVal => parameterInfo.IsRetval;

        public override void Construct()
        {
            // Type = IlInstanceBuilder.GetType(parameterInfo.ParameterType);
            // DefaultValue = parameterInfo.DefaultValue;
            // Attributes = parameterInfo.CustomAttributes.Select(IlInstanceBuilder.GetAttribute).ToList();
            // IsConstructed = true;
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
        public string FullName => Name;
        public int Position => 0;
        public IlType Type => ilType;
        public List<IlAttribute> Attributes { get; } = new();
        public IlConstant DefaultValue => IlConstant.From(null);

        public override void Construct()
        {
            // TODO check may have attr
            IsConstructed = true;
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

            foreach (var clause in _bodyParser.EhClauses)
            {
                method.Scopes.Add(EhScope.FromClause(clause));
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

            if (!method.IsConstructed || Lines.Count == 0) return;

            // TODO introduce features chain or so 

            var inlineFeature = new TacFinallyInliner(method);
            inlineFeature.Transform();
            // TODO maybe introduce another feature
            Lines = Lines.Select(stmt => stmt switch
            {
                IlLeaveStmt lst => new IlGotoStmt(lst.Target),
                _ => stmt
            }).ToList();
        }
    }

    public IlType? DeclaringType { get; private set; }
    public IlMethod? BaseMethod { get; private set; }
    public List<IlAttribute> Attributes { get; private set; }
    public List<IlType> GenericArgs { get; } = new();
    public IlType? ReturnType { get; private set; }
    public List<IParameter> Parameters { get; } = new();
    public bool HasMethodBody { get; private set; }
    public bool HasThis { get; private set; }

    public new string Name => _methodBase.Name +
                              (_methodBase.IsGenericMethod ? $"`{_methodBase.GetGenericArguments().Length}" : "");

    public string Signature =>
        $"{ReturnType!.FullName} {DeclaringType?.FullName ?? ""}.{Name}{GenericSignatureExtra}({string.Join(",", Parameters.Select(p => p.Type.FullName))})";

    public string NonGenericSignature =>
        $"{ReturnType!.FullName} {DeclaringType?.FullName ?? ""}.{Name}({string.Join(",", Parameters.Select(p => p.Type.FullName))})";

    private string GenericSignatureExtra =>
        IsGeneric
            ? $"<{string.Join(",", GenericArgs.Select(t => t.Name))}>"
            : "";

    public bool IsGeneric => _methodBase.IsGenericMethod;
    
    public bool IsGenericMethodDefinition => _methodBase.IsGenericMethodDefinition;
    
    public bool IsStatic => _methodBase.IsStatic;
    
    public bool IsVirtual => _methodBase.IsVirtual;
    
    public bool IsAbstract => _methodBase.IsAbstract;
    
    public new bool IsConstructed = false;

    public TacBody? Body => _tacBody;

    public List<IlBasicBlock> BasicBlocks => _ilBody.BasicBlocks;
    public List<int> StartBlocksIndices => _ilBody.StartBlocksIndices;
    public ILInstr FirstInstruction => _ilBody.Instructions;
    public List<ehClause> EhClauses => _ilBody.EhClauses;

    public List<IlLocalVar> LocalVars = new();
    public Dictionary<int, IlTempVar> Temps = new();
    public List<IlErrVar> Errs = new();
    public List<EhScope> Scopes = new();

    private IlBody _ilBody;
    private TacBody? _tacBody;
    private CFG _cfg;
    private ILBodyParser _bodyParser;
    public int ModuleToken => _methodBase.MetadataToken;
    public int MetadataToken => _methodBase.MetadataToken;

    public override void Construct()
    {
        if (IsConstructed) return;
        DeclaringType = IlInstanceBuilder.GetType((_methodBase.ReflectedType ?? _methodBase.DeclaringType)!);
        if (_methodBase is MethodInfo methodInfo)
        {
            BaseMethod = GetBaseMethod(methodInfo);
            ReturnType = IlInstanceBuilder.GetType(methodInfo.ReturnType);
        }
        else
        {
            ReturnType = IlInstanceBuilder.GetType(typeof(void));
        }
        
        Logger.LogInformation("Constructing {Type} {Name}", DeclaringType.Name, Name);
        Attributes = _methodBase.CustomAttributes.Select(IlInstanceBuilder.GetAttribute).ToList();

        if (_methodBase.IsGenericMethod)
        {
            var genericArgs = _methodBase.GetGenericArguments();
            foreach (var arg in genericArgs)
            {
                GenericArgs.Add(IlInstanceBuilder.GetType(arg));
            }
        }
        
        Debug.Assert(Parameters.Count == 0);

        var explicitThis = _methodBase.CallingConvention.HasFlag(CallingConventions.ExplicitThis);
        if (_methodBase.CallingConvention.HasFlag(CallingConventions.HasThis))
        {
            HasThis = true;
            var thisParamType = explicitThis
                ? IlInstanceBuilder.GetType(_methodBase.GetParameters()[0].ParameterType)
                : DeclaringType;
            Parameters.Add(IlInstanceBuilder.GetThisParameter(thisParamType));
        }

        var methodParams = _methodBase.GetParameters()
            .OrderBy(parameter => parameter.Position).ToList();

        if (explicitThis) methodParams = methodParams.Skip(1).ToList();
        foreach (var methodParam in methodParams)
        {
            Parameters.Add(IlInstanceBuilder.GetMethodParameter(methodParam, Parameters.Count));
        }

        DeclaringType.EnsureMethodAttached(this);
        if (IlInstanceBuilder.MethodFilters.All(f => !f(_methodBase))) return;
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
            foreach (var locVar in _methodBase.GetMethodBody()!.LocalVariables.OrderBy(localVar => localVar.LocalIndex))
            {
                LocalVars.Add(new IlLocalVar(IlInstanceBuilder.GetType(locVar.LocalType), locVar.LocalIndex,
                    locVar.IsPinned));
            }

            _ilBody = IlInstanceBuilder.GetMethodIlBody(this);
        }

        // TODO may be improper decision
        IsConstructed = true;
    }

    private IlMethod GetBaseMethod(MethodInfo methodInfo)
    {
        if (!methodInfo.IsVirtual) return this;

        var baseDefn = methodInfo.IsGenericMethod ? methodInfo.GetGenericMethodDefinition() : methodInfo.GetBaseDefinition();
        var baseDefnDeclType = baseDefn.DeclaringType;
        if (baseDefnDeclType?.IsInterface == true) return IlInstanceBuilder.GetMethod(baseDefn);
        foreach (var iface in baseDefnDeclType!.GetInterfaces())
        {
            var mapping = baseDefnDeclType.GetInterfaceMap(iface);
            var baseMethod = mapping.InterfaceMethods.SingleOrDefault(method => method?.Name == baseDefn.Name, defaultValue: null);
            if (baseMethod != null) return IlInstanceBuilder.GetMethod(baseMethod);
        }
        return IlInstanceBuilder.GetMethod(baseDefn);
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