using System.Diagnostics;
using TACBuilder.Exprs;
using TACBuilder.ILReflection;

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

public class TacLinesTransformerIndexImpl(IlMethod method) : TacLinesTransformerBase<int>
{
    public List<IlStmt>? Lines => method.Body?.Lines;
    public List<EhScope> Scopes => method.Scopes;

    public void InsertRange(int pos, IEnumerable<IlStmt> stmts)
    {
        if (Lines == null) return;
        var stmtsList = stmts.ToList();
        var shiftSize = stmtsList.Count;

        foreach (var line in Lines)
        {
            if (line is IlBranchStmt branch && branch.Target >= pos)
            {
                branch.Target += shiftSize;
            }
        }

        Lines.InsertRange(pos, stmtsList);

        foreach (var scope in Scopes)
        {
            scope.tacLoc.ShiftRight(pos, shiftSize);
        }
    }

    /*
     * Remove statements from tac begin index to end index inclusively
     */
    public void RemoveRange(int begin, int end)
    {
        if (Lines == null) return;
        var shiftSize = end - begin + 1;
        Lines.RemoveRange(begin, shiftSize);
        foreach (var scope in Scopes)
        {
            scope.tacLoc.ShiftLeft(begin, shiftSize);
        }
    }

    public void ApplyToSlice(int pos, int length, Func<IlStmt, IlStmt> action)
    {
        if (Lines == null) return;
        for (int i = pos; i < pos + length; i++)
        {
            Lines[i] = action(Lines[i]);
        }
    }

    public List<IlStmt> SliceCopy(int pos, int length)
    {
        if (Lines == null) return [];
        IlStmt[] copy = new IlStmt[length];
        Lines.CopyTo(pos, copy, 0, length);
        for (var i = 0; i < length; i++)
            if (copy[i] is IlBranchStmt branch)
                copy[i] = branch.Copy();
        return copy.ToList();
    }

    public void DuplicateSlice(int slicePos, int sliceLength, int dst)
    {
        if (Lines == null) return;
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
        var duplicates = Scopes.Where(s => s.IsInSegment(slicePos, slicePos + sliceLength))
            .Select(s => s.ShiftedRightAt(slicePos - s.tacLoc.tb + dst)).ToList();
        Scopes.AddRange(duplicates);
    }
}