namespace NodeScript;

public class CombinerNode(Node output) : Node
{
    private readonly Node output = output;
    private string? input = null;

    public override bool PushInput(string input)
    {
        if (this.input is null)
        {
            this.input = input;
            return true;
        }
        return false;
    }

    public override void StepLine()
    {
        if (input is null) return;
        if (output.PushInput(input!)) input = null;
    }
}