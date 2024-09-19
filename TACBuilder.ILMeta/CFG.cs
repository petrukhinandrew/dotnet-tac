using TACBuilder.ILMeta.ILBodyParser;

namespace TACBuilder.ILMeta;

class CFG(ILInstr entry, List<ehClause> ehClauses)
{
    private ILInstr _entry = entry;
    private List<ehClause> _ehClauses = ehClauses;

    // may have same behaviour as ILBodyParser
    public void MarkBasicBlocks()
    {
        
    }
    private List<BasicBlockMeta.Location> _bbLocations = [];
    public List<BasicBlockMeta.Location> BasicBlocksMarkup => _bbLocations;
}