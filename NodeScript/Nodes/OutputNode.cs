using System.Text;

namespace NodeScript;

public class OutputNode : Node
{
    private readonly StringBuilder outputBuilder = new();
    public string output => outputBuilder.ToString();

    public override bool PushInput(string input)
    {
        outputBuilder.AppendLine(input);
        return true;
    }

    public override void StepLine()
    {
        return;
    }
}