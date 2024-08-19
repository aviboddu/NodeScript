namespace NodeScript;

using static CompilerUtils;

public static class NodeFactory
{
    public static InputNode CreateInputNode(string inputData) => new(inputData);
    public static CombinerNode CreateCombinerNode(Node? output = null) => new(output);
    public static OutputNode CreateOutputNode() => new();
    public static RegularNode? CreateRegularNode(string source, CompileErrorHandler compileError, ErrorHandler runtimeError, Node[]? outputs = null)
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

        // Optimize
        Optimizer.PropogateConstants(operations, compileError);
        if (hasError) return null;

        // Validate
        Validator.Validate(operations, compileError);
        if (hasError) return null;

        // Compile
        Compiler compiler = new(operations, compileError);
        (byte[] code, object[] constants, int[] lines) = compiler.Compile();
        if (hasError) return null;

        RegularNode regularNode = new(code, constants, lines, runtimeError, outputs);
        return regularNode;
    }
}