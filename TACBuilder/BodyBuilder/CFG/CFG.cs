﻿using System.Diagnostics;
using System.Reflection.Emit;
using TACBuilder.BodyBuilder.ILBodyParser;
using TACBuilder.ILReflection;

namespace TACBuilder.BodyBuilder;

public class CFG
{
    private readonly IlInstr _entry;
    private readonly List<ehClause> _ehClauses;
    private HashSet<IlInstr> _leaders = new();
    private Dictionary<int, List<int>> _succsessors;
    private Dictionary<int, List<int>> _predecessors;
    private readonly HashSet<IlBasicBlock> _blocks = [];
    private readonly Dictionary<int, Type> _errTypeMapping = new();

    public List<IlBasicBlock> BasicBlocks => _blocks.ToList();
    public Dictionary<int, List<int>> Succsessors => _succsessors;

    public CFG(IlInstr entry, List<ehClause> ehClauses)
    {
        _entry = entry;
        _ehClauses = ehClauses;
        CollectLeaders();
        Debug.Assert(_leaders.Any(l => l is not null));

        _succsessors = _leaders.ToDictionary(l => l.idx, _ => new List<int>());
        _predecessors = _leaders.ToDictionary(l => l.idx, _ => new List<int>());

        MarkupBlocks();
        AttachMetaInfoToBlocks();
        if (!CheckAllBlockHaveSuccessors())
            Debug.Assert(false, "found block without a successor");
        if (!CheckEhClausesToBlocksMapping(out var pos))
        {
            Debug.Assert(false, "found eh clause bad mapping of type " + pos);
        }
    }

    private bool CheckAllBlockHaveSuccessors()
    {
        bool AcceptableExitInstr(IlInstr instr)
        {
            return instr.next is IlInstr.Back || instr is IlInstr.Instr
            {
                opCode.FlowControl: FlowControl.Return or FlowControl.Throw
            };
        }

        return _blocks.All(bb => bb.Successors.Count > 0 || AcceptableExitInstr(bb.Exit));
    }

    private bool CheckEhClausesToBlocksMapping(out int clausePos)
    {
        clausePos = -1;
        foreach (var clause in _ehClauses)
        {
            if (_blocks.All(b => b.Entry.idx != clause.tryBegin.idx)) clausePos = 0;
            if (_blocks.All(b => b.Exit.idx != clause.tryEnd.idx)) clausePos = 1;
            if (_blocks.All(b => b.Entry.idx != clause.handlerBegin.idx)) clausePos = 2;
            if (_blocks.All(b => b.Exit.idx != clause.handlerEnd.idx)) clausePos = 3;
            if (clausePos != -1) return false;
        }

        return true;
    }

    private void CollectLeaders()
    {
        _leaders.Add(_entry);
        IlInstr cur = _entry;
        while (cur is not IlInstr.Back)
        {
            Debug.Assert(cur is not null);
            if (cur.IsJump)
            {
                Debug.Assert(((ILInstrOperand.Target)cur.arg).value is not null);
                _leaders.Add(((ILInstrOperand.Target)cur.arg).value);
            }

            if (cur.IsCondJump || cur is IlInstr.SwitchArg)
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
            _leaders.Add(clause.tryBegin);
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
            IlInstr cur = leader;
            while (cur is IlInstr.Instr
                   {
                       opCode.FlowControl: FlowControl.Next or FlowControl.Call or FlowControl.Meta
                   } && !_leaders.Contains(cur.next))
            {
                cur = cur.next;
            }

            _blocks.Add(new IlBasicBlock(leader, cur));
            if (cur.IsJump)
            {
                var targetIdx = ((ILInstrOperand.Target)cur.arg).value.idx;
                _succsessors[leader.idx].Add(targetIdx);
                _predecessors[targetIdx].Add(leader.idx);
                if (cur.IsCondJump || cur is IlInstr.SwitchArg)
                {
                    _succsessors[leader.idx].Add(cur.idx + 1);
                    _predecessors[cur.idx + 1].Add(leader.idx);
                }
            }
            else if (cur is not IlInstr.Instr { opCode.FlowControl: FlowControl.Throw or FlowControl.Return })
            {
                _succsessors[leader.idx].Add(cur.idx + 1);
                _predecessors[cur.idx + 1].Add(leader.idx);
            }

            // if (_leaders.Any(instr => instr.idx == cur.idx + 1))
            // {
            //     _succsessors[leader.idx].Add(cur.idx + 1);
            //     _predecessors[cur.idx + 1].Add(leader.idx);
            // }
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