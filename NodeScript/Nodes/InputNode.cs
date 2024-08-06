namespace NodeScript;

public class InputNode(string input, Node output) : Node
{
    private readonly StringReader input = new(input);
    private readonly Node output = output;
    private string? currentLine = null;

    public override bool PushInput(string input)
    {
        return false;
    }

    public override void StepLine()
    {
        if (input.Peek() == -1) return;
        currentLine ??= input.ReadLine();
        if (output.PushInput(currentLine!))
            currentLine = null;
    }
}