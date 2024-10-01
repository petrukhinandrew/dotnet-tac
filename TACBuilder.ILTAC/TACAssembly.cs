namespace TACBuilder.ILTAC;

public class TACAssembly(IEnumerable<TACType> types) : TACInstance
{
    public void SerializeTo(Stream to)
    {
        foreach (var t in types)
        {
            t.SerializeTo(to);
        }
    }
}