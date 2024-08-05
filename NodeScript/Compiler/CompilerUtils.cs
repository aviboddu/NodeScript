namespace NodeScript;

using static TokenType;

public static class CompilerUtils
{
    public delegate void CompileErrorHandler(int line, string message);
}