using TACBuilder.ILReflection;

namespace TACBuilder.BodyBuilder.TacTransformer;

public interface TacMutatingTransformer
{
    public IlMethod Transform(IlMethod method);
}
