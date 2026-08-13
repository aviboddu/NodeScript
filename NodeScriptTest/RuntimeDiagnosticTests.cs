using NodeScript;

namespace NodeScriptTest;

[TestClass]
public class RuntimeDiagnosticTests
{
    [DataTestMethod]
    [DataRow("SET zero, parse_int(input)\nSET value, 1 / zero", "0", "divide by 0")]
    [DataRow("SET value, element_at(input, 9)", "abc", "index out of bounds")]
    [DataRow("SET value, slice(input, 0, 9)", "abc", "is only length")]
    [DataRow("SET value, parse_int(input)", "abc", "Failed to parse int")]
    [DataRow("PRINT 1, input", "abc", "Output index 1")]
    [DataRow("PRINT -1, input", "abc", "Output index -1")]
    public void RuntimeFailuresUseTheCallback(string code, string input, string expectedMessage)
    {
        List<(int Node, int Line, string Message)> diagnostics = [];
        Script script = ScriptTestHelpers.CreateLinearScript(
            code,
            input,
            runtimeError: (node, line, message) => diagnostics.Add((node, line, message)));

        Assert.IsTrue(script.CompileNodes());
        script.Run();

        Assert.AreEqual(1, diagnostics.Count);
        Assert.AreEqual(1, diagnostics[0].Node);
        Assert.IsTrue(diagnostics[0].Line >= 0);
        Assert.IsTrue(diagnostics[0].Message.Contains(expectedMessage, StringComparison.OrdinalIgnoreCase));

        script.Reset();
        Assert.AreEqual(string.Empty, script.GetOutput());
        script.Run();
        Assert.AreEqual(2, diagnostics.Count);
    }
}
