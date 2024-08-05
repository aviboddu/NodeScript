namespace NodeScript;

public enum OpCode : byte
{
    CONSTANT,
    TRUE,
    FALSE,
    POP,
    GET,
    SET,
    EQUAL,
    GREATER,
    LESS,
    ADD,
    SUBTRACT,
    MULTIPLY,
    DIVIDE,
    NEGATE,
    PRINT,
    NOT,
    JUMP,
    JUMP_IF_FALSE,
    AND,
    OR,
    CALL,
    RETURN
}