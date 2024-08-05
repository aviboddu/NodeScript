using System.Collections.Immutable;

namespace NodeScript;

public class RegularNode : Node
{
    private static readonly ImmutableHashSet<string> globalVars = ["input", "mem"];

    public readonly string code;
    private readonly Node[] outputs;
    private readonly ErrorHandler compileError;
    private readonly ErrorHandler runtimeError;

    private string input = string.Empty;
    private int nextLine = 0;
    private readonly Stack<object> stack = new();
    private readonly Dictionary<string, object> variables = [];

    public RegularNode(string code, Node[] outputs, ErrorHandler compileError, ErrorHandler runtimeError)
    {
        this.code = code;
        this.outputs = outputs;
        this.compileError = compileError;
        this.runtimeError = runtimeError;

        InitGlobals();
        Tokenizer tokenizer = new(code, CompileError);
        Token[][] tokens = tokenizer.ScanTokens();
    }

    private void InitGlobals()
    {
        foreach (string s in globalVars)
            variables[s] = string.Empty;

        foreach (string s in NativeFuncs.NativeFunctions.Keys)
            variables[s] = NativeFuncs.NativeFunctions[s];
    }

    public override bool PushInput(string input)
    {
        if (State == NodeState.IDLE)
        {
            this.input = input;
            variables["input"] = input;
            nextLine = 0;
            State = NodeState.RUNNING;
            return true;
        }
        return false;
    }

    public override void Step()
    {

    }

    private void CompileError(int line, string message) => compileError.Invoke(this, line, message);
}