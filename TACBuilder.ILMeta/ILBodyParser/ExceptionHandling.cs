using System.Reflection;

namespace TACBuilder.ILMeta.ILBodyParser;

public abstract record ehcType
{
    public record Filter(int offset) : ehcType;

    public record Catch(Type type) : ehcType;

    public record Finally : ehcType;

    public record Fault : ehcType;
}

public abstract record rewriterEhcType
{
    public record FilterEH(ILInstr instr) : rewriterEhcType;

    public record CatchEH(Type type) : rewriterEhcType;

    public record FinallyEH() : rewriterEhcType;

    public record FaultEH() : rewriterEhcType;
}

public class ehClause(ILInstr tryB, ILInstr tryE, ILInstr handlerB, ILInstr handlerE, rewriterEhcType type)
{
    public ILInstr tryBegin = tryB;
    public ILInstr tryEnd = tryE;
    public ILInstr handlerBegin = handlerB;
    public ILInstr handlerEnd = handlerE;
    public rewriterEhcType ehcType = type;

    public override string ToString()
    {
        string extra = ehcType switch
        {
            rewriterEhcType.FilterEH f => f.instr.idx.ToString(),
            _ => ""
        };
        return
            $"{ehcType} {tryBegin.idx.ToString()} {tryEnd.idx.ToString()} {handlerBegin.idx.ToString()} {handlerEnd.idx.ToString()} {extra}";
    }
}

public class exceptionHandlingClause
{
    public readonly int tryOffset;
    public readonly int tryLength;
    public readonly int handlerOffset;
    public readonly int handlerLength;
    public readonly ehcType type;

    public exceptionHandlingClause(ExceptionHandlingClause c)
    {
        ExceptionHandlingClauseOptions flags = c.Flags;
        ehcType ehctype;
        switch (flags)
        {
            case ExceptionHandlingClauseOptions.Filter:
                ehctype = new ehcType.Filter(c.FilterOffset);
                break;
            case ExceptionHandlingClauseOptions.Finally:
                ehctype = new ehcType.Finally();
                break;
            case ExceptionHandlingClauseOptions.Fault:
                ehctype = new ehcType.Fault();
                break;
            default:
                ehctype = new ehcType.Catch(c.CatchType ?? throw new Exception("unexpected null type"));
                break;
        }

        tryOffset = c.TryOffset;
        tryLength = c.TryLength;
        handlerOffset = c.HandlerOffset;
        handlerLength = c.HandlerLength;
        type = ehctype;
    }
}