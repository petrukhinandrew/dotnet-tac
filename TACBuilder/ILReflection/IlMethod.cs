using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;
using TACBuilder.BodyBuilder;
using TACBuilder.BodyBuilder.TacTransformer;
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

        public List<IlAttribute> Attributes => parameterInfo.CustomAttributes.Select(IlInstanceBuilder.GetAttribute).ToList();

        public IlConstant DefaultValue => IlConstant.From(parameterInfo.DefaultValue);
        
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
            return (IsRetVal ? "retval " : "") + $"{Type} {Name}";
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

    public class IlBody : IlCacheable
    {
        private readonly ILBodyParser _bodyParser;
        private readonly CFG _cfg;

        public IlBody(IlMethod method)
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

        }

        public ILInstr Instructions => _bodyParser.Instructions;
        public List<ehClause> EhClauses => _bodyParser.EhClauses;
        public List<IlBasicBlock> BasicBlocks => _cfg.BasicBlocks;
        public List<int> StartBlocksIndices => _cfg.StartBlocksIndices;

        public override void Construct()
        {
        }
    }

    public class TacBody : IlCacheable
    {
        private readonly IlMethod _method;
        public List<IlStmt> Lines { get; internal set; }

        private readonly TacTransformersChain _transformersChain = new([
            new TacFinallyClauseInliner(),
            new TacLeaveStmtEliminator()
        ]);
        public TacBody(IlMethod method)
        {
            _method = method;
            var builder = new MethodBuilder(_method);
            Lines = builder.Build();
            
        }

        public override void Construct()
        {
            if (!_method.IsConstructed || Lines.Count == 0) return;
            var transformed = _transformersChain.ApplyTo(_method);
            Debug.Assert(Equals(transformed, _method));
        }
    }

    private static IlType ResolveDeclaringType(MethodBase methodBase) => IlInstanceBuilder.GetType((methodBase.ReflectedType ?? methodBase.DeclaringType)!);
    public readonly IlType DeclaringType = ResolveDeclaringType(methodBase);
    public readonly List<IlType> GenericArgs = methodBase.IsGenericMethod ? methodBase.GetGenericArguments().Select(arg => IlInstanceBuilder.GetType(arg)).ToList() : [];
    public readonly IlType ReturnType = methodBase is MethodInfo methodInfo ? IlInstanceBuilder.GetType(methodInfo.ReturnType) : IlInstanceBuilder.GetType(typeof(void));
    public readonly List<IParameter> Parameters = ConstructParameters(methodBase);
    
    public IlMethod? BaseMethod { get; private set; }
    public List<IlAttribute> Attributes { get; private set; } = [];
    
    public bool HasMethodBody { get; private set; }
    public readonly bool HasThis = methodBase.CallingConvention.HasFlag(CallingConventions.HasThis);

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

    private static List<IParameter> ConstructParameters(MethodBase methodBase)
    {
        List<IParameter> res = [];
        if (methodBase.CallingConvention.HasFlag(CallingConventions.HasThis) || methodBase.CallingConvention.HasFlag(CallingConventions.ExplicitThis))
        {
            var declaringType = ResolveDeclaringType(methodBase);
            res.Add(IlInstanceBuilder.GetThisParameter(declaringType));
        }

        var parameters = methodBase.GetParameters()
            .OrderBy(parameter => parameter.Position).Select(IParameter (it, idx) => IlInstanceBuilder.GetMethodParameter(it, idx)).ToList();
        res.AddRange(parameters);
        return res;
    }
    
    public override void Construct()
    {
        if (IsConstructed) return;
        
        if (_methodBase is MethodInfo methodInfo)
        {
            BaseMethod = GetBaseMethod(methodInfo);
        }

        Logger.LogInformation("Constructing {Type} {Name}", DeclaringType.Name, Name);
        Attributes = _methodBase.CustomAttributes.Select(IlInstanceBuilder.GetAttribute).ToList();

        var explicitThis = _methodBase.CallingConvention.HasFlag(CallingConventions.ExplicitThis);
        if (explicitThis) throw new Exception("explicit this convention found");

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
            _tacBody = IlInstanceBuilder.GetMethodTacBody(this);
        }

        // TODO may be improper decision
        IsConstructed = true;
    }

    private IlMethod GetBaseMethod(MethodInfo methodInfo)
    {
        if (!methodInfo.IsVirtual) return this;

        var baseDefn = methodInfo.IsGenericMethod
            ? methodInfo.GetGenericMethodDefinition()
            : methodInfo.GetBaseDefinition();
        var baseDefnDeclType = baseDefn.DeclaringType;

        if (baseDefnDeclType?.IsInterface == true) return IlInstanceBuilder.GetMethod(baseDefn);

        foreach (var iface in baseDefnDeclType!.GetInterfaces())
        {
            var mapping = baseDefnDeclType.GetInterfaceMap(iface);

            var baseMethod =
                mapping.InterfaceMethods.SingleOrDefault(method =>
                        method?.Name == baseDefn.Name &&
                        method.ReturnType == baseDefn.ReturnType &&
                        method.GetParameters().Select(p => p.ParameterType)
                            .SequenceEqual(baseDefn.GetParameters().Select(p => p.ParameterType)),
                    defaultValue: null);
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