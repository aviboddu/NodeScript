namespace NodeScript;

using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static NodeScript.CompilerUtils;
using static OpCode;

[DebuggerDisplay("nextInstruction = {nextInstruction, nq}, stack = {stack, nq}")]
internal sealed class RegularNode : Node
{
    public Node[]? outputs;
    private readonly InternalErrorHandler runtimeError;

    private readonly ExecutionPlan plan;
    private readonly Value[] stack;
    private ref Value stackPtr => ref MemoryMarshal.GetArrayDataReference(stack);
    private readonly Value[] variables;
    private readonly BitArray initVar;
    private readonly int[] lines;

    private int nextInstruction = 0;
    private int instructionIndex = 0;
    private bool panic = false;
    private int stackTop = 0;

    public RegularNode(Compiler.CompiledData compiledData, InternalErrorHandler runtimeError, Node[]? outputs = null)
    {
        if (!ExecutionPlan.TryCreate(compiledData, out ExecutionPlan? executionPlan, out string? error))
            throw new ArgumentException(error, nameof(compiledData));
        plan = executionPlan!;
        lines = compiledData.Lines;
        stack = new Value[plan.MaximumStackDepth];
        variables = new Value[compiledData.NumVariables];
        initVar = new(compiledData.NumVariables);
        this.outputs = outputs;
        this.runtimeError = runtimeError;

        InitGlobals();
    }

    private void InitGlobals()
    {
        variables[INPUT_VARIABLE_IDX] = Value.FromString(string.Empty); // input
        initVar[INPUT_VARIABLE_IDX] = true;

        variables[MEM_VARIABLE_IDX] = Value.FromString(string.Empty); // mem
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
            panic = false;
            ClearFrameState();
            variables[INPUT_VARIABLE_IDX] = Value.FromString(input);
            nextInstruction = 0;
            instructionIndex = 0;
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
            Value v1, v2;
            int num1;
            bool b;
            ushort idx;
            NativeDelegate func;
            Result result;
            ref readonly PlannedInstruction instruction = ref plan[instructionIndex++];
            nextInstruction = instruction.Offset + instruction.Size;
            OpCode nextOp = instruction.OpCode;
            switch (nextOp)
            {
                case CONSTANT: PushStack(plan.GetConstant(instruction.ConstantIndex)); break;
                case TRUE: PushStack(Value.FromBool(true)); break;
                case FALSE: PushStack(Value.FromBool(false)); break;
                case POP: PopStack(); break;
                case GET:
                    idx = (ushort)instruction.Operand;
                    if (initVar[idx])
                        PushStack(variables[idx]);
                    else
                        Err("Tried to get a variable that was not yet initialized");
                    break;
                case SET:
                    v1 = PopStack();
                    idx = (ushort)instruction.Operand;
                    variables[idx] = v1;
                    initVar[idx] = true;
                    return;
                case EQUAL:
                    v2 = PopStack();
                    v1 = PopStack();
                    PushStack(Value.FromBool(v1.Equals(v2)));
                    break;
                case NOT_EQUAL:
                    v2 = PopStack();
                    v1 = PopStack();
                    PushStack(Value.FromBool(!v1.Equals(v2)));
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
                    v2 = PopStack();
                    v1 = PopStack();
                    PushStack(Value.FromBool(v1.AsInt() > v2.AsInt()));
                    break;
                case GREATER_EQUALI:
                    v2 = PopStack();
                    v1 = PopStack();
                    PushStack(Value.FromBool(v1.AsInt() >= v2.AsInt()));
                    break;
                case LESSI:
                    v2 = PopStack();
                    v1 = PopStack();
                    PushStack(Value.FromBool(v1.AsInt() < v2.AsInt()));
                    break;
                case LESS_EQUALI:
                    v2 = PopStack();
                    v1 = PopStack();
                    PushStack(Value.FromBool(v1.AsInt() <= v2.AsInt()));
                    break;
                case SUBTRACTI:
                    v2 = PopStack();
                    v1 = PopStack();
                    PushStack(Value.FromInt(v1.AsInt() - v2.AsInt()));
                    break;
                case MULTIPLYI:
                    v2 = PopStack();
                    v1 = PopStack();
                    PushStack(Value.FromInt(v1.AsInt() * v2.AsInt()));
                    break;
                case DIVIDEI:
                    v2 = PopStack();
                    v1 = PopStack();
                    if (v2.AsInt() == 0)
                        Err("Cannot divide by 0");
                    else
                        PushStack(Value.FromInt(v1.AsInt() / v2.AsInt()));
                    break;
                case ADD:
                    v2 = PopStack();
                    v1 = PopStack();
                    switch (v1.Kind)
                    {
                        case ValueKind.Int:
                            if (ValidateType<int>(v2)) PushStack(Value.FromInt(v1.AsInt() + v2.AsInt()));
                            break;
                        case ValueKind.String:
                            if (ValidateType<string>(v2)) PushStack(Value.FromString(v1.AsString() + v2.AsString()));
                            break;
                        case ValueKind.StringArray:
                            if (ValidateType<string[]>(v2)) PushStack(Value.FromStringArray(v1.AsStringArray().Concat(v2.AsStringArray()).ToArray()));
                            break;
                        default: Err("Arguments must be either int, string or string[]"); break;
                    }
                    break;
                case ADDI:
                    v2 = PopStack();
                    v1 = PopStack();
                    PushStack(Value.FromInt(v1.AsInt() + v2.AsInt()));
                    break;
                case ADDS:
                    v2 = PopStack();
                    v1 = PopStack();
                    PushStack(Value.FromString(v1.AsString() + v2.AsString()));
                    break;
                case ADDA:
                    v2 = PopStack();
                    v1 = PopStack();
                    PushStack(Value.FromStringArray(v1.AsStringArray().Concat(v2.AsStringArray()).ToArray()));
                    break;
                case AND:
                    v2 = PopStack();
                    v1 = PopStack();
                    if (ValidateType<bool>(v1, v2)) PushStack(Value.FromBool(v1.AsBool() && v2.AsBool()));
                    break;
                case ANDB:
                    v2 = PopStack();
                    v1 = PopStack();
                    PushStack(Value.FromBool(v1.AsBool() && v2.AsBool()));
                    break;
                case OR:
                    v2 = PopStack();
                    v1 = PopStack();
                    if (ValidateType<bool>(v1, v2)) PushStack(Value.FromBool(v1.AsBool() || v2.AsBool()));
                    break;
                case ORB:
                    v2 = PopStack();
                    v1 = PopStack();
                    PushStack(Value.FromBool(v1.AsBool() || v2.AsBool()));
                    break;
                case NEGATE:
                    v1 = PopStack();
                    if (ValidateType<int>(v1)) PushStack(Value.FromInt(-v1.AsInt()));
                    break;
                case NEGATEI:
                    v1 = PopStack();
                    PushStack(Value.FromInt(-v1.AsInt()));
                    break;
                case NOT:
                    v1 = PopStack();
                    if (ValidateType<bool>(v1)) PushStack(Value.FromBool(!v1.AsBool()));
                    break;
                case NOTB:
                    v1 = PopStack();
                    PushStack(Value.FromBool(!v1.AsBool()));
                    break;
                case PRINT:
                    v2 = PopStack();
                    v1 = PopStack();
                    if (ValidateType<int>(v1) && ValidateType<string>(v2))
                    {
                        int outputIndex = v1.AsInt();
                        if (outputs is null || outputIndex < 0 || outputIndex >= outputs.Length)
                        {
                            Err($"Output index {outputIndex} is out of range");
                        }
                        else if (!outputs[outputIndex].PushInput(v2.AsString()))
                        {
                            PushStack(v1);
                            PushStack(v2);
                            instructionIndex--;
                            nextInstruction = instruction.Offset;
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
                    int knownOutputIndex = v1.AsInt();
                    if (outputs is null || knownOutputIndex < 0 || knownOutputIndex >= outputs.Length)
                    {
                        Err($"Output index {knownOutputIndex} is out of range");
                    }
                    else if (!outputs[knownOutputIndex].PushInput(v2.AsString()))
                    {
                        PushStack(v1);
                        PushStack(v2);
                        instructionIndex--;
                        nextInstruction = instruction.Offset;
                        State = NodeState.BLOCKED;
                    }
                    else
                    {
                        State = NodeState.RUNNING;
                    }
                    return;
                case JUMP:
                    instructionIndex = instruction.Target;
                    nextInstruction = plan[instructionIndex].Offset;
                    return;
                case JUMP_IF_FALSE:
                    v1 = PopStack();
                    b = v1.Kind switch
                    {
                        ValueKind.Int => v1.AsInt() != 0,
                        ValueKind.Bool => v1.AsBool(),
                        ValueKind.String => v1.AsString().Length != 0,
                        ValueKind.StringArray => v1.AsStringArray().Length != 0,
                        _ => false,
                    };
                    if (!b)
                    {
                        instructionIndex = instruction.Target;
                        nextInstruction = plan[instructionIndex].Offset;
                    }
                    return;
                case CALL_TYPE_KNOWN:
                case CALL:
                    func = plan.GetFunction(instruction.ConstantIndex);
                    num1 = instruction.ArgumentCount;
                    result = CallFunc(func, num1);
                    if (!result.Success())
                        Err(result.message!);
                    else
                        PushStack(result.GetValue());
                    break;
                case RETURN:
                    State = NodeState.IDLE;
                    ClearFrameState();
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
        instructionIndex = 0;
        initVar.SetAll(false);
        panic = false;
        ClearAllVariableReferences();
        ClearStackReferences();
        InitGlobals();
    }

    private void PushStack(Value val)
    {
        Unsafe.Add(ref stackPtr, stackTop++) = val;
    }
    private Value PopStack()
    {
        ref Value valRef = ref Unsafe.Add(ref stackPtr, --stackTop);
        Value val = valRef;
        if (val.IsReference)
            valRef = default;
        return val;
    }

    private void BinaryArithmetic(OpCode op)
    {
        Value val2 = PopStack();
        Value val1 = PopStack();

        if (!ValidateType<int>(val1, val2))
            return;

        switch (op)
        {
            case GREATER: PushStack(Value.FromBool(val1.AsInt() > val2.AsInt())); break;
            case GREATER_EQUAL: PushStack(Value.FromBool(val1.AsInt() >= val2.AsInt())); break;
            case LESS: PushStack(Value.FromBool(val1.AsInt() < val2.AsInt())); break;
            case LESS_EQUAL: PushStack(Value.FromBool(val1.AsInt() <= val2.AsInt())); break;
            case SUBTRACT: PushStack(Value.FromInt(val1.AsInt() - val2.AsInt())); break;
            case MULTIPLY: PushStack(Value.FromInt(val1.AsInt() * val2.AsInt())); break;
            case DIVIDE:
                if (val2.AsInt() == 0)
                    Err("Cannot divide by 0");
                else
                    PushStack(Value.FromInt(val1.AsInt() / val2.AsInt()));
                break;
        }
    }

    private Result CallFunc(NativeDelegate func, int numParams)
    {
        stackTop -= numParams;
        Result result = func(stack.AsSpan(stackTop, numParams));
        for (int i = stackTop; i < stackTop + numParams; i++)
        {
            if (stack[i].IsReference)
                stack[i] = default;
        }
        return result;
    }

    public override Node[] OutputNodes()
    {
        return outputs is null ? [] : outputs;
    }

    private bool ValidateType<T>(params Value[] vals)
    {
        ValueKind expected = typeof(T) == typeof(int)
            ? ValueKind.Int
            : typeof(T) == typeof(bool)
            ? ValueKind.Bool
            : typeof(T) == typeof(string)
            ? ValueKind.String
            : typeof(T) == typeof(string[])
            ? ValueKind.StringArray
            : ValueKind.Object;
        bool valid = vals.All(v => v.Kind == expected || (expected == ValueKind.Object && v.Kind == ValueKind.Null));
        if (!valid) Err($"Arguments must be of type {typeof(T).Name}");
        return valid;
    }

    private void ClearStackReferences()
    {
        for (int i = 0; i < stackTop; i++)
        {
            if (stack[i].IsReference)
                stack[i] = default;
        }
        stackTop = 0;
    }

    private void ClearDeadVariableReferences()
    {
        for (int i = 0; i < variables.Length; i++)
        {
            if (!initVar[i] && variables[i].IsReference)
                variables[i] = default;
        }
    }

    private void ClearAllVariableReferences()
    {
        for (int i = 0; i < variables.Length; i++)
        {
            if (variables[i].IsReference)
                variables[i] = default;
        }
    }

    private void ClearFrameState()
    {
        initVar.SetAll(false);
        initVar[INPUT_VARIABLE_IDX] = true;
        initVar[MEM_VARIABLE_IDX] = true;
        ClearDeadVariableReferences();
        ClearStackReferences();
    }

    private void Err(string message)
    {
        State = NodeState.IDLE;
        panic = true;
        ClearFrameState();
        runtimeError.Invoke(GetLine(nextInstruction - 1), message);
    }

    public override string ToString() => "RegularNode";
}