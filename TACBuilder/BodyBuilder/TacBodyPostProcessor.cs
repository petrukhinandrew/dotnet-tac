using TACBuilder.Exprs;

namespace TACBuilder.BodyBuilder;

public interface TacBodyPostProcessor
{
    public List<IlStmt> Process(List<IlStmt> lines);
}