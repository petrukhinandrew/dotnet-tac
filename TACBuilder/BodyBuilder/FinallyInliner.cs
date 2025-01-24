using System.Diagnostics;
using TACBuilder.ILTAC.TypeSystem;

namespace TACBuilder.BodyBuilder;

public class FinallyInliner(List<EhScope> scopes) : TacBodyPostProcessor
{
    public List<EhScope> Scopes = scopes;

    public List<IlStmt> Process(List<IlStmt> lines)
    {
        var init = new IlStmt[lines.Count];
        lines.CopyTo(init, 0);
        var finallyScopes = Scopes.Where(it => it is FinallyScope)
            .Order(new EhScopeNestingComparer()).Select(s => (FinallyScope)s).ToList();

        if (finallyScopes.Count == 0) return lines;
        foreach (var scope in finallyScopes)
        {
            // reserve finally scope
            var scopeSize = scope.tacLoc.he - scope.tacLoc.hb + 1;
            var scopeLines = new IlStmt[scopeSize];
            lines.CopyTo(scope.tacLoc.hb, scopeLines, 0, scopeSize);
            for (var i = scope.tacLoc.hb; i <= scope.tacLoc.he; i++)
                if (lines[i] is IlEndFinallyStmt)
                {
                    lines[i] = new IlEndFinallyStmt { IsMutable = false };
                }

            var shiftIdx = scope.tacLoc.hb;

            var currentInlineIdx = scope.tacLoc.he + 1;

            // actual inlining 
            for (var (i, delta) = (scope.tacLoc.tb, 0); i + delta <= scope.tacLoc.te; i++)
            {
                if (lines[i + delta] is not IlLeaveStmt leaveStmt || leaveStmt.Target <= scope.tacLoc.te) continue;
                // TODO check if it is not a jump over multiple finally 
                lines[i + delta] = new IlGotoStmt(currentInlineIdx);
                // TODO check goto or leave 
                lines.InsertRange(currentInlineIdx, scopeLines);
                
                // this should be done after moving all the scopes with shift right
                var duplicatedScopes = Scopes.Where(s => s.IsNestedInHandler(scope)).Select(s => s.ShiftedRightAt(currentInlineIdx - scope.tacLoc.hb)).ToList();
                Scopes.AddRange(duplicatedScopes);

                for (var j = 0; j < scopeSize; j++)
                {
                    if (lines[currentInlineIdx + j] is IlBranchStmt branch)
                    {
                        lines[currentInlineIdx + j] = branch switch
                        {
                            IlGotoStmt gt => new IlGotoStmt(branch.Target - shiftIdx + currentInlineIdx),
                            IlIfStmt ifStmt => new IlIfStmt(ifStmt.Condition,
                                branch.Target - shiftIdx + currentInlineIdx),
                            IlLeaveStmt ls => new IlLeaveStmt(branch.Target - shiftIdx + currentInlineIdx),
                            _ => throw new ArgumentOutOfRangeException()
                        };
                    }

                    if (lines[currentInlineIdx + j] is IlEndFinallyStmt { IsMutable: true })
                        lines[currentInlineIdx + j] = new IlGotoStmt(leaveStmt.Target + scopeSize);
                }

                Scopes.ForEach(s => { s.tacLoc.ShiftRight(currentInlineIdx, scopeSize); });

                foreach (var (line, idx) in lines.Indexed()
                             .Where((_, it) => it < currentInlineIdx || it >= currentInlineIdx + scopeSize))
                {
                    if (line is IlBranchStmt branch && branch.Target > currentInlineIdx)
                    {
                        branch.Target += scopeSize;
                    }
                }

                currentInlineIdx += scopeSize;
                delta += scopeSize;
            }
        }

        // TODO may fail somehow 
        // if (!Scopes.Where(s => s is FinallyScope)
        //     .All(s => lines[s.tacLoc.he] is IlEndFinallyStmt or IlThrowStmt))
        // {
        //     Console.WriteLine("finally err");
        // }
        // Scopes.RemoveAll(s => s is FinallyScope);
        return lines;
    }

    private void CheckJumpsAreValid(List<IlStmt> oldLines, List<IlStmt> newLines, List<int> newToOld)
    {
        foreach (var (newLine, idx) in newLines.Select((v, i) => (v, i)))
        {
            if (newLine is not IlBranchStmt branch) continue;
            var newTarget = branch.Target;
            var oldTargetExpected = newToOld[newTarget];
            var oldLineIdx = newToOld[idx];
            Debug.Assert(oldLines[oldLineIdx] is IlBranchStmt oldBranch && oldBranch.Target == oldTargetExpected);
        }
    }
}

public class EhScopeNestingComparer : IComparer<EhScope>
{
    public int Compare(EhScope? x, EhScope? y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return 1;
        if (y == null) return -1;
        if (x.IsNestedIn(y)) return -1;
        if (y.IsNestedIn(x)) return 1;
        return x.tacLoc.tb < y.tacLoc.tb ? -1 : 1;
    }
}

internal static class PartitionExt
{
    public static (List<T> whereTrue, List<T> whereFalse) PartitionBy<T>(this IEnumerable<T> collection,
        Func<T, bool> predicate)
    {
        var src = collection.ToList();
        return (src.Where(predicate).ToList(), src.Where(v => !predicate(v)).ToList());
    }

    public static List<(T, int)> Indexed<T>(this IEnumerable<T> collection) =>
        collection.Select((v, i) => (v, i)).ToList();
}

internal static class EhStmtExt
{
    public static EhScope? GetClosestToOrNull(this List<EhScope> scopes, int index)
    {
        if (!scopes.Any(s => s.ScopeContainsIndex(index))) return null;
        return scopes.Where(s => s.ScopeContainsIndex(index)).MinBy(s => index.DistToScope(s));
    }

    public static int DistToScope(this int index, EhScope scope)
    {
        return int.Min(index - scope.tacLoc.tb, index - scope.tacLoc.hb);
    }

    public static bool IsNestedIn(this EhScope scope, EhScope parentScope)
    {
        return parentScope.tacLoc.tb <= scope.tacLoc.tb && scope.tacLoc.te <= parentScope.tacLoc.te ||
               parentScope.tacLoc.hb <= scope.tacLoc.hb && scope.tacLoc.he <= parentScope.tacLoc.he;
    }

    public static bool IsNestedInHandler(this EhScope scope, EhScope parentScope)
    {
        return parentScope.tacLoc.hb <= scope.tacLoc.tb && scope.tacLoc.he <= parentScope.tacLoc.he;
    }

    public static bool ScopeContainsIndex(this EhScope scope, int index)
    {
        return scope.TryContainsIndex(index) || scope.HandlerContainsIndex(index);
    }

    public static bool TryContainsIndex(this EhScope scope, int index)
    {
        return scope.tacLoc.tb <= index && index <= scope.tacLoc.te;
    }

    public static bool HandlerContainsIndex(this EhScope scope, int index)
    {
        return scope.tacLoc.hb <= index && index <= scope.tacLoc.he;
    }
}