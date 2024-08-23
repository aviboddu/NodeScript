namespace NodeScript;

internal abstract class Node
{
    public NodeState State { get; protected set; } = NodeState.IDLE;

    // Pushes an input to this node. Returns a boolean indicating whether the push was successful.
    public abstract bool PushInput(string input);

    // Steps through the code in this node line-by-line
    public abstract void StepLine();

    public abstract void Reset();

    public abstract Node[] OutputNodes();
}

public enum NodeState
{
    IDLE, BLOCKED, RUNNING
}
