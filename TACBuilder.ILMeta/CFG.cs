using TACBuilder.ILMeta.ILBodyParser;

namespace TACBuilder.ILMeta;

public class CFG
{
    private readonly ILInstr _entry;
    private readonly List<ehClause> _ehClauses;

    private HashSet<ILInstr> _leaders = new();
    private Dictionary<int, List<int>> _succsessors;
    private Dictionary<int, List<int>> _predecessors;
    private readonly HashSet<BasicBlockMeta> _blocks = [];
    public List<BasicBlockMeta> BasicBlocks => _blocks.ToList();

    public CFG(ILInstr entry, List<ehClause> ehClauses)
    {
        _entry = entry;
        _ehClauses = ehClauses;
        CollectLeaders();
        _succsessors = _leaders.ToDictionary(l => l.idx, _ => new List<int>());
        _predecessors = _leaders.ToDictionary(l => l.idx, _ => new List<int>());
        MarkupBlocks();
        AttachSuccsAndPreds();
    }

    private void CollectLeaders()
    {
        ILInstr cur = _entry;
        _leaders = [cur];
        while (cur is not ILInstr.Back)
        {
            if (cur.IsJump())
            {
                _leaders.Add(((ILInstrOperand.Target)cur.arg).value);
                _leaders.Add(cur.next);
            }

            cur = cur.next;
        }

        foreach (var clause in _ehClauses)
        {
            _leaders.Add(clause.handlerBegin);
            if (clause.ehcType is rewriterEhcType.FilterEH filterEh)
                _leaders.Add(filterEh.instr);
        }
    }

    private void MarkupBlocks()
    {
        foreach (var leader in _leaders)
        {
            ILInstr cur = leader;
            while (!IsBlockExit(cur))
            {
                cur = cur.next;
            }

            _blocks.Add(new BasicBlockMeta(leader, cur));
            if (cur.IsJump())
            {
                var targetIdx = ((ILInstrOperand.Target)cur.arg).value.idx;
                _succsessors[leader.idx].Add(targetIdx);
                _predecessors[targetIdx].Add(leader.idx);
            }

            if (!cur.IsUncondJump() && !cur.IsControlFlowInterruptor())
            {
                _succsessors[leader.idx].Add(cur.idx + 1);
                _predecessors[cur.idx + 1].Add(leader.idx);
            }
        }
    }

    private void AttachSuccsAndPreds()
    {
        foreach (var block in _blocks)
        {
            block.Successors = _succsessors[block.Entry.idx];
            block.Predecessors = _predecessors[block.Entry.idx];
        }
    }

    private bool IsBlockExit(ILInstr instr) => instr.IsControlFlowInterruptor() || instr.IsJump();
}