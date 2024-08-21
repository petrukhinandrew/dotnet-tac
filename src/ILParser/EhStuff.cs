using System.Reflection;
using System.Reflection.Emit;
using Microsoft.VisualBasic;

namespace Usvm.IL.Parser;
public abstract record ehcType
{
    public record Filter(int offset) : ehcType;
    public record Catch(Type type) : ehcType;
    public record Finally : ehcType;
    public record Fault : ehcType;
}

public class exceptionHandlingClause
{
    public int tryOffset;
    public int tryLength;
    public int handlerOffset;
    public int handlerLength;
    public ehcType type;

    public exceptionHandlingClause(ExceptionHandlingClause c)
    {
        ExceptionHandlingClauseOptions flags = c.Flags;
        ehcType ehctype;
        switch (flags)
        {
            case ExceptionHandlingClauseOptions.Filter: ehctype = new ehcType.Filter(c.FilterOffset); break;
            case ExceptionHandlingClauseOptions.Finally: ehctype = new ehcType.Finally(); break;
            case ExceptionHandlingClauseOptions.Fault: ehctype = new ehcType.Fault(); break;
            default: ehctype = new ehcType.Catch(c.CatchType ?? throw new Exception("unexpected null type")); break;
        }
        tryOffset = c.TryOffset;
        tryLength = c.TryLength;
        handlerOffset = c.HandlerOffset;
        handlerLength = c.HandlerLength;
        type = ehctype;
    }
}

public abstract record rewriterEhcType
{
    public record FilterEH(ILInstr instr) : rewriterEhcType;
    public record CatchEH(Type type) : rewriterEhcType;
    public record FinallyEH() : rewriterEhcType;
    public record FaultEH() : rewriterEhcType;
}

class ehClause
{
    public ehClause(ILInstr tryB, ILInstr tryE, ILInstr handlerB, ILInstr handlerE, rewriterEhcType type)
    {
        tryBegin = tryB;
        tryEnd = tryE;
        handlerBegin = handlerB;
        handlerEnd = handlerE;
        ehcType = type;
    }
    public ILInstr tryBegin;
    public ILInstr tryEnd;
    public ILInstr handlerBegin;
    public ILInstr handlerEnd;
    public rewriterEhcType ehcType;
    public override string ToString()
    {
        string extra = ehcType switch {
            rewriterEhcType.FilterEH f => f.instr.idx.ToString(),
            _ => ""
        };
        return string.Format("{0} {1} {2} {3} {4} {5}", ehcType.ToString(), tryBegin.idx.ToString(), tryEnd.idx.ToString(), handlerBegin.idx.ToString(), handlerEnd.idx.ToString(), extra);
    }
}