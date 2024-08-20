using System.Security.Cryptography;
using NodeScript;

namespace NodeScriptTest;

public class Test(string testName)
{
    private const string FOLDER_PATH = "../../../TestData/";
    private readonly string filePath = FOLDER_PATH + testName + "/" + testName;

    public void RunTest()
    {
        string expectedOutput = File.ReadAllText(filePath + ".out");
        Script script = new();
        int input_id = script.AddInputNode(File.ReadAllText(filePath + ".in"));
        int node_id = script.AddRegularNode(File.ReadAllText(filePath + ".ns"));
        int output_id = script.AddOutputNode();
        script.ConnectNodes(input_id, node_id);
        script.ConnectNodes(node_id, output_id);

        script.CompileNodes();
        script.Run();

        string actualOutput = ((OutputNode)script.Nodes[output_id]).Output;
        Assert.AreEqual(expectedOutput, actualOutput);
    }

}