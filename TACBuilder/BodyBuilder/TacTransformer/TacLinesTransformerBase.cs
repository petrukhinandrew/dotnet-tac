using System.Diagnostics;
using TACBuilder.ILReflection;
using TACBuilder.ILTAC.TypeSystem;

namespace TACBuilder.BodyBuilder.TacTransformer;

/*
 * Raw transformer over TAC list (either array-like or linked-list-like)
 * Consider following things:
 * 1. Any insertion or deletion may change target of IlBranchStmt
 * 2. Any insertion or deletion may change EhScope tacLoc
 * 3. Transformer is responsible for method TAC, it keeps method fields consistency itself
 */
public interface TacLinesTransformerBase<TPos>
{
    public void Insert(TPos pos, IlStmt stmt) => InsertRange(pos, [stmt]);
    public void InsertRange(TPos pos, IEnumerable<IlStmt> stmts);
    public  void Remove(TPos pos) => RemoveRange(pos, pos);
    public  void RemoveRange(TPos begin, TPos end);
}

internal class TacLinesTransformerIndexImpl(IlMethod method) : TacLinesTransformerBase<int>
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
            if (stmt is not IlBranchStmt branch) return stmt;

            var copy = branch.Copy();
            copy.Target = copy.Target + dst - slicePos;
            return copy;
        });
        var duplicates = _scopes.Where(s => s.IsInSegment(slicePos, slicePos + sliceLength))
            .Select(s => s.ShiftedRightAt(slicePos - s.tacLoc.tb + dst)).ToList();
        _scopes.AddRange(duplicates);
    }
}