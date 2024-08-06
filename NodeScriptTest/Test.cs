using NodeScript;

namespace NodeScriptTest;

public class Test(string testName)
{
    private const string FOLDER_PATH = "../../../TestData/";
    private readonly string filePath = FOLDER_PATH + testName + "/" + testName;

    public void RunTest()
    {
        string expectedOutput = File.ReadAllText(filePath + ".out");
        (InputNode inputNode, RegularNode regularNode, OutputNode outputNode) = DummyNodeFactory.CreateNodePath(filePath + ".in", filePath + ".ns", CompileError, RuntimeError);
        while (inputNode.State == NodeState.RUNNING || regularNode.State == NodeState.RUNNING)
        {
            inputNode.StepLine();
            regularNode.StepLine();
        }
        Assert.AreEqual(expectedOutput, outputNode.output);
    }

    private void CompileError(int line, string message)
    {
        Console.Error.WriteLine($"Compile error at line {line}: {message}");
    }

    private void RuntimeError(Node node, int line, string message)
    {
        Console.Error.WriteLine($"Runtime error at line {line}: {message}");
    }

}