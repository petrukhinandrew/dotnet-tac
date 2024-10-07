using TACBuilder.ILMeta.ILBodyParser;

namespace TACBuilder.ILMeta;

public class BasicBlockMeta(ILInstr entry, ILInstr exit)
{
    public ILInstr Entry => entry;
    public ILInstr Exit => exit;
    private MethodMeta? _methodMeta;
    public MethodMeta? MethodMeta => _methodMeta;

    public List<int> Successors = new();
    public List<int> Predecessors = new();
    public Type? StackErrType;

    public void AttachToMethod(MethodMeta methodMeta)
    {
        _methodMeta = methodMeta;
    }

    public override bool Equals(object? obj)
    {
        return obj is BasicBlockMeta bb && bb.Entry == Entry && bb.Exit == Exit;
    }

    public override int GetHashCode()
    {
        return Entry.idx;
    }
}
