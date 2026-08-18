namespace NodeScript;

using static OpCode;

internal readonly record struct PlannedInstruction(OpCode OpCode, byte Size, int Operand, int Target, int Offset)
{
    public int ConstantIndex => Operand & byte.MaxValue;
    public int ArgumentCount => Operand >> 8;
    public ValueKind ConstantKind => (ValueKind)(Operand >> 8);
}

internal sealed class ExecutionPlan
{
    private readonly PlannedInstruction[] instructions;
    private readonly Value[] constants;
    public int MaximumStackDepth { get; }
    public int Length => instructions.Length;
    public ref readonly PlannedInstruction this[int index] => ref instructions[index];

    private ExecutionPlan(PlannedInstruction[] instructions, Value[] constants, int maximumStackDepth)
    {
        this.instructions = instructions;
        this.constants = constants;
        MaximumStackDepth = maximumStackDepth;
    }

    public Value GetConstant(int index) => constants[index];
    public NativeDelegate GetFunction(int index) => (NativeDelegate)constants[index].AsObject()!;

    public static bool TryCreate(Compiler.CompiledData data, out ExecutionPlan? plan, out string? error)
    {
        int[] instructionAtOffset = new int[data.Code.Length];
        Array.Fill(instructionAtOffset, -1);
        int instructionCount = 0;
        int offset = 0;
        while (offset < data.Code.Length)
        {
            OpCode opCode = (OpCode)data.Code[offset];
            int width = OperandWidth(opCode);
            if (width < 0 || offset + width >= data.Code.Length)
            {
                plan = null;
                error = $"Invalid {opCode} instruction at byte {offset}";
                return false;
            }

            int operand = width switch
            {
                0 => 0,
                1 => data.Code[offset + 1],
                _ => data.Code[offset + 1] | data.Code[offset + 2] << 8,
            };
            int constantIndex = opCode is CALL or CALL_TYPE_KNOWN ? data.Code[offset + 1] : operand;
            if (opCode is CALL or CALL_TYPE_KNOWN)
                operand = data.Code[offset + 2];
            if (!ValidateOperand(opCode, constantIndex, data, out error))
            {
                plan = null;
                return false;
            }

            instructionAtOffset[offset] = instructionCount++;
            offset += width + 1;
        }

        PlannedInstruction[] instructions = new PlannedInstruction[instructionCount];
        offset = 0;
        for (int i = 0; i < instructions.Length; i++)
        {
            OpCode opCode = (OpCode)data.Code[offset];
            int width = OperandWidth(opCode);
            int operand = width switch
            {
                0 => 0,
                1 => data.Code[offset + 1],
                _ => data.Code[offset + 1] | data.Code[offset + 2] << 8,
            };
            if (opCode is CALL or CALL_TYPE_KNOWN)
                operand = data.Code[offset + 1] | data.Code[offset + 2] << 8;
            else if (opCode == CONSTANT)
                operand |= (int)Value.FromObject(data.Constants[operand]).Kind << 8;

            int target = -1;
            if (opCode is JUMP or JUMP_IF_FALSE)
            {
                int targetOffset = offset + width + 1 + operand;
                if ((uint)targetOffset >= (uint)instructionAtOffset.Length || instructionAtOffset[targetOffset] < 0)
                {
                    plan = null;
                    error = $"Invalid branch target at byte {offset}";
                    return false;
                }
                target = instructionAtOffset[targetOffset];
            }
            instructions[i] = new(opCode, (byte)(width + 1), operand, target, offset);
            offset += width + 1;
        }

        if (!ValidateControlFlow(instructions, out int maximumStackDepth, out error))
        {
            plan = null;
            return false;
        }

        Value[] constants = Array.ConvertAll(data.Constants, Value.FromObject);
        plan = new(instructions, constants, maximumStackDepth);
        error = null;
        return true;
    }

    private static int OperandWidth(OpCode opCode) => opCode switch
    {
        CONSTANT => 1,
        CALL or CALL_TYPE_KNOWN => 2,
        GET or SET or JUMP or JUMP_IF_FALSE => 2,
        TRUE or FALSE or POP or EQUAL or NOT_EQUAL or GREATER or GREATERI or GREATER_EQUAL or GREATER_EQUALI
            or LESS or LESSI or LESS_EQUAL or LESS_EQUALI or ADD or ADDI or ADDS or ADDA or SUBTRACT or SUBTRACTI
            or MULTIPLY or MULTIPLYI or DIVIDE or DIVIDEI or AND or ANDB or OR or ORB or NEGATE or NEGATEI or NOT
            or NOTB or PRINT or PRINTIS or RETURN or ENDIF or NOP => 0,
        _ => -1,
    };

    private static bool ValidateOperand(OpCode opCode, int operand, Compiler.CompiledData data, out string? error)
    {
        bool valid = opCode switch
        {
            CONSTANT => operand < data.Constants.Length,
            GET or SET => operand < data.NumVariables,
            CALL or CALL_TYPE_KNOWN => operand < data.Constants.Length && data.Constants[operand] is NativeDelegate,
            _ => true,
        };
        error = valid ? null : $"Invalid operand for {opCode}";
        return valid;
    }

    private static bool ValidateControlFlow(PlannedInstruction[] instructions, out int maximumStackDepth, out string? error)
    {
        if (instructions.Length == 0)
        {
            maximumStackDepth = 0;
            error = "Execution plan is empty";
            return false;
        }

        int[] predecessorCounts = new int[instructions.Length];
        for (int i = 0; i < instructions.Length; i++)
        {
            PlannedInstruction instruction = instructions[i];
            if (instruction.OpCode is JUMP or JUMP_IF_FALSE)
                predecessorCounts[instruction.Target]++;
            if (instruction.OpCode is not (RETURN or JUMP) && i + 1 < instructions.Length)
                predecessorCounts[i + 1]++;
        }

        int[] heights = new int[instructions.Length];
        Array.Fill(heights, -1);
        ValueKind?[][] mergeTypes = new ValueKind?[instructions.Length][];
        byte[] states = new byte[instructions.Length];
        int maxDepth = 0;
        string? validationError = null;

        bool Visit(int index, int height, List<ValueKind?> stack)
        {
            if (states[index] == 1)
            {
                validationError = $"Control-flow cycle at byte {instructions[index].Offset}";
                return false;
            }
            if (heights[index] >= 0)
            {
                int existing = heights[index];
                if (existing != height)
                    return Fail($"Incompatible stack heights at byte {instructions[index].Offset}");
                ValueKind?[] existingTypes = mergeTypes[index];
                for (int i = 0; i < existingTypes.Length; i++)
                {
                    if (existingTypes[i] is not null && stack[i] is not null && existingTypes[i] != stack[i])
                        return Fail($"Incompatible stack types at byte {instructions[index].Offset}");
                }
                return true;
            }

            heights[index] = height;
            if (predecessorCounts[index] > 1)
                mergeTypes[index] = [.. stack];
            states[index] = 1;
            PlannedInstruction instruction = instructions[index];
            (int required, int delta) = StackEffect(instruction);
            if (height < required)
                return Fail($"Stack underflow at byte {instruction.Offset}");

            if (!ApplyTypes(instruction, stack, Fail))
                return false;
            int nextHeight = height + delta;
            maxDepth = Math.Max(maxDepth, nextHeight);

            bool valid = true;
            if (instruction.OpCode == JUMP)
            {
                valid = Visit(instruction.Target, nextHeight, stack);
            }
            else if (instruction.OpCode == JUMP_IF_FALSE)
            {
                List<ValueKind?> branchStack = [.. stack];
                valid = Visit(instruction.Target, nextHeight, branchStack);
                if (valid && index + 1 < instructions.Length)
                    valid = Visit(index + 1, nextHeight, stack);
            }
            else if (instruction.OpCode != RETURN && index + 1 < instructions.Length)
            {
                valid = Visit(index + 1, nextHeight, stack);
            }

            states[index] = 2;
            return valid;
        }

        bool Fail(string message)
        {
            validationError = message;
            return false;
        }

        bool valid = Visit(0, 0, []);
        maximumStackDepth = maxDepth;
        error = validationError;
        return valid;
    }

    private static bool ApplyTypes(PlannedInstruction instruction, List<ValueKind?> stack, Func<string, bool> fail)
    {
        bool Pop(ValueKind? expected = null)
        {
            ValueKind? actual = stack[^1];
            stack.RemoveAt(stack.Count - 1);
            return expected is null || actual is null || actual == expected
                || fail($"Invalid stack type for {instruction.OpCode} at byte {instruction.Offset}");
        }
        bool Pop2(ValueKind? expected = null) => Pop(expected) && Pop(expected);
        void Push(ValueKind? kind) => stack.Add(kind);

        switch (instruction.OpCode)
        {
            case CONSTANT: Push(instruction.ConstantKind); break;
            case TRUE:
            case FALSE: Push(ValueKind.Bool); break;
            case GET: Push(null); break;
            case POP:
            case SET:
            case JUMP_IF_FALSE: return Pop();
            case PRINTIS: return Pop(ValueKind.String) && Pop(ValueKind.Int);
            case PRINT: return Pop2();
            case ADDI:
            case SUBTRACTI:
            case MULTIPLYI:
            case DIVIDEI: if (!Pop2(ValueKind.Int)) return false; Push(ValueKind.Int); break;
            case ADDS: if (!Pop2(ValueKind.String)) return false; Push(ValueKind.String); break;
            case ADDA: if (!Pop2(ValueKind.StringArray)) return false; Push(ValueKind.StringArray); break;
            case ANDB:
            case ORB: if (!Pop2(ValueKind.Bool)) return false; Push(ValueKind.Bool); break;
            case GREATERI:
            case GREATER_EQUALI:
            case LESSI:
            case LESS_EQUALI: if (!Pop2(ValueKind.Int)) return false; Push(ValueKind.Bool); break;
            case NEGATEI: if (!Pop(ValueKind.Int)) return false; Push(ValueKind.Int); break;
            case NOTB: if (!Pop(ValueKind.Bool)) return false; Push(ValueKind.Bool); break;
            case CALL:
            case CALL_TYPE_KNOWN:
                for (int i = 0; i < instruction.ArgumentCount; i++)
                    if (!Pop()) return false;
                Push(null);
                break;
            case EQUAL:
            case NOT_EQUAL:
                if (!Pop2()) return false;
                Push(ValueKind.Bool);
                break;
            case GREATER:
            case GREATER_EQUAL:
            case LESS:
            case LESS_EQUAL:
                if (!Pop2(ValueKind.Int)) return false;
                Push(ValueKind.Bool);
                break;
            case SUBTRACT:
            case MULTIPLY:
            case DIVIDE:
                if (!Pop2(ValueKind.Int)) return false;
                Push(ValueKind.Int);
                break;
            case ADD:
                if (!Pop2()) return false;
                Push(null);
                break;
            case AND:
            case OR:
                if (!Pop2(ValueKind.Bool)) return false;
                Push(ValueKind.Bool);
                break;
            case NEGATE:
                if (!Pop(ValueKind.Int)) return false;
                Push(ValueKind.Int);
                break;
            case NOT:
                if (!Pop(ValueKind.Bool)) return false;
                Push(ValueKind.Bool);
                break;
        }
        return true;
    }

    private static (int Required, int Delta) StackEffect(PlannedInstruction instruction) => instruction.OpCode switch
    {
        CONSTANT or TRUE or FALSE or GET => (0, 1),
        POP or SET or JUMP_IF_FALSE => (1, -1),
        EQUAL or NOT_EQUAL or GREATER or GREATERI or GREATER_EQUAL or GREATER_EQUALI or LESS or LESSI or LESS_EQUAL
            or LESS_EQUALI or ADD or ADDI or ADDS or ADDA or SUBTRACT or SUBTRACTI or MULTIPLY or MULTIPLYI or DIVIDE
            or DIVIDEI or AND or ANDB or OR or ORB => (2, -1),
        NEGATE or NEGATEI or NOT or NOTB => (1, 0),
        PRINT or PRINTIS => (2, -2),
        CALL or CALL_TYPE_KNOWN => (instruction.ArgumentCount, 1 - instruction.ArgumentCount),
        _ => (0, 0),
    };
}
