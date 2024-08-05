namespace NodeScript;

using System.Collections.Immutable;
using static OpCode;


public class RegularNode : Node
{
    private static readonly ImmutableHashSet<string> globalVars = ["input", "mem"];

    private readonly Node[] outputs;
    private readonly ErrorHandler runtimeError;

    private readonly byte[] code;
    private readonly object[] constants;
    private readonly Stack<object> stack = new();
    private readonly Dictionary<string, object> variables = [];
    private readonly int[] lines;

    private int nextInstruction = 0;
    private bool panic = false;

    public RegularNode(byte[] code, object[] constants, Node[] outputs, int[] lines, ErrorHandler runtimeError)
    {
        this.code = code;
        this.constants = constants;
        this.outputs = outputs;
        this.runtimeError = runtimeError;
        this.lines = CumulativeInstructionsPerLine(lines);

        InitGlobals();
    }

    private void InitGlobals()
    {
        variables["input"] = string.Empty;
        variables["mem"] = string.Empty;
    }

    private static int[] CumulativeInstructionsPerLine(int[] line)
    {
        int[] result = new int[line[^1]];

        // Frequency Count
        for (int i = 0; i < line.Length; i++)
            result[line[i]]++;

        // Cumulative Frequency
        for (int i = 1; i < result.Length; i++)
            result[i] += result[i - 1];

        return result;
    }

    private int GetLine(int instruction)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            if (instruction < lines[i])
                return i;
        }
        return -1;
    }

    public override bool PushInput(string input)
    {
        if (State == NodeState.IDLE)
        {
            variables["input"] = input;
            nextInstruction = 0;
            State = NodeState.RUNNING;
            return true;
        }
        return false;
    }

    public override void StepLine()
    {
        if (State == NodeState.IDLE) return;
        while (!panic && !Step()) { }
    }

    protected override bool Step()
    {
        string name;
        object v1, v2;
        int num1;
        bool b;
        Span<object> parameters;
        OpCode nextOp = (OpCode)Advance();
        switch (nextOp)
        {
            case CONSTANT: stack.Push(constants[Advance()]); break;
            case TRUE: stack.Push(true); break;
            case FALSE: stack.Push(false); break;
            case POP: stack.Pop(); break;
            case GET:
                name = (string)constants[Advance()];
                stack.Push(variables[name]);
                break;
            case SET:
                name = (string)constants[Advance()];
                v1 = variables[name];
                variables[name] = v1;
                break;
            case EQUAL:
                v2 = stack.Pop();
                v1 = stack.Pop();
                stack.Push(v1.Equals(v2));
                break;
            case GREATER:
            case LESS:
            case SUBTRACT:
            case MULTIPLY:
            case DIVIDE:
                BinaryArithmetic(nextOp);
                break;
            case ADD:
                v2 = stack.Pop();
                v1 = stack.Pop();
                switch (v1)
                {
                    case int n: if (ValidateType<int>(v2)) stack.Push(n + (int)v2); break;
                    case string s: if (ValidateType<string>(v2)) stack.Push(s + (string)v2); break;
                    case string[] a: if (ValidateType<string[]>(v2)) stack.Push(a.Concat((string[])v2)); break;
                    default: Err("Arguments must be either int, string or string[]"); break;
                }
                break;
            case AND:
                v2 = stack.Pop();
                v1 = stack.Pop();
                if (ValidateType<bool>(v1, v2)) stack.Push((bool)v1 && (bool)v2);
                break;
            case OR:
                v2 = stack.Pop();
                v1 = stack.Pop();
                if (ValidateType<bool>(v1, v2)) stack.Push((bool)v1 || (bool)v2);
                break;
            case NEGATE:
                v1 = stack.Pop();
                if (ValidateType<int>(v1)) stack.Push(-(int)v1);
                break;
            case NOT:
                v1 = stack.Pop();
                if (ValidateType<bool>(v1)) stack.Push(!(bool)v1);
                break;
            case PRINT:
                v2 = stack.Pop();
                v1 = stack.Pop();
                if (ValidateType<int>(v1) && ValidateType<string>(v2))
                {
                    if (!outputs[(int)v1].PushInput((string)v2))
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
                return true;
            case JUMP:
                nextInstruction += Advance();
                break;
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

                if (!b)
                    nextInstruction += Advance();
                break;
            case CALL:
                name = (string)constants[Advance()];
                num1 = Advance();
                parameters = stack.GetInternalArray().AsSpan()[^num1..];
                if (!NativeFuncs.NativeFunctions.TryGetValue(name, out NativeFuncs.NativeDelegate? func))
                {
                    Err($"Function {name} does not exist");
                    break;
                }
                v1 = func.Invoke(parameters);
                if (v1 is Err err)
                    Err(err.msg);
                stack.Push(v1);
                break;
            case RETURN:
                State = NodeState.IDLE;
                break;
            case LINE_END:
                return true;
        }
        return false;
    }

    private byte Advance() => code[nextInstruction++];

    private void BinaryArithmetic(OpCode op)
    {
        object val2 = stack.Pop();
        object val1 = stack.Pop();

        if (!ValidateType<int>(val1, val2))
            return;

        int num1 = (int)val1;
        int num2 = (int)val2;
        switch (op)
        {
            case GREATER: stack.Push(num1 > num2); break;
            case LESS: stack.Push(num1 < num2); break;
            case SUBTRACT: stack.Push(num1 - num2); break;
            case MULTIPLY: stack.Push(num1 * num2); break;
            case DIVIDE: stack.Push(num1 / num2); break;
        }
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
        runtimeError.Invoke(this, GetLine(nextInstruction - 1), message);
    }
}