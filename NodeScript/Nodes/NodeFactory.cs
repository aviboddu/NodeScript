namespace NodeScript;

using static CompilerUtils;

public static class NodeFactory
{
    public static RegularNode? CreateRegularNode(string source, CompileErrorHandler compileError, ErrorHandler runtimeError)
    {
        bool hasError = false;
        compileError += (int _, string _) => hasError = true;

        // Tokenize
        Tokenizer tokenizer = new(source, compileError);
        Token[][] tokens = tokenizer.ScanTokens();
        if (hasError) return null;

        // Parse
        Parser parser = new(tokens, compileError);
        Operation[] operations = parser.Parse();
        if (hasError) return null;

        // Validate
        parser.Validate();
        if (hasError) return null;

        // Compile
        Compiler compiler = new(operations, compileError);
        (byte[][] code, object[] constants) = compiler.Compile();
        if (hasError) return null;

        // Reformat
        int totalLength = code.Length;
        foreach (byte[] line in code)
            totalLength += line.Length;
        byte[] byteCode = new byte[totalLength];
        int[] lines = new int[totalLength];

        int i = 0;
        for (int lineNo = 0; lineNo < code.Length; lineNo++)
        {
            Array.Copy(code[lineNo], 0, byteCode, i, code[lineNo].Length);
            Array.Fill(lines, lineNo, i, code[lineNo].Length + 1);
            byteCode[i + code[lineNo].Length] = (byte)OpCode.LINE_END;
            i += code[i].Length + 1;
        }

        RegularNode regularNode = new(byteCode, constants, [], lines, runtimeError);
        return regularNode;
    }
}