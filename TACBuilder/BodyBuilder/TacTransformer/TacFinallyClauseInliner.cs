using TACBuilder.Exprs;
using TACBuilder.ILReflection;

namespace TACBuilder.BodyBuilder.TacTransformer;

public class TacFinallyClauseInliner : TacMutatingTransformer
{
    public IlMethod Transform(IlMethod method)
    {
        if (method.Body is { Lines.Count: 0 }) return method;
        var indexTransformer = new TacLinesTransformerIndexImpl(method);
        var lines = method.Body?.Lines;
        if (lines == null) return method;
        var init = new IlStmt[lines.Count];
        lines.CopyTo(init, 0);
        var finallyScopes = GetFinallyScopesSortedByNesting(method);

        if (finallyScopes.Count == 0) return method;
        foreach (var scope in finallyScopes)
        {
            var handlerSize = scope.tacLoc.he - scope.tacLoc.hb + 1;
            var inlinePos = scope.tacLoc.he + 1;
            for (var i = scope.tacLoc.tb; i <= scope.tacLoc.te; i++)
            {
                if (lines[i] is not IlLeaveStmt leaveStmt || leaveStmt.Target <= scope.tacLoc.te) continue;
                indexTransformer.DuplicateSlice(scope.tacLoc.hb, handlerSize, inlinePos);
                var leaveTarget = leaveStmt.Target;

                indexTransformer.ApplyToSlice(inlinePos, handlerSize,
                    stmt => stmt is IlEndFinallyStmt { IsMutable: true }
                        ? new IlGotoStmt(leaveTarget)
                        : stmt);
                var pos = inlinePos;
                indexTransformer.ApplyToSlice(scope.tacLoc.tb, scope.tacLoc.te - scope.tacLoc.tb + 1,
                    stmt => stmt is IlLeaveStmt ls && ls.Target == leaveTarget
                        ? new IlGotoStmt(pos)
                        : stmt);
                inlinePos += handlerSize;
            }

            FixInitialFinallyScope(method, scope.tacLoc.hb, scope.tacLoc.he);
        }

        return method;
    }

    /*
     * Args are segment ends inclusively
     */
    private static void FixInitialFinallyScope(IlMethod method, int begin, int end)
    {
        var lines = method.Body?.Lines;
        
        if (lines == null) 
            throw new ArgumentException($"No lines found in {method.Name}");
        
        for (var i = begin; i <= end; i++)
        {
            if (lines[i] is IlEndFinallyStmt) lines[i] = new IlEndFinallyStmt { IsMutable = false };
        }
    }

    private static List<FinallyScope> GetFinallyScopesSortedByNesting(IlMethod method) => 
        method.Scopes
        .Where(it => it is FinallyScope)
        .Order(new EhScopeNestingComparer()).Select(s => (FinallyScope)s)
        .ToList();
}
