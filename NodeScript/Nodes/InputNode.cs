using System.Diagnostics;

namespace NodeScript;

[DebuggerDisplay("nextLine = {nextLine}")]
internal class InputNode : Node
{
    public Node? output;

    private readonly string[] inputLines;
    private int nextLine = -1;

    public InputNode(string input, Node? output = null)
    {
        char[] splitters = ['\n', '\r'];
        inputLines = input.Split(splitters, options: StringSplitOptions.RemoveEmptyEntries);
        this.output = output;
        nextLine = 0;
        State = NodeState.RUNNING;
    }

    public override void Reset()
    {
        nextLine = 0;
        State = NodeState.RUNNING;
    }

    public override bool PushInput(string input)
    {
        return false;
    }

    public override void StepLine()
    {
        if (State != NodeState.RUNNING) return;
        if (nextLine >= inputLines.Length)
        {
            State = NodeState.IDLE;
            return;
        }
        if (output?.PushInput(inputLines[nextLine]) ?? false)
            nextLine++;
    }

    public override Node[] OutputNodes()
    {
        return output is null ? [] : [output];
    }

    public override string ToString()
    {
        return "InputNode";
    }
}