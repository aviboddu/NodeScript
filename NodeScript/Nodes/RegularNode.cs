namespace NodeScript;

using static OpCode;
public class RegularNode : Node
{
    private Node[] outputs;
    private string? input;
    private readonly Chunk? chunk;
    private int nextOp = 0;
    private Stack<object> stack = new();
    private Stack<Dictionary<string, object>> variables = [];


    public RegularNode(string code, Node[] outputs)
    {
        this.outputs = outputs;
        variables.Push([]);
        Tokenizer tokenizer = new(code, this);
        Token[] tokens = tokenizer.ScanTokens();
        Compiler compiler = new(tokens, this);
        chunk = compiler.Compile();
    }
    public override void PushInput(string input)
    {
        throw new NotImplementedException();
    }

    public override void Run()
    {
        if (chunk is null) return;
        while (nextOp < chunk.operations.Count)
        {
            if (Step()) return;
        }
        throw new NotImplementedException();
    }

    public override bool Step()
    {
        string s;
        int offset;
        Operation op = ReadOperation();
        switch (op.opCode)
        {
            case NEGATE:
                object val = stack.Pop();
                if (val is int num)
                {
                    stack.Push(-num);
                    return false;
                }
                else
                {
                    RuntimeError("Cannot negate non-number value");
                    return true;
                }
            case PRINT:
                object printValue = stack.Pop();
                if (printValue is not string)
                {
                    RuntimeError("print(a,b): b must be a string");
                    return true;
                }
                object printIdx = stack.Pop();
                if (printIdx is not int)
                {
                    RuntimeError("print(a,b): a must be an integer");
                    return true;
                }
                s = (string)printValue;
                int idx = (int)printIdx;
                outputs[idx].PushInput(s);
                break;
            case POP:
                stack.Pop();
                break;
            case DEFINE_VARIABLE:
                s = (string)ReadConstant(op);
                variables.Peek().Add(s, stack.Pop());
                break;
            case GET_VARIABLE:
                s = (string)ReadConstant(op);
                foreach (Dictionary<string, object> dict in variables)
                {
                    if (dict.TryGetValue(s, out object? value))
                    {
                        stack.Push(value);
                        return false;
                    }
                }
                RuntimeError($"Undefined variable '{s}'.");
                return true;
            case SET_VARIABLE:
                s = (string)ReadConstant(op);
                if (s == nameof(input))
                {
                    RuntimeError($"Cannot set {nameof(input)}");
                    return true;
                }
                foreach (Dictionary<string, object> dict in variables)
                {
                    if (dict.ContainsKey(s))
                    {
                        dict[s] = stack.Peek();
                        return false;
                    }
                }
                RuntimeError($"Undefined variable '{s}'.");
                return true;
            case BEGIN_SCOPE:
                variables.Push([]);
                break;
            case END_SCOPE:
                variables.Pop();
                break;
            case JUMP_IF_FALSE:
                offset = (op.data[0] << 8) | op.data[1];
                if (IsFalsey(stack.Peek())) nextOp += offset;
                break;
            case JUMP:
                offset = (op.data[0] << 8) | op.data[1];
                nextOp += offset;
                break;
            case NOT:
                stack.Push(IsFalsey(stack.Pop()));
                break;
            case ADD:
            case SUBTRACT:
            case MULTIPLY:
            case GREATER:
            case LESS:
            case DIVIDE: return BinaryOp(op.opCode);
            case CONSTANT:
                object con = ReadConstant(op);
                stack.Push(con);
                break;
            case TRUE: stack.Push(true); break;
            case FALSE: stack.Push(false); break;
            case EQUAL:
                object b = stack.Pop();
                object a = stack.Pop();
                stack.Push(a.Equals(b));
                break;
            case RETURN: return true;
        }
        return false;
    }

    private static bool IsFalsey(object value)
    {
        return value switch
        {
            bool b => !b,
            int n => n == 0,
            string s => s.Length == 0,
            _ => false,
        };
    }

    private bool BinaryOp(OpCode opCode)
    {
        object val2 = stack.Pop();
        object val1 = stack.Pop();
        if (opCode == ADD && val1 is string s1 && val2 is string s2)
        {
            stack.Push(s1 + s2);
            return false;
        }

        if (val1 is not int || val2 is not int)
        {
            RuntimeError("Both operators must be integers");
            return true;
        }

        int num1 = (int)val1;
        int num2 = (int)val2;
        switch (opCode)
        {
            case ADD:
                stack.Push(num1 + num2); break;
            case SUBTRACT:
                stack.Push(num1 - num2); break;
            case MULTIPLY:
                stack.Push(num1 * num2); break;
            case DIVIDE:
                stack.Push(num1 / num2); break;
            case GREATER:
                stack.Push(num1 > num2); break;
            case LESS:
                stack.Push(num1 < num2); break;
        }
        return false;
    }

    private void RuntimeError(string message)
    {
        Script.script!.RuntimeError(this, chunk?.locations[nextOp - 1].line ?? -1, chunk?.locations[nextOp - 1].column ?? -1, message);
    }

    private Operation ReadOperation() => chunk!.operations[nextOp++];

    private object ReadConstant(Operation op) => chunk!.constants[op.data[0]];
}