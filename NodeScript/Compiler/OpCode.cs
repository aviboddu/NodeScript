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
    AND,
    OR,
    NEGATE,
    NOT,
    PRINT,
    JUMP,
    JUMP_IF_FALSE,
    CALL,
    RETURN,
    LINE_END,
    NOP
}