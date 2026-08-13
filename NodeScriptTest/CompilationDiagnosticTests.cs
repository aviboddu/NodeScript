using NodeScript;

namespace NodeScriptTest;

[TestClass]
public class CompilationDiagnosticTests
{
    [DataTestMethod]
    [DataRow("ELSE", "ELSE without corresponding IF")]
    [DataRow("ENDIF", "IF statements do not match ENDIF statements")]
    [DataRow("IF true\nNOP", "IF statements do not match ENDIF statements")]
    [DataRow("IF true\nELSE\nELSE\nENDIF", "Duplicate ELSE")]
    public void InvalidControlFlowReturnsDiagnosticsWithoutEscapingExceptions(string code, string expectedMessage)
    {
        List<(int Node, int Line, string Message)> diagnostics = [];
        Script script = ScriptTestHelpers.CreateLinearScript(
            code,
            compileError: (node, line, message) => diagnostics.Add((node, line, message)));

        Assert.IsFalse(script.CompileNodes());
        Assert.IsTrue(diagnostics.Any(d =>
            d.Node == 1 && d.Line >= 0 && d.Message.Contains(expectedMessage, StringComparison.Ordinal)));
        script.Run();
        Assert.AreEqual(string.Empty, script.GetOutput());
    }

    [TestMethod]
    public void NestedIfElseCompilesAndExecutes()
    {
        const string code = """
            IF true
            IF false
            PRINT 0, "wrong"
            ELSE
            PRINT 0, input
            ENDIF
            ELSE
            PRINT 0, "wrong"
            ENDIF
            """;
        List<string> diagnostics = [];
        Script script = ScriptTestHelpers.CreateLinearScript(
            code,
            "nested",
            (node, line, message) => diagnostics.Add($"{node}:{line}:{message}"));

        Assert.IsTrue(script.CompileNodes());
        script.Run();

        Assert.AreEqual(0, diagnostics.Count);
        Assert.AreEqual($"nested{Environment.NewLine}", script.GetOutput());
    }

    [TestMethod]
    public void IndependentControlFlowErrorsAreAllReported()
    {
        List<string> diagnostics = [];
        Script script = ScriptTestHelpers.CreateLinearScript(
            "ELSE\nENDIF",
            compileError: (_, _, message) => diagnostics.Add(message));

        Assert.IsFalse(script.CompileNodes());
        Assert.IsTrue(diagnostics.Any(message => message.Contains("ELSE", StringComparison.Ordinal)));
        Assert.IsTrue(diagnostics.Any(message => message.Contains("ENDIF", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void ConstantDivisionByZeroIsAControlledCompilationError()
    {
        List<(int Line, string Message)> diagnostics = [];
        Script script = ScriptTestHelpers.CreateLinearScript(
            "SET value, 1 / 0",
            compileError: (_, line, message) => diagnostics.Add((line, message)));

        Assert.IsFalse(script.CompileNodes());
        Assert.IsTrue(diagnostics.Any(d =>
            d.Line == 0 && d.Message.Contains("divide by 0", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void FailedRecompileCannotRunPreviouslyCompiledCode()
    {
        Script script = ScriptTestHelpers.CreateLinearScript("PRINT 0, input", "stale");
        Assert.IsTrue(script.CompileNodes());
        script.Run();
        script.Reset();

        script.UpdateData(1, "ELSE");
        Assert.IsFalse(script.CompileNodes());
        script.Run();

        Assert.AreEqual(string.Empty, script.GetOutput());
    }
}
