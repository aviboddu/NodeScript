using NodeScript;

namespace NodeScriptTest;

internal static class ScriptTestHelpers
{
    public static Script CreateLinearScript(
        string code,
        string input = "input",
        ErrorHandler? compileError = null,
        ErrorHandler? runtimeError = null)
    {
        Script script = new();
        script.CompileError += compileError;
        script.RuntimeError += runtimeError;
        int inputId = script.AddInputNode(input);
        int nodeId = script.AddRegularNode(code);
        int outputId = script.AddOutputNode();
        script.ConnectNodes(inputId, nodeId);
        script.ConnectNodes(nodeId, outputId);
        return script;
    }
}
