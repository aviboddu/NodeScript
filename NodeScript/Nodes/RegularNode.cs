namespace NodeScript;

using System.Buffers;
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
    private readonly Stack<object> stack = new();
    private readonly object[] variables;
    private readonly BitArray initVar;
    private readonly int[] lines;

    private int nextInstruction = 0;
    private bool panic = false;

    public RegularNode(Compiler.CompiledData compiledData, InternalErrorHandler runtimeError, Node[]? outputs = null)
    {
        code = compiledData.Code;
        constants = compiledData.Constants;
        lines = compiledData.Lines;
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
            string name;
            object v1, v2;
            int num1;
            bool b;
            ushort idx, jump_val;
            Result result;
            OpCode nextOp = (OpCode)NextByte();
            switch (nextOp)
            {
                case CONSTANT: stack.Push(constants[NextByte()]); break;
                case TRUE: stack.Push(true); break;
                case FALSE: stack.Push(false); break;
                case POP: stack.Pop(); break;
                case GET:
                    idx = NextShort();
                    if (initVar[idx])
                        stack.Push(variables[idx]);
                    else
                        Err("Tried to get a variable that was not yet initialized");
                    break;
                case SET:
                    v1 = stack.Pop();
                    idx = NextShort();
                    variables[idx] = v1;
                    initVar[idx] = true;
                    return;
                case EQUAL:
                    v2 = stack.Pop();
                    v1 = stack.Pop();
                    stack.Push(v1.Equals(v2));
                    break;
                case NOT_EQUAL:
                    v2 = stack.Pop();
                    v1 = stack.Pop();
                    stack.Push(!v1.Equals(v2));
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
                    v2 = stack.Pop();
                    v1 = stack.Pop();
                    BinaryArithmeticUnchecked(nextOp, Unsafe.Unbox<int>(v1), Unsafe.Unbox<int>(v2));
                    break;
                case ADD:
                    v2 = stack.Pop();
                    v1 = stack.Pop();
                    switch (v1)
                    {
                        case int n: if (ValidateType<int>(v2)) stack.Push(n + Unsafe.Unbox<int>(v2)); break;
                        case string s: if (ValidateType<string>(v2)) stack.Push(s + Unsafe.As<string>(v2)); break;
                        case string[] a: if (ValidateType<string[]>(v2)) stack.Push(a.Concat(Unsafe.As<string[]>(v2))); break;
                        default: Err("Arguments must be either int, string or string[]"); break;
                    }
                    break;
                case ADDI:
                    v2 = stack.Pop();
                    v1 = stack.Pop();
                    stack.Push(Unsafe.Unbox<int>(v1) + Unsafe.Unbox<int>(v2));
                    break;
                case ADDS:
                    v2 = stack.Pop();
                    v1 = stack.Pop();
                    stack.Push(Unsafe.As<string>(v1) + Unsafe.As<string>(v2));
                    break;
                case ADDA:
                    v2 = stack.Pop();
                    v1 = stack.Pop();
                    stack.Push(Unsafe.As<string[]>(v1).Concat(Unsafe.As<string[]>(v2)));
                    break;
                case AND:
                    v2 = stack.Pop();
                    v1 = stack.Pop();
                    if (ValidateType<bool>(v1, v2)) stack.Push(Unsafe.Unbox<bool>(v1) && Unsafe.Unbox<bool>(v2));
                    break;
                case ANDB:
                    v2 = stack.Pop();
                    v1 = stack.Pop();
                    stack.Push(Unsafe.Unbox<bool>(v1) && Unsafe.Unbox<bool>(v2));
                    break;
                case OR:
                    v2 = stack.Pop();
                    v1 = stack.Pop();
                    if (ValidateType<bool>(v1, v2)) stack.Push(Unsafe.Unbox<bool>(v1) || Unsafe.Unbox<bool>(v2));
                    break;
                case ORB:
                    v2 = stack.Pop();
                    v1 = stack.Pop();
                    stack.Push(Unsafe.Unbox<bool>(v1) || Unsafe.Unbox<bool>(v2));
                    break;
                case NEGATE:
                    v1 = stack.Pop();
                    if (ValidateType<int>(v1)) stack.Push(-Unsafe.Unbox<int>(v1));
                    break;
                case NEGATEI:
                    v1 = stack.Pop();
                    stack.Push(-Unsafe.Unbox<int>(v1));
                    break;
                case NOT:
                    v1 = stack.Pop();
                    if (ValidateType<bool>(v1)) stack.Push(!Unsafe.Unbox<bool>(v1));
                    break;
                case NOTB:
                    v1 = stack.Pop();
                    stack.Push(!Unsafe.Unbox<bool>(v1));
                    break;
                case PRINT:
                    v2 = stack.Pop();
                    v1 = stack.Pop();
                    if (ValidateType<int>(v1) && ValidateType<string>(v2))
                    {
                        if ((!outputs?[Unsafe.Unbox<int>(v1)].PushInput(Unsafe.As<string>(v2))) ?? true)
                        {
                            stack.Push(v1);
                            stack.Push(v2);
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
                    v2 = stack.Pop();
                    v1 = stack.Pop();
                    if ((!outputs?[Unsafe.Unbox<int>(v1)].PushInput(Unsafe.As<string>(v2))) ?? true)
                    {
                        stack.Push(v1);
                        stack.Push(v2);
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
                    v1 = stack.Pop();
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
                case CALL:
                    name = (string)constants[NextByte()];
                    num1 = NextByte();
                    if (!NativeFuncs.NativeFunctions.TryGetValue(name, out NativeDelegate? func))
                    {
                        Err($"Function {name} does not exist");
                        break;
                    }
                    result = CallFunc(func, num1);
                    if (!result.Success())
                        Err(result.message!);
                    else
                        stack.Push(result.GetValue()!);
                    break;
                case CALL_TYPE_KNOWN:
                    name = (string)constants[NextByte()];
                    num1 = NextByte();
                    if (!NativeFuncsKnownType.NativeFunctions.TryGetValue(name, out NativeDelegate? knownFunc))
                    {
                        Err($"Function {name} does not exist");
                        break;
                    }
                    result = CallFunc(knownFunc, num1);
                    if (!result.Success())
                        Err(result.message!);
                    else
                        stack.Push(result.GetValue()!);
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

    private void BinaryArithmetic(OpCode op)
    {
        object val2 = stack.Pop();
        object val1 = stack.Pop();

        if (!ValidateType<int>(val1, val2))
            return;

        BinaryArithmeticUnchecked(op, Unsafe.Unbox<int>(val1), Unsafe.Unbox<int>(val2));
    }

    private void BinaryArithmeticUnchecked(OpCode op, int num1, int num2)
    {
        switch (op)
        {
            case GREATER:
            case GREATERI: stack.Push(num1 > num2); break;
            case GREATER_EQUAL:
            case GREATER_EQUALI: stack.Push(num1 >= num2); break;
            case LESS:
            case LESSI: stack.Push(num1 < num2); break;
            case LESS_EQUAL:
            case LESS_EQUALI: stack.Push(num1 <= num2); break;
            case SUBTRACT:
            case SUBTRACTI: stack.Push(num1 - num2); break;
            case MULTIPLY:
            case MULTIPLYI: stack.Push(num1 * num2); break;
            case DIVIDE:
            case DIVIDEI:
                if (num2 == 0)
                    Err("Cannot divide by 0");
                else
                    stack.Push(num1 / num2);
                break;
        }
    }

    private Result CallFunc(NativeDelegate func, int numParams)
    {
        object[] parameters = ArrayPool<object>.Shared.Rent(numParams);
        int paramsToAdd = numParams;
        while (paramsToAdd-- > 0)
            parameters[paramsToAdd] = stack.Pop();
        Result res = func(parameters.AsSpan()[..numParams]);
        ArrayPool<object>.Shared.Return(parameters);
        return res;
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