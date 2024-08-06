using System.Text;

namespace NodeScript;

public class OutputNode : Node
{
    private readonly StringBuilder output = new();
    public override bool PushInput(string input)
    {
        output.AppendLine(input);
        return true;
    }

    public override void StepLine()
    {
        return;
    }
}