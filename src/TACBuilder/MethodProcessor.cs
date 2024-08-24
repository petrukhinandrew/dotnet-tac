
using System.Reflection;
using Usvm.IL.TypeSystem;
using Usvm.IL.Parser;
using Microsoft.VisualBasic;

namespace Usvm.IL.TACBuilder;

class MethodProcessor
{
    public Module DeclaringModule;
    public MethodInfo MethodInfo;
    private ehClause[] _ehs;
    public List<ILLocal> Locals;
    public List<ILLocal> Params;
    public List<ILExpr> Temps = new List<ILExpr>();
    public List<ILExpr> Errs = new List<ILExpr>();
    public Dictionary<SMFrame, List<SMFrame>> Successors = new Dictionary<SMFrame, List<SMFrame>>();
    private ILInstr _begin;
    public List<EHScope> Scopes = [];

    private int _stmtIndex = 0;
    public int StmtIndex
    {
        get
        {
            return _stmtIndex++;
        }
    }

    public MethodProcessor(Module declaringModule, MethodInfo methodInfo, IList<LocalVariableInfo> locals, int maxDepth, ILInstr begin, ehClause[] ehs)
    {
        _begin = begin;
        _ehs = ehs;
        DeclaringModule = declaringModule;
        MethodInfo = methodInfo;
        Params = methodInfo.GetParameters().OrderBy(p => p.Position).Select(l => new ILLocal(TypeSolver.Resolve(l.ParameterType), Logger.ArgVarName(l.Position))).ToList();
        Locals = locals.OrderBy(l => l.LocalIndex).Select(l => new ILLocal(TypeSolver.Resolve(l.LocalType), Logger.LocalVarName(l.LocalIndex))).ToList();
        InitEHScopes();
        ProcessIL();
    }
    public ILInstr GetBeginInstr()
    {
        return _begin;
    }
    private void ProcessIL()
    {
        SMFrame initial = SMFrame.CreateInitial(this);
        List<SMFrame> branches = [initial];
        while (branches.Count > 0)
        {
            var newBranches = branches.First().Branch();
            Successors.GetValueOrDefault(branches.First(), []).AddRange(newBranches);
            branches.AddRange(newBranches);
            branches.RemoveAt(0);
        }
    }
    private void InitEHScopes()
    {
        foreach (var ehc in _ehs)
        {
            EHScope scope = EHScope.FromClause(ehc);
            if (!Scopes.Contains(scope))
            {
                if (scope is EHScopeWithVarIdx s)
                {
                    s.ErrIdx = Errs.Count;
                    Errs.Add(new ILLocal(TypeSolver.Resolve(s.Type), Logger.ErrVarName(s.ErrIdx)));
                }
                Scopes.Add(scope);
            }
        }
    }

    public FieldInfo ResolveField(int target)
    {
        return DeclaringModule.ResolveField(target, MethodInfo.DeclaringType!.GetGenericArguments(), MethodInfo.GetGenericArguments()) ?? throw new Exception("cannot resolve field");

    }
    public Type ResolveType(int target)
    {
        return DeclaringModule.ResolveType(target) ?? throw new Exception("cannot resolve type");
    }
    public MethodBase ResolveMethod(int target)
    {
        return DeclaringModule.ResolveMethod(target) ?? throw new Exception("cannot resolve method");
    }
    public byte[] ResolveSignature(int target)
    {
        return DeclaringModule.ResolveSignature(target);
    }
    public string ResolveString(int target)
    {
        return DeclaringModule.ResolveString(target);
    }
}