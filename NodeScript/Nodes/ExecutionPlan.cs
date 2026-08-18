namespace NodeScript;

using static OpCode;

internal readonly record struct PlannedInstruction(OpCode OpCode, int Operand, int Target, NativeDelegate? Function, Value Constant, int Offset, int Width);

internal sealed class ExecutionPlan
{
    private readonly PlannedInstruction[] instructions;
    public int MaximumStackDepth { get; }
    public int Length => instructions.Length;
    public PlannedInstruction this[int index] => instructions[index];

    private ExecutionPlan(PlannedInstruction[] instructions, int maximumStackDepth)
    {
        this.instructions = instructions;
        MaximumStackDepth = maximumStackDepth;
    }

    public static bool TryCreate(Compiler.CompiledData data, out ExecutionPlan? plan, out string? error)
    {
        List<PlannedInstruction> instructions = [];
        Dictionary<int, int> instructionAtOffset = [];
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

            NativeDelegate? function = opCode is CALL or CALL_TYPE_KNOWN ? (NativeDelegate)data.Constants[constantIndex] : null;
            Value constant = opCode == CONSTANT ? Value.FromObject(data.Constants[constantIndex]) : default;
            instructionAtOffset.Add(offset, instructions.Count);
            instructions.Add(new(opCode, operand, -1, function, constant, offset, width + 1));
            offset += width + 1;
        }

        for (int i = 0; i < instructions.Count; i++)
        {
            PlannedInstruction instruction = instructions[i];
            if (instruction.OpCode is not (JUMP or JUMP_IF_FALSE))
                continue;

            int targetOffset = instruction.Offset + instruction.Width + instruction.Operand;
            if (!instructionAtOffset.TryGetValue(targetOffset, out int target))
            {
                plan = null;
                error = $"Invalid branch target at byte {instruction.Offset}";
                return false;
            }
            instructions[i] = instruction with { Target = target };
        }

        if (!ValidateControlFlow(instructions, out int maximumStackDepth, out error))
        {
            plan = null;
            return false;
        }

        plan = new([.. instructions], maximumStackDepth);
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

    private static bool ValidateControlFlow(List<PlannedInstruction> instructions, out int maximumStackDepth, out string? error)
    {
        if (instructions.Count == 0)
        {
            maximumStackDepth = 0;
            error = "Execution plan is empty";
            return false;
        }

        int?[] heights = new int?[instructions.Count];
        ValueKind?[][] types = new ValueKind?[instructions.Count][];
        byte[] states = new byte[instructions.Count];
        int maxDepth = 0;
        string? validationError = null;

        bool Visit(int index, int height, List<ValueKind?> stack)
        {
            if (states[index] == 1)
            {
                validationError = $"Control-flow cycle at byte {instructions[index].Offset}";
                return false;
            }
            if (heights[index] is int existing)
            {
                if (existing != height)
                    return Fail($"Incompatible stack heights at byte {instructions[index].Offset}");
                ValueKind?[] existingTypes = types[index];
                return existingTypes.Zip(stack).All(pair => pair.First is null || pair.Second is null || pair.First == pair.Second)
                    || Fail($"Incompatible stack types at byte {instructions[index].Offset}");
            }

            heights[index] = height;
            types[index] = [.. stack];
            states[index] = 1;
            PlannedInstruction instruction = instructions[index];
            (int required, int delta) = StackEffect(instruction);
            if (height < required)
                return Fail($"Stack underflow at byte {instruction.Offset}");

            List<ValueKind?> nextStack = [.. stack];
            if (!ApplyTypes(instruction, nextStack, Fail))
                return false;
            int nextHeight = height + delta;
            maxDepth = Math.Max(maxDepth, nextHeight);
            foreach (int successor in Successors(instructions, index))
            {
                if (!Visit(successor, nextHeight, nextStack))
                    return false;
            }
            states[index] = 2;
            return true;
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
            case CONSTANT: Push(instruction.Constant.Kind); break;
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
                for (int i = 0; i < instruction.Operand; i++)
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

    private static IEnumerable<int> Successors(List<PlannedInstruction> instructions, int index)
    {
        PlannedInstruction instruction = instructions[index];
        if (instruction.OpCode == RETURN)
            yield break;
        if (instruction.OpCode == JUMP)
        {
            yield return instruction.Target;
            yield break;
        }
        if (instruction.OpCode == JUMP_IF_FALSE)
            yield return instruction.Target;
        if (index + 1 < instructions.Count)
            yield return index + 1;
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
        CALL or CALL_TYPE_KNOWN => (instruction.Operand, 1 - instruction.Operand),
        _ => (0, 0),
    };
}
