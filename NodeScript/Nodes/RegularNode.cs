using System.Collections.Immutable;

namespace NodeScript;

public class RegularNode : Node
{
    private static readonly ImmutableHashSet<string> globalVars = ["input", "mem"];
    private Node[] outputs;
    private string? input;

    private int nextLine = 0;
    private Stack<object> stack = new();
    private Dictionary<string, object> variables = [];

    public RegularNode(string code, Node[] outputs)
    {
        this.outputs = outputs;
        Tokenizer tokenizer = new(code, this);
        Token[] tokens = tokenizer.ScanTokens();
    }

    private void InitGlobals()
    {
        foreach (string s in globalVars)
        {
            variables[s] = string.Empty;
        }
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
}