namespace NodeScript;

public abstract class Node
{
    public NodeState State { get; protected set; }

    // Pushes an input to this node. Returns a boolean indicating whether the push was successful.
    public abstract bool PushInput(string input);

    // Steps through the code in this node line-by-line.
    public abstract void Step();
}

public enum NodeState
{
    RUNNING, IDLE, BLOCKED
}
