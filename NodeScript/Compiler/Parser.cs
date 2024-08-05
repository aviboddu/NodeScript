namespace NodeScript;

using static CompilerUtils;

public class Parser(Token[][] tokens, CompileErrorHandler errorHandler)
{
    private readonly CompileErrorHandler errorHandler = errorHandler;
    private readonly Token[][] tokens = tokens;
    private int currentLine = 0;
    private int currentToken = 0;

    private Operation[] operations = new Operation[tokens.Length];

    public Operation[] Parse()
    {
        throw new NotImplementedException();
    }

    public bool Validate()
    {
        return true;
    }
}