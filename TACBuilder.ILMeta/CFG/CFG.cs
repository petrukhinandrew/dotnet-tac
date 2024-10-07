using System.Diagnostics;
using System.Reflection.Emit;
using System.Runtime.InteropServices.ComTypes;
using TACBuilder.ILMeta.ILBodyParser;

namespace TACBuilder.ILMeta.CFG;

public class CFG
{
    private readonly ILInstr _entry;
    private readonly List<ehClause> _ehClauses;

    private HashSet<ILInstr> _leaders = new();
    private Dictionary<int, List<int>> _succsessors;
    public Dictionary<int, List<int>> Succsessors => _succsessors;
    private Dictionary<int, List<int>> _predecessors;
    private readonly HashSet<BasicBlockMeta> _blocks = [];
    public List<BasicBlockMeta> BasicBlocks => _blocks.ToList();
    private readonly Dictionary<int, Type> _errTypeMapping = new();

    public CFG(ILInstr entry, List<ehClause> ehClauses)
    {
        _entry = entry;
        _ehClauses = ehClauses;
        CollectLeaders();
        // TODO make iterator to walk over ILinstr as now _entry moves in collect leaders (at least i think so)
        Debug.Assert(_leaders.Any(l => l is not null));
        try
        {
            _succsessors = _leaders.ToDictionary(l => l.idx, _ => new List<int>());
            _predecessors = _leaders.ToDictionary(l => l.idx, _ => new List<int>());
        }
        catch
        {
            Console.WriteLine("lolkek");
        }

        MarkupBlocks();
        AttachMetaInfoToBlocks();
        if (!CheckAllBlockHaveSuccessors())
            Debug.Assert(CheckAllBlockHaveSuccessors(), "found block without a successor");
    }

    private bool CheckAllBlockHaveSuccessors()
    {
        bool AcceptableExitInstr(ILInstr instr)
        {
            return instr.next is ILInstr.Back || instr is ILInstr.Instr
            {
                opCode.FlowControl: FlowControl.Return or FlowControl.Throw
            };
        }

        return _blocks.All(bb => bb.Successors.Count > 0 || AcceptableExitInstr(bb.Exit));
    }

    private void CollectLeaders()
    {
        _leaders.Add(_entry);
        ILInstr cur = _entry;
        while (cur is not ILInstr.Back)
        {
            Debug.Assert(cur is not null);
            if (cur.IsJump())
            {
                Debug.Assert(((ILInstrOperand.Target)cur.arg).value is not null);
                _leaders.Add(((ILInstrOperand.Target)cur.arg).value);
            }

            if (cur.IsCondJump || cur is ILInstr.SwitchArg)
            {
                Debug.Assert(cur.next is not null);
                _leaders.Add(cur.next);
            }

            cur = cur.next;
        }

        foreach (var clause in _ehClauses)
        {
            Debug.Assert(clause.handlerBegin is not null);
            // TODO check try is to be leader
            _leaders.Add(clause.handlerBegin);
            if (clause.ehcType is rewriterEhcType.CatchEH catchEh)
            {
                _errTypeMapping[clause.handlerBegin.idx] = catchEh.type;
            }

            if (clause.ehcType is rewriterEhcType.FilterEH filterEh)
            {
                Debug.Assert(filterEh.instr is not null);
                _errTypeMapping[clause.handlerBegin.idx] = typeof(Exception);
                _errTypeMapping[filterEh.instr.idx] = typeof(Exception);
                _leaders.Add(filterEh.instr);
            }
        }

        Debug.Assert(_leaders.All(l => l is not null));
    }

    private void MarkupBlocks()
    {
        foreach (var leader in _leaders)
        {
            ILInstr cur = leader;
            while (cur is ILInstr.Instr
                   {
                       opCode.FlowControl: not FlowControl.Cond_Branch and not FlowControl.Branch
                       and not FlowControl.Return and not FlowControl.Throw
                   } && !_leaders.Contains(cur.next))
            {
                cur = cur.next;
            }

            _blocks.Add(new BasicBlockMeta(leader, cur));
            if (cur.IsJump())
            {
                var targetIdx = ((ILInstrOperand.Target)cur.arg).value.idx;
                _succsessors[leader.idx].Add(targetIdx);
                _predecessors[targetIdx].Add(leader.idx);
                if (cur.IsCondJump || cur is ILInstr.SwitchArg)
                {
                    _succsessors[leader.idx].Add(cur.idx + 1);
                    _predecessors[cur.idx + 1].Add(leader.idx);
                }

                continue;
            }

            if (_leaders.Any(instr => instr.idx == cur.idx + 1))
            {
                _succsessors[leader.idx].Add(cur.idx + 1);
                _predecessors[cur.idx + 1].Add(leader.idx);
            }
        }

        foreach (var succ in _succsessors.Values)
        {
            succ.Sort();
        }
    }

    private void AttachMetaInfoToBlocks()
    {
        foreach (var block in _blocks)
        {
            block.Successors = _succsessors[block.Entry.idx];
            block.Predecessors = _predecessors[block.Entry.idx];

            block.StackErrType = _errTypeMapping.GetValueOrDefault(block.Entry.idx, null);
        }
    }

    public List<int> StartBlocksIndices => new List<int> { _entry.idx }
        .Concat(_ehClauses.Select(c => c.handlerBegin.idx)).Concat(_ehClauses
            .Where(c => c.ehcType is rewriterEhcType.FilterEH).Select(f =>
                ((rewriterEhcType.FilterEH)f.ehcType).instr.idx)).ToList();
}
