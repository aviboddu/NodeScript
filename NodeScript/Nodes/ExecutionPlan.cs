namespace NodeScript;

using System.Buffers;

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
        byte[] code = data.Code;
        int capacity = Math.Max(code.Length, 1);
        int[] instructionAtOffset = ArrayPool<int>.Shared.Rent(capacity);
        PlannedInstruction[] decoded = ArrayPool<PlannedInstruction>.Shared.Rent(capacity);
        try
        {
            Array.Fill(instructionAtOffset, -1, 0, code.Length);
            int instructionCount = 0;
            int offset = 0;
            while (offset < code.Length)
            {
                OpCode opCode = (OpCode)code[offset];
                int width = OperandWidth(opCode);
                if (width < 0 || offset + width >= code.Length)
                {
                    plan = null;
                    error = $"Invalid {opCode} instruction at byte {offset}";
                    return false;
                }

                int operand = width switch
                {
                    0 => 0,
                    1 => code[offset + 1],
                    _ => code[offset + 1] | code[offset + 2] << 8,
                };
                int constantIndex = opCode is CALL or CALL_TYPE_KNOWN ? code[offset + 1] : operand;
                if (!ValidateOperand(opCode, constantIndex, data, out error))
                {
                    plan = null;
                    return false;
                }

                if (opCode == CONSTANT)
                    operand |= (int)Value.FromObject(data.Constants[operand]).Kind << 8;

                instructionAtOffset[offset] = instructionCount;
                decoded[instructionCount++] = new(opCode, (byte)(width + 1), operand, -1, offset);
                offset += width + 1;
            }

            PlannedInstruction[] instructions = decoded[..instructionCount];
            for (int i = 0; i < instructions.Length; i++)
            {
                PlannedInstruction instruction = instructions[i];
                if (instruction.OpCode is not (JUMP or JUMP_IF_FALSE)) continue;

                int targetOffset = instruction.Offset + instruction.Size + instruction.Operand;
                if ((uint)targetOffset >= (uint)code.Length || instructionAtOffset[targetOffset] < 0)
                {
                    plan = null;
                    error = $"Invalid branch target at byte {instruction.Offset}";
                    return false;
                }
                instructions[i] = instruction with { Target = instructionAtOffset[targetOffset] };
            }

            if (!ValidateControlFlow(instructions, out int maximumStackDepth, out error))
            {
                plan = null;
                return false;
            }

            Value[] constants = new Value[data.Constants.Length];
            for (int i = 0; i < constants.Length; i++)
                constants[i] = Value.FromObject(data.Constants[i]);

            plan = new(instructions, constants, maximumStackDepth);
            error = null;
            return true;
        }
        finally
        {
            ArrayPool<PlannedInstruction>.Shared.Return(decoded);
            ArrayPool<int>.Shared.Return(instructionAtOffset);
        }
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
        maximumStackDepth = 0;
        if (instructions.Length == 0)
        {
            error = "Execution plan is empty";
            return false;
        }

        // The language has no loops, so every branch must jump forward. Rejecting backward branches
        // keeps the control-flow graph acyclic, which lets the stack contract be validated in a
        // single forward pass instead of a recursive traversal.
        for (int i = 0; i < instructions.Length; i++)
        {
            PlannedInstruction instruction = instructions[i];
            if (instruction.OpCode is JUMP or JUMP_IF_FALSE && instruction.Target <= i)
            {
                error = $"Control-flow cycle at byte {instruction.Offset}";
                return false;
            }
        }

        int count = instructions.Length;
        int[] mergeHeights = ArrayPool<int>.Shared.Rent(count);
        ValueKind?[]?[] mergeTypes = ArrayPool<ValueKind?[]?>.Shared.Rent(count);
        ValueKind?[] stack = ArrayPool<ValueKind?>.Shared.Rent(count + 1);
        try
        {
            Array.Clear(mergeTypes, 0, count);
            int height = 0;
            int maxDepth = 0;
            bool reachable = true;

            for (int i = 0; i < count; i++)
            {
                PlannedInstruction instruction = instructions[i];
                ValueKind?[]? incoming = mergeTypes[i];
                if (incoming is not null)
                {
                    if (reachable)
                    {
                        if (!TryMerge(stack, height, incoming, mergeHeights[i], instruction, out error))
                            return false;
                    }
                    else
                    {
                        height = mergeHeights[i];
                        incoming.AsSpan(0, height).CopyTo(stack);
                        reachable = true;
                    }

                    mergeTypes[i] = null;
                    ArrayPool<ValueKind?>.Shared.Return(incoming);
                }

                if (!reachable) continue;

                if (!ApplyTypes(instruction, stack, ref height, out error))
                    return false;
                maxDepth = Math.Max(maxDepth, height);

                if (instruction.OpCode is JUMP or JUMP_IF_FALSE)
                {
                    if (!RecordBranch(instructions, mergeTypes, mergeHeights, instruction.Target, stack, height, out error))
                        return false;
                }

                if (instruction.OpCode is JUMP or RETURN)
                    reachable = false;
            }

            maximumStackDepth = maxDepth;
            error = null;
            return true;
        }
        finally
        {
            for (int i = 0; i < count; i++)
            {
                ValueKind?[]? pending = mergeTypes[i];
                if (pending is null) continue;
                mergeTypes[i] = null;
                ArrayPool<ValueKind?>.Shared.Return(pending);
            }
            ArrayPool<ValueKind?>.Shared.Return(stack);
            ArrayPool<ValueKind?[]?>.Shared.Return(mergeTypes);
            ArrayPool<int>.Shared.Return(mergeHeights);
        }
    }

    /// <summary>
    /// Records the stack state reaching a branch target, merging it with any state recorded by an
    /// earlier predecessor of that target.
    /// </summary>
    private static bool RecordBranch(
        PlannedInstruction[] instructions,
        ValueKind?[]?[] mergeTypes,
        int[] mergeHeights,
        int target,
        ValueKind?[] stack,
        int height,
        out string? error)
    {
        ValueKind?[]? existing = mergeTypes[target];
        if (existing is not null)
            return TryMerge(existing, mergeHeights[target], stack, height, instructions[target], out error);

        ValueKind?[] recorded = ArrayPool<ValueKind?>.Shared.Rent(Math.Max(height, 1));
        stack.AsSpan(0, height).CopyTo(recorded);
        mergeTypes[target] = recorded;
        mergeHeights[target] = height;
        error = null;
        return true;
    }

    /// <summary>
    /// Merges <paramref name="other"/> into <paramref name="destination"/>, keeping only the value
    /// kinds both paths agree on.
    /// </summary>
    private static bool TryMerge(
        ValueKind?[] destination,
        int destinationHeight,
        ValueKind?[] other,
        int otherHeight,
        PlannedInstruction instruction,
        out string? error)
    {
        if (destinationHeight != otherHeight)
        {
            error = $"Incompatible stack heights at byte {instruction.Offset}";
            return false;
        }

        for (int i = 0; i < destinationHeight; i++)
        {
            if (destination[i] == other[i]) continue;
            if (destination[i] is not null && other[i] is not null)
            {
                error = $"Incompatible stack types at byte {instruction.Offset}";
                return false;
            }
            destination[i] = null;
        }

        error = null;
        return true;
    }

    private static bool TryPop(ValueKind?[] stack, ref int height, ValueKind? expected, PlannedInstruction instruction, out string? error)
    {
        if (height == 0)
        {
            error = $"Stack underflow at byte {instruction.Offset}";
            return false;
        }

        ValueKind? actual = stack[--height];
        if (expected is null || actual is null || actual == expected)
        {
            error = null;
            return true;
        }
        error = $"Invalid stack type for {instruction.OpCode} at byte {instruction.Offset}";
        return false;
    }

    private static bool TryPop2(ValueKind?[] stack, ref int height, ValueKind? expected, PlannedInstruction instruction, out string? error)
        => TryPop(stack, ref height, expected, instruction, out error) && TryPop(stack, ref height, expected, instruction, out error);

    private static bool ApplyTypes(PlannedInstruction instruction, ValueKind?[] stack, ref int height, out string? error)
    {
        error = null;
        switch (instruction.OpCode)
        {
            case CONSTANT: stack[height++] = instruction.ConstantKind; break;
            case TRUE:
            case FALSE: stack[height++] = ValueKind.Bool; break;
            case GET: stack[height++] = null; break;
            case POP:
            case SET:
            case JUMP_IF_FALSE: return TryPop(stack, ref height, null, instruction, out error);
            case PRINTIS:
                return TryPop(stack, ref height, ValueKind.String, instruction, out error)
                    && TryPop(stack, ref height, ValueKind.Int, instruction, out error);
            case PRINT: return TryPop2(stack, ref height, null, instruction, out error);
            case ADDI:
            case SUBTRACTI:
            case MULTIPLYI:
            case DIVIDEI: if (!TryPop2(stack, ref height, ValueKind.Int, instruction, out error)) return false; stack[height++] = ValueKind.Int; break;
            case ADDS: if (!TryPop2(stack, ref height, ValueKind.String, instruction, out error)) return false; stack[height++] = ValueKind.String; break;
            case ADDA: if (!TryPop2(stack, ref height, ValueKind.StringArray, instruction, out error)) return false; stack[height++] = ValueKind.StringArray; break;
            case ANDB:
            case ORB: if (!TryPop2(stack, ref height, ValueKind.Bool, instruction, out error)) return false; stack[height++] = ValueKind.Bool; break;
            case GREATERI:
            case GREATER_EQUALI:
            case LESSI:
            case LESS_EQUALI: if (!TryPop2(stack, ref height, ValueKind.Int, instruction, out error)) return false; stack[height++] = ValueKind.Bool; break;
            case NEGATEI: if (!TryPop(stack, ref height, ValueKind.Int, instruction, out error)) return false; stack[height++] = ValueKind.Int; break;
            case NOTB: if (!TryPop(stack, ref height, ValueKind.Bool, instruction, out error)) return false; stack[height++] = ValueKind.Bool; break;
            case CALL:
            case CALL_TYPE_KNOWN:
                for (int i = 0; i < instruction.ArgumentCount; i++)
                    if (!TryPop(stack, ref height, null, instruction, out error)) return false;
                stack[height++] = null;
                break;
            case EQUAL:
            case NOT_EQUAL:
                if (!TryPop2(stack, ref height, null, instruction, out error)) return false;
                stack[height++] = ValueKind.Bool;
                break;
            case GREATER:
            case GREATER_EQUAL:
            case LESS:
            case LESS_EQUAL:
                if (!TryPop2(stack, ref height, ValueKind.Int, instruction, out error)) return false;
                stack[height++] = ValueKind.Bool;
                break;
            case SUBTRACT:
            case MULTIPLY:
            case DIVIDE:
                if (!TryPop2(stack, ref height, ValueKind.Int, instruction, out error)) return false;
                stack[height++] = ValueKind.Int;
                break;
            case ADD:
                if (!TryPop2(stack, ref height, null, instruction, out error)) return false;
                stack[height++] = null;
                break;
            case AND:
            case OR:
                if (!TryPop2(stack, ref height, ValueKind.Bool, instruction, out error)) return false;
                stack[height++] = ValueKind.Bool;
                break;
            case NEGATE:
                if (!TryPop(stack, ref height, ValueKind.Int, instruction, out error)) return false;
                stack[height++] = ValueKind.Int;
                break;
            case NOT:
                if (!TryPop(stack, ref height, ValueKind.Bool, instruction, out error)) return false;
                stack[height++] = ValueKind.Bool;
                break;
        }
        return true;
    }
}
