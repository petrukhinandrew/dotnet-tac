using TACBuilder.ILMeta.ILBodyParser;

namespace TACBuilder.ILMeta;

// TODO expected API is 
// getBasicBlocks(): BasicBlockList

class CFG(ILInstr entry, List<ehClause> ehClauses)
{
    private ILInstr _entry = entry;
    private List<ehClause> _ehClauses = ehClauses;

    // may have same behaviour as ILBodyParser
    public void MarkBasicBlocks()
    {
    }

    private List<BasicBlockLocation> _bbLocations = [];
    public List<BasicBlockLocation> BasicBlocksMarkup => _bbLocations;
}

public class BasicBlockLocation(ILInstr begin, ILInstr end)
{
}