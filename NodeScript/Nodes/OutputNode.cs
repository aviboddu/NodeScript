using System.Diagnostics;
using System.Text;

namespace NodeScript;

[DebuggerDisplay("output={output}")]
public class OutputNode : Node
{
    private readonly StringBuilder outputBuilder = new();
    public string Output => outputBuilder.ToString();

    public override bool PushInput(string input)
    {
        outputBuilder.AppendLine(input);
        return true;
    }

    public override void StepLine()
    {
        return;
    }

    public override void Reset() => outputBuilder.Clear();

    public override Node[] OutputNodes()
    {
        return [];
    }

    public override string ToString()
    {
        return "OutputNode";
    }
}