using System.Reflection;

namespace Usvm.IL.TypeSystem;

interface ILType { }

interface ILExpr
{
    ILType Type { get; }
    public string ToString();
}

interface ILValue : ILExpr { }



class ILNullValue : ILValue
{
    private static ILType _instance = new ILNull();
    public ILType Type => _instance;

    public override string ToString() => "null";
}

interface ILLValue : ILValue { }
class ILLocal(ILType type, string name) : ILLValue
{
    public ILType Type => type;

    public new string ToString() => name;

}

class ILObjectLiteral(ILType type, object? obj) : ILValue
{
    public object? Object = obj;

    public ILType Type => type;

    public override string ToString()
    {
        return Type.ToString() + " obj";
    }
}

class ILLiteral(ILType type, string value) : ILValue
{
    public ILType Type => type;
    public new string ToString() => value;

}

class ILMethod(ILType retType, string declType, string name, int argCount, ILExpr[] args) : ILValue
{


    public static ILMethod FromMethodBase(MethodBase mb)
    {
        int paramCount = mb.GetParameters().Where(p => !p.IsRetval).Count();
        Type retType = typeof(void);
        if (mb is MethodInfo methodInfo)
        {
            retType = methodInfo.ReturnType;
        }
        ILType ilRetType = TypeSolver.Resolve(retType);

        return new ILMethod(ilRetType, mb.DeclaringType?.FullName ?? "", mb.Name, paramCount, new ILExpr[paramCount]);
    }
    public void LoadArgs(Stack<ILExpr> stack)
    {
        for (int i = 0; i < _argCount; i++)
        {
            Args[i] = stack.Pop();
        }
    }
    public ILType ReturnType = retType;
    public string DeclaringType = declType;
    public string Name = name;
    private int _argCount = argCount;
    public ILExpr[] Args = args;

    public ILType Type => throw new NotImplementedException();

    // TODO declaringClass
    public override string ToString()
    {
        return string.Format("{0} {1}({2})", ReturnType.ToString(), Name, string.Join(", ", Args.Select(p => p.ToString())));
    }
    public bool IsInitializeArray()
    {
        return DeclaringType == "System.Runtime.CompilerServices.RuntimeHelpers" && Name == "InitializeArray";
    }
}