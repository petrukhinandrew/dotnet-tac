using TACBuilder.ILTAC.TypeSystem;

namespace TACBuilder.BodyBuilder;

public interface TacBodyPostProcessor
{
    public List<IlStmt> Process(List<IlStmt> lines);
}