using TACBuilder.BodyBuilder.ILBodyParser;

namespace TACBuilder.BodyBuilder;

public abstract class EhScope
{
    public static EhScope FromClause(ehClause clause)
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

    public struct ScopeLocation : IEquatable<ScopeLocation>
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

        public void ShiftRight(int index, int delta)
        {
            if (tb >= index) tb += delta;
            if (te >= index) te += delta;
            if (hb >= index) hb += delta;
            if (he >= index) he += delta;
        }

        public void ShiftLeft(int index, int delta)
        {
            if (tb >= index) tb -= delta;
            if (te >= index) te -= delta;
            if (hb >= index) hb -= delta;
            if (he >= index) he -= delta;
        }

        public override string ToString() => $"loc: {tb} {te} {hb} {he}";

        public override int GetHashCode()
        {
            return (tb, te, hb, he).GetHashCode();
        }

        public bool Equals(ScopeLocation other)
        {
            return tb.Equals(other.tb) && te.Equals(other.te) && hb.Equals(other.hb) && he.Equals(other.he);
        }
    }

    public ScopeLocation ilLoc, tacLoc = new();
    public abstract EhScope ShiftedRightAt(int delta);
}

// TODO check if still needed
public abstract class EhScopeWithVarIdx(Type type) : EhScope
{
    public int ErrIdx;

    public readonly Type Type = type;
}

class CatchScope(Type type) : EhScopeWithVarIdx(type)
{
    public new static CatchScope FromClause(ehClause clause)
    {
        return new CatchScope((clause.ehcType as rewriterEhcType.CatchEH)!.type)
        {
            ilLoc = ScopeLocation.FromClause(clause)
        };
    }

    public override CatchScope ShiftedRightAt(int delta)
    {
        return new CatchScope(type)
        {
            tacLoc = { tb = tacLoc.tb + delta, te = tacLoc.te + delta, hb = tacLoc.hb + delta, he = tacLoc.he + delta }
        };
    }

    public override string ToString()
    {
        return $"catch: {tacLoc.ToString()}";
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

public class FilterScope() : EhScopeWithVarIdx(typeof(Exception))
{
    public int fb;
    public int fbt;

    public new static FilterScope FromClause(ehClause clause)
    {
        FilterScope scope = new FilterScope
        {
            ilLoc = ScopeLocation.FromClause(clause),
            fb = (clause.ehcType as rewriterEhcType.FilterEH)!.instr.idx
        };
        return scope;
    }

    public override FilterScope ShiftedRightAt(int delta)
    {
        return new FilterScope
        {
            fbt = fbt + delta,
            tacLoc = { tb = tacLoc.tb + delta, te = tacLoc.te + delta, hb = tacLoc.hb + delta, he = tacLoc.he + delta }
        };
    }

    public override string ToString()
    {
        return $"filter: {tacLoc.ToString()} {fbt}";
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

class FaultScope : EhScope
{
    public new static FaultScope FromClause(ehClause clause)
    {
        return new FaultScope()
        {
            ilLoc = ScopeLocation.FromClause(clause)
        };
    }

    public override FaultScope ShiftedRightAt(int delta)
    {
        return new FaultScope
        {
            tacLoc = { tb = tacLoc.tb + delta, te = tacLoc.te + delta, hb = tacLoc.hb + delta, he = tacLoc.he + delta }
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

class FinallyScope : EhScope
{
    public new static FinallyScope FromClause(ehClause clause)
    {
        return new FinallyScope
        {
            ilLoc = ScopeLocation.FromClause(clause)
        };
    }

    public override FinallyScope ShiftedRightAt(int delta)
    {
        return new FinallyScope
        {
            tacLoc = { tb = tacLoc.tb + delta, te = tacLoc.te + delta, hb = tacLoc.hb + delta, he = tacLoc.he + delta }
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