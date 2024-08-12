using System.Diagnostics;

namespace NodeScript;

[DebuggerDisplay("stringReader = {input}")]
public class InputNode : Node
{
    private readonly string inputData;
    private StringReader input;
    private readonly Node output;
    private string? currentLine = null;

    public InputNode(string input, Node output)
    {
        inputData = input;
        this.input = new(input);
        this.output = output;
        State = NodeState.RUNNING;
    }

    public void Reset()
    {
        input = new(inputData);
        currentLine = null;
        State = NodeState.RUNNING;
    }

    public override bool PushInput(string input)
    {
        return false;
    }

    public override void StepLine()
    {
        if (State != NodeState.RUNNING) return;
        if (input.Peek() == -1 && currentLine is null)
        {
            State = NodeState.IDLE;
            return;
        }
        currentLine ??= input.ReadLine();
        if (output.PushInput(currentLine!))
            currentLine = null;
    }
}