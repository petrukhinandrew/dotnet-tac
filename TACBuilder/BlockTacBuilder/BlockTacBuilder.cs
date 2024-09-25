using System.Diagnostics;
using System.Reflection;
using TACBuilder.ILMeta;
using TACBuilder.ILMeta.ILBodyParser;
using TACBuilder.ILTAC.TypeSystem;
using TACBuilder.Utils;

namespace Usvm.TACBuilder;

class BlockTacBuilder(MethodTacBuilder methodBuilder, BasicBlockMeta meta)
{
    public BasicBlockMeta Meta => meta;

    public bool BuiltAtLeastOnce => _builtAtLeastOnce;
    internal bool _builtAtLeastOnce = false;

    internal readonly ILInstr _firstInstr = meta.Entry;
    internal ILInstr CurInstr = meta.Entry;

    private EvaluationStack<ILExpr> _entryStackState =
        meta.StackErrType is null ? new() : new([methodBuilder.GetNewErr(meta.StackErrType)]);

    private EvaluationStack<ILExpr> _stack =
        meta.StackErrType is null ? new() : new([methodBuilder.GetNewErr(meta.StackErrType)]);

    private HashSet<BlockTacBuilder> _preds = new();
    private HashSet<BlockTacBuilder> _succs = new();
    public List<BlockTacBuilder> Successors => _succs.ToList();

    private readonly Dictionary<ILMerged, ILExpr> _extraAssignments = new();
    public readonly List<ILStmt> TacLines = new();

    public List<ILLocal> Locals => methodBuilder.Locals;
    public List<ILLocal> Params => methodBuilder.Params;
    public List<ILExpr> Temps => methodBuilder.Temps;
    public int ILFirst => _firstInstr.idx;
    public string MethodName => methodBuilder.MethodInfo.Name;
    public Type MethodReturnType => methodBuilder.MethodInfo.ReturnParameter.ParameterType;

    public void ConnectSuccsAndPreds(List<BlockTacBuilder> succs, List<BlockTacBuilder> preds)
    {
        _succs = succs.ToHashSet();
        _preds = preds.ToHashSet();
    }

    public bool StackInitIsTheSame()
    {
        if (_preds.Count == 0) return true;
        EvaluationStack<ILExpr> copy = EvaluationStack<ILExpr>.CopyOf(_entryStackState);

        var stacks = _preds.Where(bb => bb._builtAtLeastOnce)
            .Select((p, i) => (i, EvaluationStack<ILExpr>.CopyOf(p._stack))).ToList();
        List<ILExpr> newStack = new();
        var stackLengths = stacks.Select(p => p.Item2.Count).ToList();
        Debug.Assert(stackLengths.Max() == stackLengths.Min());
        for (int j = 0; j < stackLengths.Max(); j++)
        {
            var values = stacks.Select(s => s.Item2.Pop()).ToList();
            if (values.Distinct().Count() == 1)
            {
                newStack.Add(values[0]);
                continue;
            }

            ILMerged tmp = methodBuilder.GetMerged(ILFirst, j);
            tmp.MergeOf(values);
            foreach (var (i, p) in _preds.Select((v, i) => (i, v)))
            {
                p._extraAssignments[tmp] = values[i];
            }

            newStack.Add(tmp);
        }

        _entryStackState = new EvaluationStack<ILExpr>(newStack);
        return copy.SequenceEqual(_entryStackState);
    }

    public void ResetStackToInitial()
    {
        _stack = EvaluationStack<ILExpr>.CopyOf(_entryStackState);
    }

    public void InsertExtraAssignments()
    {
        var pos = TacLines.FindIndex(l => l is ILBranchStmt);
        pos = pos == -1 ? TacLines.Count : pos;
        TacLines.InsertRange(pos,
            _extraAssignments.OrderBy(p => p.Key.ToString())
                .Select(p => new ILAssignStmt(p.Key, p.Value)));
    }

    public ILExpr Pop()
    {
        return _stack.Pop();
    }

    // TODO check expr Type, if < ILInt => push ((ILInt) expr)
    public void Push(ILExpr expr)
    {
        _stack.Push(expr);
    }

    public void ClearStack()
    {
        _stack.Clear();
    }

    internal bool CurInstrIsLast()
    {
        return CurInstr == Meta.Exit;
    }

    public void NewLine(ILStmt line)
    {
        TacLines.Add(line);
    }

    public void PushLiteral<T>(T value)
    {
        ILLiteral lit = new ILLiteral(TypingUtil.ILTypeFrom(typeof(T)), value?.ToString() ?? "");
        Push(lit);
    }

    public ILLocal GetNewTemp(ILType type, ILExpr value)
    {
        return methodBuilder.GetNewTemp(type, value);
    }

    // TODO move to another extension or introduce new approach
    public FieldInfo ResolveField(int target)
    {
        return methodBuilder.ResolveField(target);
    }

    public Type ResolveType(int target)
    {
        return methodBuilder.ResolveType(target);
    }

    public MethodBase ResolveMethod(int target)
    {
        return methodBuilder.ResolveMethod(target);
    }

    // TODO find test case with calli
    public byte[] ResolveSignature(int target)
    {
        return methodBuilder.ResolveSignature(target);
    }

    public string ResolveString(int target)
    {
        return methodBuilder.ResolveString(target);
    }

    public MemberInfo ResolveMember(int target)
    {
        return methodBuilder.ResolveMember(target);
    }


    public override bool Equals(object? obj)
    {
        return obj is BlockTacBuilder f && ILFirst == f.ILFirst;
    }

    public override int GetHashCode()
    {
        return ILFirst;
    }

    public override string ToString()
    {
        return ILFirst.ToString();
    }
}