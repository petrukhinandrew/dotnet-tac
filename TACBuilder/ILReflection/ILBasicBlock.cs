using TACBuilder.BodyBuilder;

namespace TACBuilder.ILReflection;

public class ILBasicBlock(ILInstr entry, ILInstr exit)
{
    public ILInstr Entry => entry;
    public ILInstr Exit => exit;
    private ILMethod? _methodMeta;
    public ILMethod? MethodMeta => _methodMeta;

    public List<int> Successors = new();
    public List<int> Predecessors = new();
    public Type? StackErrType;

    public void AttachToMethod(ILMethod ilMethod)
    {
        _methodMeta = ilMethod;
    }

    public override bool Equals(object? obj)
    {
        return obj is ILBasicBlock bb && bb.Entry == Entry && bb.Exit == Exit;
    }

    public override int GetHashCode()
    {
        return Entry.idx;
    }
}
