namespace TACBuilder.ILTAC;

public class TACType(IEnumerable<TACMethod> methods) : TACInstance
{
    public void SerializeTo(Stream to)
    {
        foreach (var m in methods)
        {
            m.SerializeTo(to);
        }
    }
}