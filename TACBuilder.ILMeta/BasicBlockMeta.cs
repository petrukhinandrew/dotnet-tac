using TACBuilder.ILMeta.ILBodyParser;

namespace TACBuilder.ILMeta;

public class BasicBlockMeta(ILInstr entry, ILInstr exit)
{
    public ILInstr Entry => entry;

    public ILInstr Exit => exit;

    public List<int> Successors = new List<int>();
    public List<int> Predecessors = new List<int>();
    public Type? StackErrType;

    public override bool Equals(object? obj)
    {
        return obj is BasicBlockMeta bb && bb.Entry == Entry && bb.Exit == Exit;
    }

    public override int GetHashCode()
    {
        return Entry.idx;
    }
}