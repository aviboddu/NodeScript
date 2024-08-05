namespace NodeScript;

public abstract class Node
{
    public NodeState State { get; protected set; } = NodeState.IDLE;

    // Pushes an input to this node. Returns a boolean indicating whether the push was successful.
    public abstract bool PushInput(string input);

    // Steps through the code in this node line-by-line
    public abstract void StepLine();

    // Steps through the code in this node instruction-by-instruction. Returns true if we've reached the end of the line
    protected abstract bool Step();
}

public enum NodeState
{
    IDLE, BLOCKED, RUNNING
}
