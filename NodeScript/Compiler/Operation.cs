namespace NodeScript;

using static CompilerUtils;

public class Operation(OpCode opCode)
{
    public readonly OpCode opCode = opCode;
    public Expression[] expressions = new Expression[ExpectedNumExpressions(opCode)];
}