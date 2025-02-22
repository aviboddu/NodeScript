namespace NodeScript;

using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static NodeScript.CompilerUtils;
using static OpCode;

[DebuggerDisplay("nextInstruction = {nextInstruction, nq}, stack = {stack, nq}")]
internal class RegularNode : Node
{
    public Node[]? outputs;
    private readonly InternalErrorHandler runtimeError;

    private readonly byte[] code;
    private readonly object[] constants;
    private readonly object[] stack;
    private readonly object[] variables;
    private readonly BitArray initVar;
    private readonly int[] lines;

    private int nextInstruction = 0;
    private bool panic = false;
    private int stackTop = 0;

    public RegularNode(Compiler.CompiledData compiledData, InternalErrorHandler runtimeError, Node[]? outputs = null)
    {
        code = compiledData.Code;
        constants = compiledData.Constants;
        lines = compiledData.Lines;
        stack = new object[compiledData.MaxStackSize];
        variables = new object[compiledData.NumVariables];
        initVar = new(compiledData.NumVariables);
        this.outputs = outputs;
        this.runtimeError = runtimeError;

        InitGlobals();
    }

    private void InitGlobals()
    {
        variables[INPUT_VARIABLE_IDX] = string.Empty; // input
        initVar[INPUT_VARIABLE_IDX] = true;

        variables[MEM_VARIABLE_IDX] = string.Empty; // mem
        initVar[MEM_VARIABLE_IDX] = true;
    }

    public int GetCurrentLine() => GetLine(nextInstruction - 1);

    private int GetLine(int instruction)
    {
        return ~Array.BinarySearch(lines, instruction);
    }

    public override bool PushInput(string input)
    {
        if (State == NodeState.IDLE)
        {
            variables[INPUT_VARIABLE_IDX] = input;
            nextInstruction = 0;
            State = NodeState.RUNNING;
            return true;
        }
        return false;
    }

    public override void StepLine()
    {
        if (State == NodeState.IDLE) return;
        while (!panic)
        {
            object v1, v2;
            int num1;
            bool b;
            ushort idx, jump_val;
            NativeDelegate func;
            Result result;
            OpCode nextOp = (OpCode)NextByte();
            switch (nextOp)
            {
                case CONSTANT: PushStack(constants[NextByte()]); break;
                case TRUE: PushStack(true); break;
                case FALSE: PushStack(false); break;
                case POP: PopStack(); break;
                case GET:
                    idx = NextShort();
                    if (initVar[idx])
                        PushStack(variables[idx]);
                    else
                        Err("Tried to get a variable that was not yet initialized");
                    break;
                case SET:
                    v1 = PopStack();
                    idx = NextShort();
                    variables[idx] = v1;
                    initVar[idx] = true;
                    return;
                case EQUAL:
                    v2 = PopStack();
                    v1 = PopStack();
                    PushStack(v1.Equals(v2));
                    break;
                case NOT_EQUAL:
                    v2 = PopStack();
                    v1 = PopStack();
                    PushStack(!v1.Equals(v2));
                    break;
                case GREATER:
                case GREATER_EQUAL:
                case LESS:
                case LESS_EQUAL:
                case SUBTRACT:
                case MULTIPLY:
                case DIVIDE:
                    BinaryArithmetic(nextOp);
                    break;
                case GREATERI:
                case GREATER_EQUALI:
                case LESSI:
                case LESS_EQUALI:
                case SUBTRACTI:
                case MULTIPLYI:
                case DIVIDEI:
                    v2 = PopStack();
                    v1 = PopStack();
                    BinaryArithmeticUnchecked(nextOp, Unsafe.Unbox<int>(v1), Unsafe.Unbox<int>(v2));
                    break;
                case ADD:
                    v2 = PopStack();
                    v1 = PopStack();
                    switch (v1)
                    {
                        case int n: if (ValidateType<int>(v2)) PushStack(n + Unsafe.Unbox<int>(v2)); break;
                        case string s: if (ValidateType<string>(v2)) PushStack(s + Unsafe.As<string>(v2)); break;
                        case string[] a: if (ValidateType<string[]>(v2)) PushStack(a.Concat(Unsafe.As<string[]>(v2))); break;
                        default: Err("Arguments must be either int, string or string[]"); break;
                    }
                    break;
                case ADDI:
                    v2 = PopStack();
                    v1 = PopStack();
                    PushStack(Unsafe.Unbox<int>(v1) + Unsafe.Unbox<int>(v2));
                    break;
                case ADDS:
                    v2 = PopStack();
                    v1 = PopStack();
                    PushStack(Unsafe.As<string>(v1) + Unsafe.As<string>(v2));
                    break;
                case ADDA:
                    v2 = PopStack();
                    v1 = PopStack();
                    PushStack(Unsafe.As<string[]>(v1).Concat(Unsafe.As<string[]>(v2)));
                    break;
                case AND:
                    v2 = PopStack();
                    v1 = PopStack();
                    if (ValidateType<bool>(v1, v2)) PushStack(Unsafe.Unbox<bool>(v1) && Unsafe.Unbox<bool>(v2));
                    break;
                case ANDB:
                    v2 = PopStack();
                    v1 = PopStack();
                    PushStack(Unsafe.Unbox<bool>(v1) && Unsafe.Unbox<bool>(v2));
                    break;
                case OR:
                    v2 = PopStack();
                    v1 = PopStack();
                    if (ValidateType<bool>(v1, v2)) PushStack(Unsafe.Unbox<bool>(v1) || Unsafe.Unbox<bool>(v2));
                    break;
                case ORB:
                    v2 = PopStack();
                    v1 = PopStack();
                    PushStack(Unsafe.Unbox<bool>(v1) || Unsafe.Unbox<bool>(v2));
                    break;
                case NEGATE:
                    v1 = PopStack();
                    if (ValidateType<int>(v1)) PushStack(-Unsafe.Unbox<int>(v1));
                    break;
                case NEGATEI:
                    v1 = PopStack();
                    PushStack(-Unsafe.Unbox<int>(v1));
                    break;
                case NOT:
                    v1 = PopStack();
                    if (ValidateType<bool>(v1)) PushStack(!Unsafe.Unbox<bool>(v1));
                    break;
                case NOTB:
                    v1 = PopStack();
                    PushStack(!Unsafe.Unbox<bool>(v1));
                    break;
                case PRINT:
                    v2 = PopStack();
                    v1 = PopStack();
                    if (ValidateType<int>(v1) && ValidateType<string>(v2))
                    {
                        if ((!outputs?[Unsafe.Unbox<int>(v1)].PushInput(Unsafe.As<string>(v2))) ?? true)
                        {
                            PushStack(v1);
                            PushStack(v2);
                            nextInstruction--;
                            State = NodeState.BLOCKED;
                        }
                        else
                        {
                            State = NodeState.RUNNING;
                        }
                    }
                    return;
                case PRINTIS:
                    v2 = PopStack();
                    v1 = PopStack();
                    if ((!outputs?[Unsafe.Unbox<int>(v1)].PushInput(Unsafe.As<string>(v2))) ?? true)
                    {
                        PushStack(v1);
                        PushStack(v2);
                        nextInstruction--;
                        State = NodeState.BLOCKED;
                    }
                    else
                    {
                        State = NodeState.RUNNING;
                    }
                    return;
                case JUMP:
                    jump_val = NextShort();
                    nextInstruction += jump_val;
                    return;
                case JUMP_IF_FALSE:
                    v1 = PopStack();
                    b = v1 switch
                    {
                        int i => i != 0,
                        bool l => l,
                        string s => s.Length != 0,
                        string[] a => a.Length != 0,
                        _ => false,
                    };
                    jump_val = NextShort();
                    if (!b)
                        nextInstruction += jump_val;
                    return;
                case CALL_TYPE_KNOWN:
                case CALL:
                    func = (NativeDelegate)constants[NextByte()];
                    num1 = NextByte();
                    result = CallFunc(func, num1);
                    if (!result.Success())
                        Err(result.message!);
                    else
                        PushStack(result.GetValue()!);
                    break;
                case RETURN:
                    State = NodeState.IDLE;
                    initVar.SetAll(false);
                    initVar[INPUT_VARIABLE_IDX] = true;
                    initVar[MEM_VARIABLE_IDX] = true;
                    return;
                case ENDIF:
                case NOP: return;
            }
        }
    }

    public override void Reset()
    {
        State = NodeState.IDLE;
        nextInstruction = 0;
        initVar.SetAll(false);
        panic = false;
        InitGlobals();
    }

    private byte NextByte() => code[nextInstruction++];
    private ushort NextShort() => (ushort)((NextByte() << 8) | (NextByte() & 0xff));

    private void PushStack(object val) => stack[stackTop++] = val;
    private object PopStack() => stack[--stackTop];

    private void BinaryArithmetic(OpCode op)
    {
        object val2 = PopStack();
        object val1 = PopStack();

        if (!ValidateType<int>(val1, val2))
            return;

        BinaryArithmeticUnchecked(op, Unsafe.Unbox<int>(val1), Unsafe.Unbox<int>(val2));
    }

    private void BinaryArithmeticUnchecked(OpCode op, int num1, int num2)
    {
        switch (op)
        {
            case GREATER:
            case GREATERI: PushStack(num1 > num2); break;
            case GREATER_EQUAL:
            case GREATER_EQUALI: PushStack(num1 >= num2); break;
            case LESS:
            case LESSI: PushStack(num1 < num2); break;
            case LESS_EQUAL:
            case LESS_EQUALI: PushStack(num1 <= num2); break;
            case SUBTRACT:
            case SUBTRACTI: PushStack(num1 - num2); break;
            case MULTIPLY:
            case MULTIPLYI: PushStack(num1 * num2); break;
            case DIVIDE:
            case DIVIDEI:
                if (num2 == 0)
                    Err("Cannot divide by 0");
                else
                    PushStack(num1 / num2);
                break;
        }
    }

    private Result CallFunc(NativeDelegate func, int numParams)
    {
        stackTop -= numParams;
        return func(stack.AsSpan(stackTop, numParams));
    }

    public override Node[] OutputNodes()
    {
        return outputs is null ? [] : outputs;
    }

    private bool ValidateType<T>(params object[] vals)
    {
        bool valid = vals.All((v) => v is T);
        if (!valid) Err($"Arguments must be of type {typeof(T).Name}");
        return valid;
    }

    private void Err(string message)
    {
        State = NodeState.IDLE;
        panic = true;
        runtimeError.Invoke(GetLine(nextInstruction - 1), message);
    }

    public override string ToString() => "RegularNode";
}