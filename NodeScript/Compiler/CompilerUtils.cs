namespace NodeScript;

using static OpCode;

public static class CompilerUtils
{
    public delegate void CompileErrorHandler(int line, string message);

    public static int ExpectedNumExpressions(OpCode opCode)
    {
        switch (opCode)
        {
            case TRUE:
            case FALSE:
            case POP:
            case JUMP:
            case RETURN: return 0;
            case CONSTANT:
            case GET:
            case NOT:
            case NEGATE:
            case JUMP_IF_FALSE: return 1;
            case SET:
            case MULTIPLY:
            case ADD:
            case SUBTRACT:
            case DIVIDE:
            case AND:
            case OR:
            case EQUAL:
            case GREATER:
            case LESS:
            case PRINT: return 2;
            case CALL:
            default: return -1;
        }
    }

    public static Type[][] AllowedTypes(OpCode opCode)
    {
        if (ExpectedNumExpressions(opCode) <= 0) return [];
        switch (opCode)
        {
            case CONSTANT: return [[typeof(string)]];
            case GET: return [[typeof(string)]];
            case NOT: return [[typeof(bool)]];
            case NEGATE: return [[typeof(int)]];
            case JUMP: return [[typeof(int)]];
            case JUMP_IF_FALSE: return [[typeof(int)], [typeof(bool)]];
            case SET: return [[typeof(string)], [typeof(string), typeof(bool), typeof(int), typeof(string[])]];
            case GREATER:
            case LESS:
            case MULTIPLY:
            case SUBTRACT:
            case DIVIDE: return [[typeof(int)], [typeof(int)]];
            case EQUAL:
            case ADD: return [[typeof(int), typeof(string), typeof(string[])], [typeof(int), typeof(string), typeof(string[])]];
            case AND:
            case OR: return [[typeof(bool)], [typeof(bool)]];
            case PRINT: return [[typeof(int)], [typeof(string)]];
            default: return [];
        }
    }

    public static Type[] ProducedTypes(OpCode opCode)
    {
        switch (opCode)
        {
            case AND:
            case OR:
            case EQUAL:
            case GREATER:
            case LESS:
            case NOT:
            case TRUE:
            case FALSE: return [typeof(bool)];
            case CONSTANT: return [typeof(int), typeof(string), typeof(string[])];
            case GET: return [typeof(bool), typeof(int), typeof(string), typeof(string[])];
            case NEGATE:
            case MULTIPLY:
            case SUBTRACT:
            case DIVIDE: return [typeof(int)];
            case ADD: return [typeof(int), typeof(string), typeof(string[])];
            default: return [];
        }
    }
}