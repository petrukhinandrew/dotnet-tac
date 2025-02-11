using System.Diagnostics;
using TACBuilder.ILReflection;
using TACBuilder.ILTAC.TypeSystem;

namespace TACBuilder.BodyBuilder;

/*
 * Raw transformer over TAC list (either array-like or linked-list-like)
 * Consider following things:
 * 1. Any insertion or deletion may change target of IlBranchStmt
 * 2. Any insertion or deletion may change EhScope tacLoc
 * 3. Transformer is responsible for method TAC, it keeps method fields consistency itself
 */
public interface TacTransformerBase<TPos>
{
    public void Insert(TPos pos, IlStmt stmt) => InsertRange(pos, [stmt]);
    public void InsertRange(TPos pos, IEnumerable<IlStmt> stmts);

    public void Remove(TPos pos) => RemoveRange(pos, pos);
    public void RemoveRange(TPos begin, TPos end);
}

public interface TacTransformStrategyBase
{
    public void Transform();
}

public class TacTransformerIndexImpl(IlMethod method) : TacTransformerBase<int>
{
    protected List<IlStmt>? _lines => method.Body?.Lines;
    protected List<EhScope> _scopes => method.Scopes;

    public void InsertRange(int pos, IEnumerable<IlStmt> stmts)
    {
        if (_lines == null) return;
        var stmtsList = stmts.ToList();
        var shiftSize = stmtsList.Count;

        foreach (var line in _lines)
        {
            if (line is IlBranchStmt branch && branch.Target >= pos)
            {
                branch.Target += shiftSize;
            }
        }

        _lines.InsertRange(pos, stmtsList);

        foreach (var scope in _scopes)
        {
            scope.tacLoc.ShiftRight(pos, shiftSize);
        }
    }

    /*
     * Remove statements from tac begin index to end index inclusively
     */
    public void RemoveRange(int begin, int end)
    {
        if (_lines == null) return;
        var shiftSize = end - begin + 1;
        _lines.RemoveRange(begin, shiftSize);
        foreach (var scope in _scopes)
        {
            scope.tacLoc.ShiftLeft(begin, shiftSize);
        }
    }

    public void ApplyToSlice(int pos, int length, Func<IlStmt, IlStmt> action)
    {
        if (_lines == null) return;
        for (int i = pos; i < pos + length; i++)
        {
            _lines[i] = action(_lines[i]);
        }
    }

    public List<IlStmt> SliceCopy(int pos, int length)
    {
        if (_lines == null) return [];
        IlStmt[] copy = new IlStmt[length];
        _lines.CopyTo(pos, copy, 0, length);
        for (var i = 0; i < length; i++)
            if (copy[i] is IlBranchStmt branch)
                copy[i] = branch.Copy();
        return copy.ToList();
    }

    public void DuplicateSlice(int slicePos, int sliceLength, int dst)
    {
        if (_lines == null) return;
        Debug.Assert(dst > slicePos);
        var slice = SliceCopy(slicePos, sliceLength);
        InsertRange(dst, slice);
        ApplyToSlice(dst, sliceLength, stmt =>
        {
            if (stmt is IlBranchStmt branch)
            {
                var copy = branch.Copy();
                copy.Target = copy.Target + dst - slicePos;
                return copy;
            }
            else return stmt;
        });
        var duplicates = _scopes.Where(s => s.IsInSegment(slicePos, slicePos + sliceLength))
            .Select(s => s.ShiftedRightAt(slicePos - s.tacLoc.tb + dst)).ToList();
        _scopes.AddRange(duplicates);
    }
}

public class TacFinallyInliner(IlMethod method) : TacTransformerIndexImpl(method), TacTransformStrategyBase
{
    public void Transform()
    {
        if (_lines == null) return;
        var init = new IlStmt[_lines.Count];
        _lines.CopyTo(init, 0);
        var finallyScopes = GetFinallyScopesSortedByNesting();

        if (finallyScopes.Count == 0) return;
        foreach (var scope in finallyScopes)
        {
            var handlerSize = scope.tacLoc.he - scope.tacLoc.hb + 1;
            var inlinePos = scope.tacLoc.he + 1;
            for (var i = scope.tacLoc.tb; i <= scope.tacLoc.te; i++)
            {
                if (_lines[i] is not IlLeaveStmt leaveStmt || leaveStmt.Target <= scope.tacLoc.te) continue;
                DuplicateSlice(scope.tacLoc.hb, handlerSize, inlinePos);
                var leaveTarget = leaveStmt.Target;

                ApplyToSlice(inlinePos, handlerSize,
                    stmt => stmt is IlEndFinallyStmt { IsMutable: true }
                        ? new IlGotoStmt(leaveTarget)
                        : stmt);
                ApplyToSlice(scope.tacLoc.tb, scope.tacLoc.te - scope.tacLoc.tb + 1,
                    stmt => stmt is IlLeaveStmt ls && ls.Target == leaveTarget
                        ? new IlGotoStmt(inlinePos)
                        : stmt);
                inlinePos += handlerSize;
            }

            FixInitialFinallyScope(scope.tacLoc.hb, scope.tacLoc.he);
        }
    }

    /*
     * Args are segment ends inclusively
     */
    private void FixInitialFinallyScope(int begin, int end)
    {
        for (var i = begin; i <= end; i++)
        {
            if (_lines![i] is IlEndFinallyStmt) _lines[i] = new IlEndFinallyStmt { IsMutable = false };
        }
    }

    private List<FinallyScope> GetFinallyScopesSortedByNesting() => _scopes.Where(it => it is FinallyScope)
        .Order(new EhScopeNestingComparer()).Select(s => (FinallyScope)s).ToList();
}