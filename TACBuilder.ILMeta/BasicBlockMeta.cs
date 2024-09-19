using TACBuilder.ILMeta.ILBodyParser;

namespace TACBuilder.ILMeta;

public class BasicBlockMeta(BasicBlockMeta.Location location)
{
    // built by CFG
    // contains smth like pair of il instrs with guarantees like in JcBasicBlock
    private IEnumerable<ILInstr> Instructions { get; }

    public class Location
    {
        
    }
}