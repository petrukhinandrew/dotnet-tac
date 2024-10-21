namespace TACBuilder.ILReflection;

public class ILConstructQueue : Queue<ILCacheable>
{
    public new void Enqueue(ILCacheable item)
    {
        base.Enqueue(item);
    }
}
