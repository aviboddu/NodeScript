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
    NOT_EQUAL,
    GREATER,
    GREATERI,
    GREATER_EQUAL,
    GREATER_EQUALI,
    LESS,
    LESSI,
    LESS_EQUAL,
    LESS_EQUALI,
    ADD,
    ADDI,
    ADDS,
    ADDA,
    SUBTRACT,
    SUBTRACTI,
    MULTIPLY,
    MULTIPLYI,
    DIVIDE,
    DIVIDEI,
    AND,
    ANDB,
    OR,
    ORB,
    NEGATE,
    NEGATEI,
    NOT,
    NOTB,
    PRINT,
    PRINTIS,
    JUMP,
    JUMP_IF_FALSE,
    CALL,
    CALL_TYPE_KNOWN,
    RETURN,
    LINE_END,
    NOP
}