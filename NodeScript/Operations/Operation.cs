namespace NodeScript;

public struct Operation(OpCode opCode, byte[] data)
{
    public readonly OpCode opCode = opCode;
    public byte[] data = data;
}

public enum OpCode
{
    ADD,
    SUBTRACT,
    MULTIPLY,
    DIVIDE,
    NEGATE,
    PRINT,
    POP,
    NOT,
    CONSTANT,
    TRUE,
    FALSE,
    EQUAL,
    GREATER,
    LESS,
    DEFINE_VARIABLE,
    GET_VARIABLE,
    SET_VARIABLE,
    BEGIN_SCOPE,
    END_SCOPE,
    JUMP,
    JUMP_IF_FALSE,
    CALL,
    RETURN
}