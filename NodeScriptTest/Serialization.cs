using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Xml.Serialization;
using NodeScript;

namespace NodeScriptTest;

[TestClass]
public class Serialization
{
    private const string FOLDER_PATH = "../../../TestData/";
    private readonly string filePath = FOLDER_PATH + "Serialization/Serialization";

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