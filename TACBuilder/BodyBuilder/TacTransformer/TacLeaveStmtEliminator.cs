using TACBuilder.Exprs;
using TACBuilder.ILReflection;

namespace TACBuilder.BodyBuilder.TacTransformer;

public class TacLeaveStmtEliminator : TacMutatingTransformer
{
    public IlMethod Transform(IlMethod method)
    {
        var lines = method.Body?.Lines;
        if (lines == null) return method;
        lines = lines.Select(stmt => stmt switch
        {
            IlLeaveStmt lst => new IlGotoStmt(lst.Target),
            _ => stmt
        }).ToList();
        
        method.Body!.Lines = lines;
        return method;
    }
}