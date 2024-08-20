using System.Diagnostics;

namespace NodeScript;

[DebuggerDisplay("input = {input, nq}")]
public class CombinerNode(Node? output) : Node
{
    public Node? output = output;
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
        if (output?.PushInput(input!) ?? false) input = null;
    }

    public override void Reset()
    {
        input = null;
    }

    public override Node[] OutputNodes()
    {
        return output is null ? [] : [output];
    }

    public override string ToString()
    {
        return "CombinerNode";
    }
}