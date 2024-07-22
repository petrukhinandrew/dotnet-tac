using System.Reflection;

namespace Usvm.IL.Parser;

public abstract record SMValue
{
    public record Local(int Index, bool AsAddr = false) : SMValue
    {
        public override string Name
        {
            get
            {
                string pref = AsAddr ? "&" : "";
                return pref + Logger.LocalVarName(Index);
            }
        }
    }
    public record Arg(int Index, bool AsAddr = false) : SMValue
    {
        public override string Name
        {
            get
            {
                string pref = AsAddr ? "&" : "";
                return pref + Logger.ArgVarName(Index);
            }
        }
    }
    public record Temp(int Index, bool AsAddr = false) : SMValue
    {
        public override string Name
        {
            get
            {
                string pref = AsAddr ? "&" : "";
                return pref + Logger.TempVarName(Index);
            }
        }
    }

    public record Const<T>(T Value, bool AsRef = false) : SMValue
    {
        public override string Name
        {
            get
            {
                return Value!.ToString() ?? "Const";
            }
        }
    }
    public record Null() : SMValue
    {
        public override string Name
        {
            get
            {
                return "null";
            }
        }
    }
    abstract public string Name { get; }
}

class StackMachine
{
    private Dictionary<Type, List<LocalVariableInfo>> _locals = new Dictionary<Type, List<LocalVariableInfo>>();
    private List<ParameterInfo> _args;
    private int _nextTempIdx = 0;
    private int _nextTacLineIdx = 0;
    private Stack<SMValue> _stack;
    private List<string> _tac = new List<string>();
    private ILInstr _begin;
    private Module _declaringModule;
    private MethodInfo _methodInfo;

    public StackMachine(Module declaringModule, MethodInfo methodInfo, IList<LocalVariableInfo> locals, int maxDepth, ILInstr begin)
    {
        _begin = begin;
        _declaringModule = declaringModule;
        _methodInfo = methodInfo;
        _args = _methodInfo.GetParameters().ToList();
        _locals = locals.GroupBy(v => v.LocalType).ToDictionary(bt => bt.Key, bt => bt.ToList());
        _stack = new Stack<SMValue>(maxDepth);
        ProcessIL();
    }
    private void ProcessIL()
    {
        ILInstr curInstr = _begin;
        while (curInstr != _begin.prev)
        {
            ILInstr.Instr instr;
            if (curInstr is ILInstr.Instr ilinstr)
            {
                instr = ilinstr;
            }
            else
            {
                curInstr = curInstr.next;
                continue;
            }
            switch (instr.opCode.Name)
            {
                case "nop":
                case "break": break;
                case "ldarg.0": _stack.Push(new SMValue.Arg(0)); break;
                case "ldarg.1": _stack.Push(new SMValue.Arg(1)); break;
                case "ldarg.2": _stack.Push(new SMValue.Arg(2)); break;
                case "ldarg.3": _stack.Push(new SMValue.Arg(3)); break;
                case "ldarg.s":
                    _stack.Push(new SMValue.Arg(((ILInstrOperand.Arg8)instr.arg).value)); break;
                case "ldloc.0": _stack.Push(new SMValue.Local(0)); break;
                case "ldloc.1": _stack.Push(new SMValue.Local(1)); break;
                case "ldloc.2": _stack.Push(new SMValue.Local(2)); break;
                case "ldloc.3": _stack.Push(new SMValue.Local(3)); break;
                case "ldloc.s":
                    _stack.Push(new SMValue.Local(((ILInstrOperand.Arg8)instr.arg).value));
                    break;
                case "stloc.0": _tac.Add(string.Format("{0} = {1};", Logger.LocalVarName(0), _stack.Pop().Name)); break;
                case "stloc.1": _tac.Add(string.Format("{0} = {1};", Logger.LocalVarName(1), _stack.Pop().Name)); break;
                case "stloc.2": _tac.Add(string.Format("{0} = {1};", Logger.LocalVarName(2), _stack.Pop().Name)); break;
                case "stloc.3": _tac.Add(string.Format("{0} = {1};", Logger.LocalVarName(3), _stack.Pop().Name)); break;
                case "stloc.s":
                    _tac.Add(string.Format("{0} = {1};", Logger.LocalVarName(((ILInstrOperand.Arg8)instr.arg).value), _stack.Pop().Name)); break;
                case "ldarga.s":
                    _stack.Push(new SMValue.Arg(((ILInstrOperand.Arg8)instr.arg).value, AsAddr: true)); break;
                case "starg.s":
                    _tac.Add(string.Format("{0} = {1};", Logger.ArgVarName(((ILInstrOperand.Arg8)instr.arg).value), _stack.Pop().Name)); break;
                case "ldloca.s":
                    _stack.Push(new SMValue.Local(((ILInstrOperand.Arg8)instr.arg).value, AsAddr: true)); break;
                case "ldnull": _stack.Push(new SMValue.Null()); break;
                case "ldc.i4.m1":
                case "ldc.i4.M1": _stack.Push(new SMValue.Const<Int32>(-1)); break;
                case "ldc.i4.0": _stack.Push(new SMValue.Const<Int32>(0)); break;
                case "ldc.i4.1": _stack.Push(new SMValue.Const<Int32>(1)); break;
                case "ldc.i4.2": _stack.Push(new SMValue.Const<Int32>(2)); break;
                case "ldc.i4.3": _stack.Push(new SMValue.Const<Int32>(3)); break;
                case "ldc.i4.4": _stack.Push(new SMValue.Const<Int32>(4)); break;
                case "ldc.i4.5": _stack.Push(new SMValue.Const<Int32>(5)); break;
                case "ldc.i4.6": _stack.Push(new SMValue.Const<Int32>(6)); break;
                case "ldc.i4.7": _stack.Push(new SMValue.Const<Int32>(7)); break;
                case "ldc.i4.8": _stack.Push(new SMValue.Const<Int32>(8)); break;
                case "ldc.i4.s": _stack.Push(new SMValue.Const<Int32>(((ILInstrOperand.Arg8)instr.arg).value)); break;
                case "ldc.i4": _stack.Push(new SMValue.Const<Int32>(((ILInstrOperand.Arg32)instr.arg).value)); break;
                case "ldc.i8": _stack.Push(new SMValue.Const<Int64>(((ILInstrOperand.Arg64)instr.arg).value)); break;
                case "ldc.r4": _stack.Push(new SMValue.Const<float>(((ILInstrOperand.Arg32)instr.arg).value)); break;
                case "ldc.r8": _stack.Push(new SMValue.Const<double>(((ILInstrOperand.Arg64)instr.arg).value)); break;
                case "ldstr": _stack.Push(new SMValue.Const<string>(safeStringResolve(((ILInstrOperand.Arg32)instr.arg).value))); break;
                case "dup": _stack.Push(_stack.Peek()); break;
                case "pop": _stack.Pop(); break;
                case "jmp": throw new Exception("jmp occured");
                case "call":
                    MethodBase? callResolvedMethod = safeMethodResolve(((ILInstrOperand.Arg32)instr.arg).value);
                    if (callResolvedMethod == null) break;
                    string callResolvedMethodRetVal = "";
                    string callResolvedMethodArgs = " ";
                    // TODO handle in out args 
                    foreach (var p in callResolvedMethod.GetParameters().Where(p => p.IsRetval))
                    {
                        callResolvedMethodRetVal += p.ToString() + " ";
                    }
                    for (int i = 0; i < callResolvedMethod.GetParameters().Where(p => !p.IsRetval).Count(); i++)
                    {
                        callResolvedMethodArgs += _stack.Pop().Name + " ";
                    }
                    string callResolvedMethodRetValPref = "";
                    if (callResolvedMethodRetVal != "")
                    {
                        _stack.Push(new SMValue.Temp(_nextTempIdx++));
                        callResolvedMethodRetValPref = _stack.Peek().Name + " = ";
                    }
                    _tac.Add(callResolvedMethodRetValPref + callResolvedMethodRetVal + callResolvedMethod.DeclaringType + " " + callResolvedMethod.Name + callResolvedMethodArgs + ";");
                    break;
                case "ret":
                    if (_stack.Count != 0)
                    {
                        _tac.Add("return " + _stack.Pop().Name + ";");
                    }
                    else
                    {
                        _tac.Add("return;");
                    }
                    break;
                case "add":
                    {
                        SMValue add2 = _stack.Pop();
                        SMValue add1 = _stack.Pop();
                        _stack.Push(new SMValue.Temp(_nextTempIdx++));
                        _tac.Add(string.Format("{0} = {1} + {2};", _stack.Peek().Name, add1.Name, add2.Name)); break;
                    }
                case "sub":
                    {
                        SMValue sub2 = _stack.Pop();
                        SMValue sub1 = _stack.Pop();
                        _stack.Push(new SMValue.Temp(_nextTempIdx++));
                        _tac.Add(string.Format("{0} = {1} - {2};", _stack.Peek().Name, sub1.Name, sub2.Name)); break;
                    }
                case "mul":
                    {
                        SMValue mul2 = _stack.Pop();
                        SMValue mul1 = _stack.Pop();
                        _stack.Push(new SMValue.Temp(_nextTempIdx++));
                        _tac.Add(string.Format("{0} = {1} * {2};", _stack.Peek().Name, mul1.Name, mul2.Name)); break;
                    }
                case "div.un":
                case "div":
                    {
                        SMValue div2 = _stack.Pop();
                        SMValue div1 = _stack.Pop();
                        _stack.Push(new SMValue.Temp(_nextTempIdx++));
                        _tac.Add(string.Format("{0} = {1} / {2};", _stack.Peek().Name, div1.Name, div2.Name)); break;
                    }
                case "rem.un":
                case "rem":
                    {
                        SMValue rem2 = _stack.Pop();
                        SMValue rem1 = _stack.Pop();
                        _stack.Push(new SMValue.Temp(_nextTempIdx++));
                        _tac.Add(string.Format("{0} = {1} % {2};", _stack.Peek().Name, rem1.Name, rem2.Name)); break;
                    }
                case "and":
                    {
                        SMValue and2 = _stack.Pop();
                        SMValue and1 = _stack.Pop();
                        _stack.Push(new SMValue.Temp(_nextTempIdx++));
                        _tac.Add(string.Format("{0} = {1} & {2};", _stack.Peek().Name, and1.Name, and2.Name)); break;
                    }
                case "or":
                    {
                        SMValue or2 = _stack.Pop();
                        SMValue or1 = _stack.Pop();
                        _stack.Push(new SMValue.Temp(_nextTempIdx++));
                        _tac.Add(string.Format("{0} = {1} | {2};", _stack.Peek().Name, or1.Name, or2.Name)); break;
                    }
                case "xor":
                    {
                        SMValue xor2 = _stack.Pop();
                        SMValue xor1 = _stack.Pop();
                        _stack.Push(new SMValue.Temp(_nextTempIdx++));
                        _tac.Add(string.Format("{0} = {1} ^ {2};", _stack.Peek().Name, xor1.Name, xor2.Name)); break;
                    }
                case "shl":
                    {
                        SMValue shl2 = _stack.Pop();
                        SMValue shl1 = _stack.Pop();
                        _stack.Push(new SMValue.Temp(_nextTempIdx++));
                        _tac.Add(string.Format("{0} = {1} << {2};", _stack.Peek().Name, shl1.Name, shl2.Name)); break;
                    }
                case "shr.un":
                case "shr":
                    {
                        SMValue shr2 = _stack.Pop();
                        SMValue shr1 = _stack.Pop();
                        _stack.Push(new SMValue.Temp(_nextTempIdx++));
                        _tac.Add(string.Format("{0} = {1} >> {2};", _stack.Peek().Name, shr1.Name, shr2.Name)); break;
                    }
                case "neg":
                    {
                        SMValue negVal = _stack.Peek();
                        _tac.Add(string.Format("{0} = -{0};", negVal.Name)); break;
                    }
                case "not":
                    {
                        SMValue notVal = _stack.Peek();
                        _tac.Add(string.Format("{0} = !{0};", notVal.Name)); break;
                    }
                default: Console.WriteLine("unhandled instr " + instr.ToString()); break;
            }
            curInstr = curInstr.next;
        }
    }
    private MethodBase? safeMethodResolve(int target)
    {
        try
        {
            return _declaringModule.ResolveMethod(target);
        }
        catch (Exception e)
        {
            Console.WriteLine("error resolving method " + e.Message);
            return null;
        }

    }
    private string safeStringResolve(int target)
    {
        try
        {
            return _declaringModule.ResolveString(target);
        }
        catch (Exception e)
        {
            Console.WriteLine("error resolving string " + e.Message);
            return "";
        }
    }
    public List<string> ListLocalVars()
    {
        List<string> res = new List<string>();
        foreach (var mapping in _locals)
        {
            string buf = string.Format("{0} {1};", mapping.Key.ToString(), string.Join(", ", mapping.Value.Select(v => Logger.LocalVarName(v.LocalIndex))));
            res.Add(buf);
        }
        return res;
    }
    public string ListMethodSignature()
    {
        return string.Format("{0} {1}({2})", _methodInfo.ReturnType, _methodInfo.Name, string.Join(",", _methodInfo.GetParameters().Select(mi => mi.ToString())));
    }
    public void DumpMethodSignature()
    {
        Console.WriteLine(ListMethodSignature());
    }
    public void DumpLocalVars()
    {
        foreach (var v in ListLocalVars())
        {
            Console.WriteLine(v);
        }
    }
    public void DumpTAC()
    {
        foreach (var line in _tac)
        {
            Console.WriteLine(line);
        }
    }
    public void DumpAll()
    {
        DumpMethodSignature();
        DumpLocalVars();
        DumpTAC();
    }
}