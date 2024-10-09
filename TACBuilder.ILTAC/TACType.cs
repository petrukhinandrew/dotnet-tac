using TACBuilder.ILMeta;

namespace TACBuilder.ILTAC;

// public class TACTypeInfo(TypeMeta typeMeta)
// {
//     public List<TacField> Fields = typeMeta.Fields;
// }

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
