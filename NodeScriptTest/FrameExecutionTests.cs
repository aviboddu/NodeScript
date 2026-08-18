using NodeScript;

namespace NodeScriptTest;

[TestClass]
public class FrameExecutionTests
{
    [TestMethod]
    public void BranchJoinUsesTheValueFromTheExecutedBranch()
    {
        List<string> compileDiagnostics = [];
        Script script = ScriptTestHelpers.CreateLinearScript(
            """
            IF length(input) > 4
            SET value, 1
            ELSE
            SET value, 2
            ENDIF
            PRINT 0, to_string(value)
            """,
            "short\nno",
            compileError: (_, _, message) => compileDiagnostics.Add(message));

        Assert.IsTrue(script.CompileNodes(), string.Join(Environment.NewLine, compileDiagnostics));
        script.Run();

        Assert.AreEqual($"1{Environment.NewLine}2{Environment.NewLine}", script.GetOutput());
    }

    [TestMethod]
    public void RuntimeErrorDoesNotLeakBranchInitializationIntoTheNextInput()
    {
        List<string> compileDiagnostics = [];
        List<string> diagnostics = [];
        Script script = ScriptTestHelpers.CreateLinearScript(
            """
            IF length(input) > 2
            SET value, "stale"
            SET zero, 0
            SET failed, 1 / zero
            ENDIF
            PRINT 0, value
            """,
            "bad\nx",
            compileError: (_, _, message) => compileDiagnostics.Add(message),
            runtimeError: (_, _, message) => diagnostics.Add(message));

        Assert.IsTrue(script.CompileNodes(), string.Join(Environment.NewLine, compileDiagnostics));
        script.Run();

        Assert.AreEqual(2, diagnostics.Count);
        Assert.IsTrue(diagnostics[0].Contains("divide by 0", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(diagnostics[1].Contains("not yet initialized", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(string.Empty, script.GetOutput());
    }
}
