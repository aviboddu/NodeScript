namespace NodeScript;

using static CompilerUtils;

public class Expression(OpCode opCode)
{
    public readonly OpCode opCode = opCode;
    public Expression[] arguments = new Expression[ExpectedNumExpressions(opCode)];
}