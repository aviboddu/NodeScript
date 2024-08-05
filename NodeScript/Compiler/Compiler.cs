namespace NodeScript;

using static CompilerUtils;

public class Compiler(Operation[] operations, CompileErrorHandler errorHandler)
{
    private readonly Operation[] operations = operations;
    private readonly CompileErrorHandler errorHandler = errorHandler;
    private int currentLine = 0;
    private byte[][] byteCode = new byte[operations.Length][];

    public (byte[][] code, object[] constants) Compile()
    {
        throw new NotImplementedException();
    }

}