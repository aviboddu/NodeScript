namespace NodeScript;

using static CompilerUtils;

public static class NodeFactory
{
    public static RegularNode? CreateRegularNode(string source, Node[] outputNodes, CompileErrorHandler compileError, ErrorHandler runtimeError)
    {
        bool hasError = false;
        compileError += (int _, string _) => hasError = true;

        // Tokenize
        Tokenizer tokenizer = new(source, compileError);
        Token[][] tokens = tokenizer.ScanTokens();
        if (hasError) return null;

        // Parse
        Parser parser = new(tokens, compileError);
        Operation?[] operations = parser.Parse();
        if (hasError) return null;

        // Validate
        Validator.Validate(operations, compileError);
        if (hasError) return null;

        // Optimize
        Optimizer.Optimize(operations, compileError);
        if (hasError) return null;

        // Compile
        Compiler compiler = new(operations, compileError);
        (byte[] code, object[] constants, int[] lines) = compiler.Compile();
        if (hasError) return null;

        RegularNode regularNode = new(code, constants, outputNodes, lines, runtimeError);
        return regularNode;
    }
}