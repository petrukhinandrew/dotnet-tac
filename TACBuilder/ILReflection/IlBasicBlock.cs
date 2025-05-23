using TACBuilder.BodyBuilder;
using TACBuilder.BodyBuilder.ILBodyParser;

namespace TACBuilder.ILReflection;

public class IlBasicBlock(IlInstr entry, IlInstr exit)
{
    public IlInstr Entry => entry;
    public IlInstr Exit => exit;
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
