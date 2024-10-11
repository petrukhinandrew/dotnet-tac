using TACBuilder.Utils;

namespace TACBuilder.BodyBuilder;

public abstract class EHScope
{
    public static EHScope FromClause(ehClause clause)
    {
        return clause.ehcType switch
        {
            rewriterEhcType.CatchEH => CatchScope.FromClause(clause),
            rewriterEhcType.FilterEH => FilterScope.FromClause(clause),
            rewriterEhcType.FinallyEH => FinallyScope.FromClause(clause),
            rewriterEhcType.FaultEH => FaultScope.FromClause(clause),
            _ => throw new Exception("unexpected clause type " + clause)
        };
    }

    public struct ILScopeLocation
    {
        public ILInstr tb, te, hb, he;

        public static ILScopeLocation FromClause(ehClause clause)
        {
            return new()
            {
                tb = clause.tryBegin,
                te = clause.tryEnd,
                hb = clause.handlerBegin,
                he = clause.handlerEnd,
            };
        }

        public override string ToString() => string.Join(" ", new int[tb.idx, te.idx, hb.idx, he.idx]);

        public override bool Equals(object? obj)
        {
            return obj is ILScopeLocation l && tb.idx == l.tb.idx && te.idx == l.te.idx &&
                   hb.idx == l.hb.idx && he.idx == l.he.idx;
        }

        public override int GetHashCode()
        {
            return (tb, te, hb, he).GetHashCode();
        }
    }

    public ILScopeLocation ilLoc, tacLoc = new();
}

public abstract class EHScopeWithVarIdx(Type type) : EHScope
{
    public int ErrIdx;

    public readonly Type Type = type;
    // public BlockTacBuilder.BlockTacBuilder HandlerFrame;
}

class CatchScope(Type type) : EHScopeWithVarIdx(type)
{
    public new static CatchScope FromClause(ehClause clause)
    {
        return new CatchScope((clause.ehcType as rewriterEhcType.CatchEH)!.type)
        {
            ilLoc = ILScopeLocation.FromClause(clause)
        };
    }

    public override string ToString()
    {
        return $"catch {tacLoc.ToString()}";
    }

    public override bool Equals(object? obj)
    {
        return obj is CatchScope cs && ilLoc.Equals(cs.ilLoc);
    }

    public override int GetHashCode()
    {
        return ilLoc.GetHashCode();
    }
}

public class FilterScope() : EHScopeWithVarIdx(typeof(Exception))
{
    public ILInstr fb;
    // public BlockTacBuilder.BlockTacBuilder FilterFrame;

    public new static FilterScope FromClause(ehClause clause)
    {
        FilterScope scope = new FilterScope
        {
            ilLoc = ILScopeLocation.FromClause(clause),
            fb = (clause.ehcType as rewriterEhcType.FilterEH)!.instr
        };
        return scope;
    }

    public override string ToString()
    {
        return string.Format("filter {5} {0} {1} {2} {3} {4}", tacLoc.tb, tacLoc.te, fb, tacLoc.hb, tacLoc.he,
            NamingUtil.ErrVar(ErrIdx));
    }

    public override bool Equals(object? obj)
    {
        return obj is FilterScope fs && ilLoc.Equals(fs.ilLoc);
    }

    public override int GetHashCode()
    {
        return ilLoc.GetHashCode();
    }
}

class FaultScope : EHScope
{
    public new static FaultScope FromClause(ehClause clause)
    {
        return new FaultScope()
        {
            ilLoc = ILScopeLocation.FromClause(clause)
        };
    }

    public override string ToString()
    {
        return $"fault {tacLoc.ToString()}";
    }

    public override bool Equals(object? obj)
    {
        return obj is FaultScope fs && ilLoc.Equals(fs.ilLoc);
    }

    public override int GetHashCode()
    {
        return ilLoc.GetHashCode();
    }
}

class FinallyScope : EHScope
{
    public new static FinallyScope FromClause(ehClause clause)
    {
        return new FinallyScope
        {
            ilLoc = ILScopeLocation.FromClause(clause)
        };
    }

    public override string ToString()
    {
        return $"finally {tacLoc.ToString()}";
    }

    public override bool Equals(object? obj)
    {
        return obj is FinallyScope fs && ilLoc.Equals(fs.ilLoc);
    }

    public override int GetHashCode()
    {
        return ilLoc.GetHashCode();
    }
}
