using NodeScript;

namespace NodeScriptTest;

[TestClass]
public class SuccessfulTests()
{
    private static readonly string FOLDER_PATH = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Successful");

    private static void RunTest(string testName)
    {
        string filePath = Path.Combine(FOLDER_PATH, testName, testName);
        string expectedOutput = File.ReadAllText(filePath + ".out");
        List<string> diagnostics = [];
        Script script = new(
            (_, line, message) => diagnostics.Add($"compile:{line}:{message}"),
            (_, line, message) => diagnostics.Add($"runtime:{line}:{message}"));

        int input_id = script.AddInputNode(File.ReadAllText(filePath + ".in"));
        int node_id = script.AddRegularNode(File.ReadAllText(filePath + ".ns"));
        int output_id = script.AddOutputNode();
        script.ConnectNodes(input_id, node_id);
        script.ConnectNodes(node_id, output_id);

        Assert.IsTrue(script.CompileNodes(), $"{testName}: {string.Join(Environment.NewLine, diagnostics)}");
        script.Run();
        Assert.AreEqual(0, diagnostics.Count, $"{testName}: {string.Join(Environment.NewLine, diagnostics)}");
        Assert.AreEqual(expectedOutput.ReplaceLineEndings(), script.GetOutput());
    }

    [TestMethod] public void ArithmeticTest() => RunTest("Arithmetic");
    [TestMethod] public void CommentsTest() => RunTest("Comments");
    [TestMethod] public void ConstantIndexTest() => RunTest("ConstantIndex");
    [TestMethod] public void ConstantPropogationTest() => RunTest("ConstantPropogation");
    [TestMethod] public void IfTest() => RunTest("If");
    [TestMethod] public void IfElseTest() => RunTest("IfElse");
    [TestMethod] public void IndexTest() => RunTest("Index");
    [TestMethod] public void LongestStringTest() => RunTest("LongestString");
    [TestMethod] public void MemoryTest() => RunTest("Memory");
    [TestMethod] public void MinimalTest() => RunTest("Minimal");
    [TestMethod] public void NativeFunctionsTest() => RunTest("NativeFunctions");
    [TestMethod] public void NopTest() => RunTest("Nop");
    [TestMethod] public void SliceTest() => RunTest("Slice");
    [TestMethod] public void VariablesTest() => RunTest("Variables");

    [TestMethod] public void UnaryExpressionsCompileAndRun() => RunTest("Unary");
    [TestMethod] public void NestedControlFlowFixture() => RunTest("NestedControlFlow");
    [TestMethod] public void PrecedenceFixture() => RunTest("Precedence");
    [TestMethod] public void UnicodeAndNativeBoundaryFixture() => RunTest("UnicodeAndNativeBoundaries");
}