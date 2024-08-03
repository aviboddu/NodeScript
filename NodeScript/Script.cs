namespace NodeScript;

public class Script
{
    public static Script? script;
    public void Error(Node node, int line, int column, string message) { }

    public void RuntimeError(Node node, int line, int column, string message) { }
}