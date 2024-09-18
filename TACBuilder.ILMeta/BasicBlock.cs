using Usvm.IL.Parser;

namespace TACBuilder.ILMeta;

public class BasicBlock
{
    // built by CFG
    // contains smth like pair of il instrs with guarantees like in JcBasicBlock
    private IEnumerable<ILInstr> Instructions { get; }
}