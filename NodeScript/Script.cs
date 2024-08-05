namespace NodeScript;

public class Script
{
    public void Error(Node node, int line, string message) { }
    public void RuntimeError(Node node, int line, string message) { }
}

public delegate void ErrorHandler(Node node, int line, string message);