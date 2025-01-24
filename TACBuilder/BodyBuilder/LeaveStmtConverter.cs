using TACBuilder.ILTAC.TypeSystem;

namespace TACBuilder.BodyBuilder;

public class LeaveStmtConverter
{
    public List<IlStmt> Process(List<IlStmt> lines)
    {
        return lines.Select(stmt => stmt switch
        {
            IlLeaveStmt leaveStmt => new IlGotoStmt(leaveStmt.Target),
            { } other => other
        }).ToList();
    }
}