using TACBuilder.ILReflection;

namespace TACBuilder.Serialization;

public interface ITacSerializer
{
    public void Serialize();
    public void NewInstance(IlCacheable instance);
}
