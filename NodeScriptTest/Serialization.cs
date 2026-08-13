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

    [TestMethod]
    public void RoundTripScriptCompilesExecutesAndResets()
    {
        Script original = ScriptTestHelpers.CreateLinearScript("PRINT 0, input", "round trip");
        string json = JsonSerializer.Serialize(original);
        Script script = JsonSerializer.Deserialize<Script>(json)!;
        List<string> diagnostics = [];
        script.CompileError += (_, _, message) => diagnostics.Add(message);
        script.RuntimeError += (_, _, message) => diagnostics.Add(message);

        Assert.IsTrue(script.CompileNodes());
        script.Run();
        Assert.AreEqual($"round trip{Environment.NewLine}", script.GetOutput());

        script.Reset();
        script.Run();
        Assert.AreEqual($"round trip{Environment.NewLine}", script.GetOutput());
        Assert.AreEqual(0, diagnostics.Count);
    }

}