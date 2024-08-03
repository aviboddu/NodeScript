namespace NodeScript;

public abstract class Node
{
    // Pushes an input to this node. Blocks until node is able to accept input.
    public abstract void PushInput(string input);

    // Runs the code in this node, reading input and pushing output.
    public abstract void Run();

    // Steps through the code in this node line-by-line.
    public abstract bool Step();
}
