using System.Diagnostics;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Pdb;
using Mono.Cecil.Rocks;
using TACBuilder.ILReflection;
using MethodBody = System.Reflection.MethodBody;
using OpCode = System.Reflection.Emit.OpCode;
using OperandType = System.Reflection.Emit.OperandType;

namespace TACBuilder.BodyBuilder.ILBodyParser;

public class IlMonoInst(Instruction inst)
{
    public Instruction Inst = inst;
    public SequencePoint? SequencePoint;
}

public class IlBodyParser(MethodBase methodBase)
{
    private readonly MethodBody? _methodBody = methodBase.GetMethodBody();
    
    private byte[] _il = [];
    private List<IlMonoInst> _ilMono = [];
    private IlInstr[] _offsetToInstr = [];
    private IlInstr _back = new IlInstr.Back();
    private ehClause[] _ehs = [];

    public void Parse()
    {
        ImportIL();
        ImportEH();
    }

    public IlInstr Instructions => _back.next;
    public List<IlMonoInst> IlMonoInstructions => _ilMono;
    public string? FilePath = null;
    public List<ehClause> EhClauses => _ehs.ToList();

    private void ImportEH()
    {
        if (_methodBody == null) return;
        
        var clauses = _methodBody.ExceptionHandlingClauses
            .Select(ehc => new exceptionHandlingClause(ehc)).ToArray();
        _ehs = clauses.Select(ParseEh).ToArray();
        return;

        /*
         * Here _offsetToInstr[endIdx].prev works because of IlInstr.Back at the end of list
         * Otherwise any handler with no instruction after it may fail
         */
        ehClause ParseEh(exceptionHandlingClause c)
        {
            IlInstr tryBegin = _offsetToInstr[c.tryOffset];
            Debug.Assert(tryBegin is not null);

            int te = c.tryOffset + c.tryLength;
            Debug.Assert(_offsetToInstr[te].prev is not null);
            IlInstr tryEnd = _offsetToInstr[te].prev;

            IlInstr handlerBegin = _offsetToInstr[c.handlerOffset];
            Debug.Assert(handlerBegin is not null);

            int he = c.handlerOffset + c.handlerLength;
            Debug.Assert(_offsetToInstr[he].prev is not null);
            IlInstr handlerEnd = _offsetToInstr[he].prev;
            Debug.Assert(handlerBegin.idx <= handlerEnd.idx);
            int fd = 0;
            if (c.type is ehcType.Filter filt)
            {
                fd = filt.offset;
                Debug.Assert(_offsetToInstr[fd] is not null);
            }

            if (c.type is ehcType.Catch excType)
            {
                IlInstanceBuilder.GetType(excType.type);
            }
            rewriterEhcType type = c.type switch
            {
                ehcType.Filter _ => new rewriterEhcType.FilterEH(_offsetToInstr[fd]),
                ehcType.Catch ct => new rewriterEhcType.CatchEH(ct.type),
                ehcType.Finally => new rewriterEhcType.FinallyEH(),
                ehcType.Fault => new rewriterEhcType.FaultEH(),
                _ => throw new Exception("unexpected ehcType")
            };

            return new ehClause(tryBegin, tryEnd, handlerBegin, handlerEnd, type);
        }
    }

    private void ImportIL()
    {
        if (_methodBody == null) return;
        
        _il = _methodBody.GetILAsByteArray() ?? [];
        
        _offsetToInstr = new IlInstr[_il.Length + 1];

        _back.next = _back;
        _back.prev = _back;
        _offsetToInstr[_il.Length] = _back;
        int offset = 0;
        bool branch = false;
        while (offset < _il.Length)
        {
            int opOffset = offset;
            (OpCode op, int _) = OpCodeOp.GetOpCode(_il, offset);
            offset += op.Size;
            int size =
                op.OperandType switch
                {
                    OperandType.InlineNone or
                        OperandType.InlineSwitch => 0,
                    OperandType.ShortInlineVar or
                        OperandType.ShortInlineI or
                        OperandType.ShortInlineBrTarget => 1,
                    OperandType.InlineVar => 2,
                    OperandType.InlineMethod or
                        OperandType.InlineI or
                        OperandType.InlineType or
                        OperandType.InlineString or
                        OperandType.InlineSig or
                        OperandType.InlineTok or
                        OperandType.ShortInlineR or
                        OperandType.InlineField or
                        OperandType.InlineBrTarget => 4,
                    OperandType.InlineI8 or
                        OperandType.InlineR => 8,
                    _ => throw new Exception("unreachable " + op.OperandType.ToString())
                };
            if (offset + size > _il.Length) throw new Exception("IL stream unexpectedly ended!");
            IlInstr.InsertBefore(_back, new IlInstr.Instr(op, opOffset));
            _offsetToInstr[opOffset] = _back.prev;
            IlInstr instr = _offsetToInstr[opOffset];
            Debug.Assert(_back.prev == instr);
            switch (op.OperandType)
            {
                case OperandType.InlineNone:
                {
                    instr.arg = new ILInstrOperand.NoArg();
                    break;
                }
                case OperandType.ShortInlineVar:
                case OperandType.ShortInlineI:
                {
                    instr.arg = new ILInstrOperand.Arg8(_il[offset]);
                    break;
                }
                case OperandType.InlineVar:
                {
                    instr.arg = new ILInstrOperand.Arg16(BitConverter.ToInt16(_il, offset));
                    break;
                }

                case OperandType.InlineMethod:
                {
                    var token = BitConverter.ToInt32(_il, offset);
                    instr.arg = new ILInstrOperand.ResolvedMethod(
                        IlInstanceBuilder.GetMethod(methodBase, token)
                    );
                    break;
                }
                case OperandType.InlineType:
                {
                    var token = BitConverter.ToInt32(_il, offset);
                    instr.arg = new ILInstrOperand.ResolvedType(IlInstanceBuilder.GetType(methodBase, token));
                    break;
                }
                case OperandType.InlineString:
                {
                    var token = BitConverter.ToInt32(_il, offset);
                    instr.arg = new ILInstrOperand.ResolvedString(IlInstanceBuilder.GetString(methodBase, token));
                    break;
                }
                case OperandType.InlineSig:
                {
                    var token = BitConverter.ToInt32(_il, offset);

                    instr.arg = new ILInstrOperand.ResolvedSignature(
                        IlInstanceBuilder.GetSignature(methodBase, token));

                    break;
                }
                case OperandType.InlineTok:
                {
                    var token = BitConverter.ToInt32(_il, offset);
                    instr.arg = new ILInstrOperand.ResolvedMember(
                        IlInstanceBuilder.GetMember(methodBase, token));
                    break;
                }
                case OperandType.InlineField:
                {
                    var token = BitConverter.ToInt32(_il, offset);
                    instr.arg = new ILInstrOperand.ResolvedField(
                        IlInstanceBuilder.GetField(methodBase, token));
                    break;
                }

                case OperandType.InlineI:
                case OperandType.ShortInlineR:
                {
                    instr.arg = new ILInstrOperand.Arg32(BitConverter.ToInt32(_il, offset));
                    break;
                }
                case OperandType.InlineI8:
                case OperandType.InlineR:
                {
                    instr.arg = new ILInstrOperand.Arg64(BitConverter.ToInt64(_il, offset));
                    break;
                }
                case OperandType.ShortInlineBrTarget:
                {
                    int delta = Convert.ToInt32((sbyte)_il[offset]);

                    instr.arg = new ILInstrOperand.Arg32(offset + delta + sizeof(sbyte));
                    branch = true;
                    break;
                }
                case OperandType.InlineBrTarget:
                {
                    int delta = BitConverter.ToInt32(_il, offset);
                    instr.arg = new ILInstrOperand.Arg32(delta + sizeof(int) + offset);
                    branch = true;
                    break;
                }
                case OperandType.InlineSwitch:
                {
                    int sizeOfInt = sizeof(int);
                    if (offset + sizeOfInt > _il.Length)
                    {
                        throw new Exception("IL stream unexpectedly ended!");
                    }

                    int targetCnt = BitConverter.ToInt32(_il, offset);
                    instr.arg = new ILInstrOperand.Arg32(targetCnt);
                    offset += sizeOfInt;
                    int baseOffset = offset + targetCnt * sizeOfInt;
                    for (int i = 0; i < targetCnt; i++)
                    {
                        if (offset + sizeOfInt > _il.Length)
                        {
                            throw new Exception("IL stream unexpectedly ended!");
                        }

                        IlInstr instrArg = new IlInstr.SwitchArg(i)
                        {
                            arg = new ILInstrOperand.Arg32(BitConverter.ToInt32(_il, offset) + baseOffset)
                        };
                        offset += sizeOfInt;

                        IlInstr.InsertBefore(_back, instrArg);
                    }

                    branch = true;
                    break;
                }
                default:
                    throw new Exception("Unexpected operand type!");
            }

            Debug.Assert(instr is not null);
            offset += size;
        }

        if (offset != _il.Length)
        {
            throw new Exception("offset != il.Length");
        }

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        Debug.Assert(ILInstrs().All(i => i is not null));
        try
        {
            if (IlInstanceBuilder.MethodFilters.All(f => !f(methodBase))) return;

            var asmPath = methodBase.Module.Assembly.Location;
            if (!moduleReaderCache.ContainsKey(asmPath))
                moduleReaderCache.Add(asmPath, ModuleDefinition.ReadModule(asmPath, readerParameters));
            var module = moduleReaderCache[asmPath];

            var monoDeclType = module.GetTypes().First(t =>
                t.MetadataToken.ToInt32() == (methodBase.ReflectedType ?? methodBase.DeclaringType)!.MetadataToken);
            var monoMethod = methodBase.IsConstructor
                ? monoDeclType.GetConstructors().First(m =>
                    m.MetadataToken.ToInt32() == methodBase.MetadataToken)
                : monoDeclType.GetMethods().First(m =>
                    m.MetadataToken.ToInt32() == methodBase.MetadataToken);
            var insts = monoMethod.Body.Instructions;
            string? filePath = null;
            SequencePoint? sp = null;
            foreach (var inst in insts)
            {
                _ilMono.Add(new IlMonoInst(inst));
                sp = monoMethod.DebugInformation.GetSequencePoint(inst) ?? sp;
                if (sp != null)
                {
                    _ilMono.Last().SequencePoint = sp;
                    filePath ??= sp.Document.Url;
                }
            }
            FilePath = filePath;
        }
        catch (Exception e)
        {
            if (!e.Message.Contains("em.Private.CoreLib.pdb"))
                Console.WriteLine($"\n{methodBase.Name}\n\n");
        }
        if (!branch) return;
        
        foreach (var cur in ILInstrs())
        {
            if (!cur.IsJump) continue;
            if (cur.arg is ILInstrOperand.Arg32 a32)
            {
                Debug.Assert(_offsetToInstr[a32.value] != null);

                cur.arg = new ILInstrOperand.Target(_offsetToInstr[a32.value]);
            }
            else
            {
                throw new Exception("Wrong operand of branching instruction!");
            }
        }
    }

    private static ReaderParameters readerParameters = new ReaderParameters
    {
        SymbolReaderProvider = new PdbReaderProvider(),
        ReadSymbols = true
    };

    private static Dictionary<string, ModuleDefinition> moduleReaderCache = new Dictionary<string, ModuleDefinition>();

    public IEnumerable<IlInstr> ILInstrs()
    {
        IlInstr cur = _back.next;
        while (cur != _back)
        {
            yield return cur;
            cur = cur.next;
        }
    }
}