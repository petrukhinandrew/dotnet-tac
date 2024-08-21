using Usvm.IL.Parser;
namespace Usvm.IL.TACBuilder;

abstract class EHScope
{
    public static EHScope FromClause(ehClause clause)
    {
        return clause.ehcType switch
        {
            rewriterEhcType.CatchEH => CatchScope.FromClause(clause),
            rewriterEhcType.FilterEH => FilterScope.FromClause(clause),
            rewriterEhcType.FinallyEH => FinallyScope.FromClause(clause),
            rewriterEhcType.FaultEH => FaultScope.FromClause(clause),
            _ => throw new Exception("unexpected clause type " + clause.ToString())
        };
    }
    public struct ScopeLocation
    {
        public int tb, te, hb, he;
        public static ScopeLocation FromClause(ehClause clause)
        {
            return new()
            {
                tb = clause.tryBegin.idx,
                te = clause.tryEnd.idx,
                hb = clause.handlerBegin.idx,
                he = clause.handlerEnd.idx,
            };
        }
        public List<int> Indices()
        {
            return [tb, te, hb, he];
        }
        public override string ToString()
        {
            return string.Join(" ", new int[] { tb, te, hb, he });
        }
        public override bool Equals(object? obj)
        {
            return obj != null && obj is ScopeLocation l && tb == l.tb && te == l.te && hb == l.hb && he == l.he;
        }

        public override int GetHashCode()
        {
            return (tb, te, hb, he).GetHashCode();
        }
    }
    public ScopeLocation ilLoc = new(), tacLoc = new();
}
abstract class EHScopeWithVarIdx : EHScope
{
    public int ErrIdx;
    public Type Type = typeof(Exception);
}
class CatchScope(Type type) : EHScopeWithVarIdx
{
    public new Type Type = type;
    public new int ErrIdx = 0;
    public static new CatchScope FromClause(ehClause clause)
    {
        return new CatchScope((clause.ehcType as rewriterEhcType.CatchEH)!.type)
        {
            ilLoc = ScopeLocation.FromClause(clause)
        };
    }
    public override string ToString()
    {
        return string.Format("catch {0}", tacLoc.ToString());
    }
    public override bool Equals(object? obj)
    {
        return obj != null && obj is CatchScope cs && ilLoc.Equals(cs.ilLoc);
    }
    public override int GetHashCode()
    {
        return ilLoc.GetHashCode();
    }

}

class FilterScope : EHScopeWithVarIdx
{
    public int fb = -1;
    public new int ErrIdx = 0;
    public static new FilterScope FromClause(ehClause clause)
    {
        FilterScope scope = new FilterScope
        {
            ilLoc = ScopeLocation.FromClause(clause),
            fb = (clause.ehcType as rewriterEhcType.FilterEH)!.instr.idx
        };
        return scope;
    }
    public override string ToString()
    {
        return string.Format("filter {5} {0} {1} {2} {3} {4}", tacLoc.tb, tacLoc.te, fb, tacLoc.hb, tacLoc.he, Logger.ErrVarName(ErrIdx));
    }
    public override bool Equals(object? obj)
    {
        return obj != null && obj is FilterScope fs && ilLoc.Equals(fs.ilLoc);
    }
    public override int GetHashCode()
    {
        return ilLoc.GetHashCode();
    }
}

class FaultScope : EHScope
{
    public static new FaultScope FromClause(ehClause clause)
    {
        return new FaultScope()
        {
            ilLoc = ScopeLocation.FromClause(clause)
        };
    }
    public override string ToString()
    {
        return string.Format("fault {0}", tacLoc.ToString());
    }
    public override bool Equals(object? obj)
    {
        return obj != null && obj is FaultScope fs && ilLoc.Equals(fs.ilLoc);
    }
    public override int GetHashCode()
    {
        return ilLoc.GetHashCode();
    }
}

class FinallyScope : EHScope
{
    public static new FinallyScope FromClause(ehClause clause)
    {
        return new FinallyScope()
        {
            ilLoc = ScopeLocation.FromClause(clause)
        };
    }
    public override string ToString()
    {
        return string.Format("finally {0}", tacLoc.ToString());
    }
    public override bool Equals(object? obj)
    {
        return obj != null && obj is FinallyScope fs && ilLoc.Equals(fs.ilLoc);
    }
    public override int GetHashCode()
    {
        return ilLoc.GetHashCode();
    }
}

