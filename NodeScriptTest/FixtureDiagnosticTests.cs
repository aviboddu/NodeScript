using NodeScript;

namespace NodeScriptTest;

[TestClass]
public class FixtureDiagnosticTests
{
    private static readonly string FolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Erroring");

    [DataTestMethod]
    [DataRow("Tokenization", "UnexpectedCharacter", "Unexpected character")]
    [DataRow("Parsing", "NoCommaBetweenExpressions", "comma")]
    [DataRow("Validation", "DuplicateElse", "Duplicate ELSE")]
    public void InvalidSourceFixturesReportDiagnosticsAndDoNotExecute(
        string stage,
        string testName,
        string expectedMessage)
    {
        List<string> diagnostics = [];
        string code = File.ReadAllText(Path.Combine(FolderPath, stage, $"{testName}.ns"));
        Script script = ScriptTestHelpers.CreateLinearScript(
            code,
            compileError: (_, line, message) => diagnostics.Add($"{line}:{message}"));

        Assert.IsFalse(script.CompileNodes());
        Assert.IsTrue(diagnostics.Any(diagnostic =>
            diagnostic.Contains(expectedMessage, StringComparison.OrdinalIgnoreCase)));
        script.Run();
        Assert.AreEqual(string.Empty, script.GetOutput());
    }

    [TestMethod]
    public void RuntimeFailureFixtureReportsExpectedDiagnostic()
    {
        List<string> diagnostics = [];
        string filePath = Path.Combine(FolderPath, "Runtime", "DivideByZero");
        Script script = ScriptTestHelpers.CreateLinearScript(
            File.ReadAllText(filePath + ".ns"),
            File.ReadAllText(filePath + ".in"),
            runtimeError: (_, line, message) => diagnostics.Add($"{line}:{message}"));

        Assert.IsTrue(script.CompileNodes());
        script.Run();

        Assert.AreEqual(File.ReadAllText(filePath + ".error").Trim(), diagnostics.Single());
        Assert.AreEqual(string.Empty, script.GetOutput());
    }
}
