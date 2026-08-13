using System.Diagnostics;
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
        Script script = new(CompileError, RuntimeError);

        int input_id = script.AddInputNode(File.ReadAllText(filePath + ".in"));
        int node_id = script.AddRegularNode(File.ReadAllText(filePath + ".ns"));
        int output_id = script.AddOutputNode();
        script.ConnectNodes(input_id, node_id);
        script.ConnectNodes(node_id, output_id);

        script.CompileNodes();
        script.Run();
        Assert.AreEqual(expectedOutput.ReplaceLineEndings(), script.GetOutput());
    }

    private static void CompileError(int id, int line, string message)
    {
        Debug.WriteLine($"Compilation error at node {id}, line {line}: {message}");
    }

    private static void RuntimeError(int id, int line, string message)
    {
        Debug.WriteLine($"Runtime error at node {id}, line {line}: {message}");
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

    [TestMethod]
    public void UnaryExpressionsCompileAndRun()
    {
        List<string> errors = [];
        Script script = new((_, _, message) => errors.Add(message), (_, _, _) => { });
        int inputId = script.AddInputNode("input");
        int nodeId = script.AddRegularNode("SET x, -length(input)\nPRINT 0, to_string(x)\nIF !(input == mem)\nPRINT 0, \"ok\"\nENDIF");
        int outputId = script.AddOutputNode();
        script.ConnectNodes(inputId, nodeId);
        script.ConnectNodes(nodeId, outputId);

        Assert.IsTrue(script.CompileNodes(), string.Join(Environment.NewLine, errors));
        script.Run();
        Assert.AreEqual("-5\nok\n", script.GetOutput());
    }









}