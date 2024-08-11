namespace NodeScript;

public static class CompilerUtils
{
    public delegate void CompileErrorHandler(int line, string message);

    public static bool IsType(Type testType, Type compareType) => testType == compareType || testType == typeof(object);
}