﻿using System.Diagnostics;
using System.Reflection.Emit;
using TACBuilder.ILMeta.ILBodyParser;

namespace TACBuilder.ILMeta;

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

            if (cur.IsUncondJump())
            {
                Debug.Assert(cur.next is not null);
                _leaders.Add(cur.next);
            }

            cur = cur.next;
        }

        foreach (var clause in _ehClauses)
        {
            Debug.Assert(clause.handlerBegin is not null);
            _leaders.Add(clause.handlerBegin);
            if (clause.ehcType is rewriterEhcType.CatchEH catchEh)
            {
                _errTypeMapping[clause.handlerBegin.idx] = catchEh.type;
            }

            if (clause.ehcType is rewriterEhcType.FilterEH filterEh)
            {
                Debug.Assert(filterEh.instr is not null);
                _leaders.Add(filterEh.instr);
            }
        }

        Debug.Assert(_leaders.Any(l => l is not null));
    }

    private void MarkupBlocks()
    {
        foreach (var leader in _leaders)
        {
            ILInstr cur = leader;
            while (!cur.IsControlFlowInterruptor())
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

    private void AttachMetaInfoToBlocks()
    {
        foreach (var block in _blocks)
        {
            block.Successors = _succsessors[block.Entry.idx];
            block.Predecessors = _predecessors[block.Entry.idx];

            block.StackErrType = _errTypeMapping.GetValueOrDefault(block.Entry.idx, null);
        }
    }

    public List<int> StartBlocksIndices => ((List<int>) [_entry.idx]).ToList()
        .Concat(_ehClauses.Select(c => c.handlerBegin.idx)).Concat(_ehClauses
            .Where(c => c.ehcType is rewriterEhcType.FilterEH).Select(f =>
                ((rewriterEhcType.FilterEH)f.ehcType).instr.idx)).ToList();
}
