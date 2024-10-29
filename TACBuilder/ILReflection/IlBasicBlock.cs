using TACBuilder.BodyBuilder;

namespace TACBuilder.ILReflection;

public class IlBasicBlock(ILInstr entry, ILInstr exit)
{
    public ILInstr Entry => entry;
    public ILInstr Exit => exit;
    private IlMethod? _methodMeta;
    public IlMethod? MethodMeta => _methodMeta;

    public List<int> Successors = new();
    public List<int> Predecessors = new();
    public Type? StackErrType;

    public void AttachToMethod(IlMethod ilMethod)
    {
        _methodMeta = ilMethod;
    }

    public override bool Equals(object? obj)
    {
        return obj is IlBasicBlock bb && bb.Entry == Entry && bb.Exit == Exit;
    }

    public override int GetHashCode()
    {
        return Entry.idx;
    }
}
