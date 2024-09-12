using System.Text.Json;
using NodeScript;

namespace NodeScriptTest;

[TestClass]
public class Serialization
{
    private static readonly string FOLDER_PATH = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Serialization");
    private readonly string filePath = Path.Combine(FOLDER_PATH, "Serialization");

    [TestMethod]
    public void SerializationTest()
    {
        Script script = new();

        int input_id = script.AddInputNode(File.ReadAllText(filePath + ".in"));
        int node_id = script.AddRegularNode(File.ReadAllText(filePath + ".ns"));
        int output_id = script.AddOutputNode();
        script.ConnectNodes(input_id, node_id);
        script.ConnectNodes(node_id, output_id);

        string json = JsonSerializer.Serialize(script);
        Script? deserializedScript = JsonSerializer.Deserialize<Script>(json);
        Assert.IsTrue(script.Equals(deserializedScript));
    }

}