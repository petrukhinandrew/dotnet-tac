using TACBuilder.ILReflection;

namespace TACBuilder.BodyBuilder.TacTransformer;

public class TacTransformersChain(List<TacMutatingTransformer> transformers)
{
    public IlMethod ApplyTo(IlMethod method)
    {
        return transformers.Aggregate(method, (current, t) => t.Transform(current));
    }
}