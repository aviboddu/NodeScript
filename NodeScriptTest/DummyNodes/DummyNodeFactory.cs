using NodeScript;

namespace NodeScriptTest;

public static class DummyNodeFactory
{
    public static InputNode CreateInputNode(string filePath, Node output)
    {
        string input = File.ReadAllText(filePath);
        return new(input, output);
    }

    public static OutputNode CreateOutputNode()
    {
        return new();
    }

    public static RegularNode? CreateRegularNode(string filePath, Node[] outputs, CompilerUtils.CompileErrorHandler compileErrorHandler, ErrorHandler runtimeErrorHandler)
    {
        string code = File.ReadAllText(filePath);
        return NodeFactory.CreateRegularNode(code, outputs, compileErrorHandler, runtimeErrorHandler);
    }

    public static (InputNode, RegularNode, OutputNode) CreateNodePath(string inputFilePath, string codePath, CompilerUtils.CompileErrorHandler compileErrorHandler, ErrorHandler runtimeErrorHandler)
    {
        OutputNode outputNode = CreateOutputNode();
        RegularNode regularNode = CreateRegularNode(codePath, [outputNode], compileErrorHandler, runtimeErrorHandler) ?? throw new ArgumentException("Failed to create regular node");
        InputNode inputNode = CreateInputNode(inputFilePath, regularNode);
        return (inputNode, regularNode, outputNode);
    }
}