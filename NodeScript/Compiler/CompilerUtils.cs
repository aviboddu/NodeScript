namespace NodeScript;

internal static class CompilerUtils
{
    public const int INPUT_VARIABLE_IDX = 0;
    public const int MEM_VARIABLE_IDX = 1;

    public delegate void InternalErrorHandler(int line, string message);

    public static bool IsTypeOrObj(Type testType, Type compareType) => testType == compareType || testType == typeof(object);

}