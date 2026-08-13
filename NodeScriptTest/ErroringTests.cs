namespace NodeScriptTest;

using NodeScript;
using System.Text.Json;
using System.Text.Json.Nodes;

[TestClass]
public class ErroringTests
{
    private static readonly string FOLDER_PATH = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Erroring");

    [TestMethod]
    public void FailedScriptConstruction()
    {
        Script script = new();
        int input_id = script.AddInputNode("TEST DATA");
        int node_id = script.AddRegularNode("TEST CODE");
        int output_id = script.AddOutputNode();

        script.ConnectNodes(input_id, node_id);
        script.ConnectNodes(node_id, output_id);

        Assert.ThrowsException<ArgumentException>(() => script.AddInputNode("TEST DATA"), "Script.AddInputNode() allowed duplicate input node");
        Assert.ThrowsException<ArgumentException>(() => script.AddOutputNode(), "Script.AddOutputNode() allowed duplicate output node");
        Assert.ThrowsException<ArgumentException>(() => script.ConnectNodes(-1, 1), "Script.ConnectNodes() allows negative ids");
        Assert.ThrowsException<ArgumentException>(() => script.ConnectNodes(4, 5), "Script.ConnectNodes() allows ids which are too large (not assigned to a node yet)");
        Assert.ThrowsException<ArgumentException>(() => script.ConnectNodes(node_id, node_id), "Script.ConnectNodes() allows nodes to connect to themselves");
        Assert.ThrowsException<ArgumentException>(() => script.ConnectNodes(node_id, input_id), "Script.ConnectNodes() allows outputting to input node");
        Assert.ThrowsException<ArgumentException>(() => script.ConnectNodes(output_id, node_id), "Script.ConnectNodes() allows inputting from output node");
        Assert.ThrowsException<ArgumentException>(() => script.ConnectNodes(input_id, output_id), "Script.ConnectNodes() allows input node to have a second output");
        Assert.ThrowsException<ArgumentException>(() => script.ConnectNodes(node_id, output_id), "Script.ConnectNodes() allows making the same connection twice");
        Assert.ThrowsException<ArgumentException>(() => script.UpdateData(output_id, "JUNK_DATA"), "Script.UpdateData() allows updating output node's data");
        Assert.ThrowsException<ArgumentException>(() => script.UpdateData(-1, "JUNK_DATA"), "Script.UpdateData() allows negative id");
        Assert.ThrowsException<ArgumentException>(() => script.UpdateData(4, "JUNK_DATA"), "Script.UpdateData() allows ids which are too large (not assigned to a node yet)");
        Assert.ThrowsException<InvalidOperationException>(() => new Script().CompileNodes(), "Script.CompileNodes allows compilation without an input node");
        Assert.ThrowsException<ArgumentException>(() => script.GetCurrentLine(-1), "Script.GetCurrentLine() allows negative id");
        Assert.ThrowsException<ArgumentException>(() => script.GetCurrentLine(4), "Script.GetCurrentLine() allows ids which are too large (not assigned to a node yet)");
    }

    [TestMethod]
    public void NodeIdEqualToNodeCountIsRejected()
    {
        Script script = new();
        int inputId = script.AddInputNode("input");
        int nodeId = script.AddRegularNode(File.ReadAllText(Path.Combine(FOLDER_PATH, "Compilation", "Return.ns")));
        int outputId = script.AddOutputNode();
        script.ConnectNodes(inputId, nodeId);
        script.ConnectNodes(nodeId, outputId);

        Assert.ThrowsException<ArgumentException>(() => script.ConnectNodes(3, outputId));
        Assert.ThrowsException<ArgumentException>(() => script.UpdateData(3, "code"));
        Assert.IsTrue(script.CompileNodes());
        Assert.ThrowsException<ArgumentException>(() => script.GetCurrentLine(3));
    }

    [TestMethod]
    public void InvalidControlFlowReportsErrorsWithoutThrowing()
    {
        string[] files = Directory
            .EnumerateFiles(Path.Combine(FOLDER_PATH, "Validation"), "*.ns")
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        Assert.IsTrue(files.Length > 0, "Expected validation fixtures.");

        foreach (string filePath in files)
        {
            string code = File.ReadAllText(filePath);
            List<string> errors = [];
            Script script = new((_, _, message) => errors.Add(message), (_, _, _) => { });
            int inputId = script.AddInputNode("input");
            int nodeId = script.AddRegularNode(code);
            int outputId = script.AddOutputNode();
            script.ConnectNodes(inputId, nodeId);
            script.ConnectNodes(nodeId, outputId);

            Assert.IsFalse(script.CompileNodes(), filePath);
            Assert.IsTrue(errors.Count > 0, filePath);
        }
    }

    [TestMethod]
    public void InvalidDeserializedTopologyReportsErrorsWithoutThrowing()
    {
        Script original = new();
        int inputId = original.AddInputNode("input");
        int nodeId = original.AddRegularNode(File.ReadAllText(Path.Combine(FOLDER_PATH, "Compilation", "Return.ns")));
        int outputId = original.AddOutputNode();
        original.ConnectNodes(inputId, nodeId);
        original.ConnectNodes(nodeId, outputId);

        JsonObject serialized = JsonNode.Parse(JsonSerializer.Serialize(original))!.AsObject();
        JsonArray nodes = serialized["nodesData"]!.AsArray();
        nodes[0]!["Outputs"] = new JsonArray(1, 2);
        nodes[1]!["Outputs"] = new JsonArray(2, 2, -1, 99);
        nodes[2]!["Outputs"] = new JsonArray(0);
        List<string> errors = [];
        Script script = JsonSerializer.Deserialize<Script>(serialized.ToJsonString())!;
        script.CompileError += (_, _, message) => errors.Add(message);

        Assert.IsFalse(script.CompileNodes());
        Assert.IsTrue(errors.Count >= 6);
        Assert.IsTrue(errors.Any(message => message.Contains("only have one output", StringComparison.Ordinal)));
        Assert.IsTrue(errors.Count(message => message.Contains("out of range", StringComparison.Ordinal)) >= 2);
        Assert.IsTrue(errors.Any(message => message.Contains("same connection twice", StringComparison.Ordinal)));
        Assert.IsTrue(errors.Any(message => message.Contains("doesn't have its own output", StringComparison.Ordinal)));
        Assert.IsTrue(errors.Any(message => message.Contains("input node", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void CyclicTopologyReportsErrorsWithoutRunning()
    {
        List<string> errors = [];
        Script script = new((_, _, message) => errors.Add(message), (_, _, _) => { });
        int inputId = script.AddInputNode("input");
        string code = File.ReadAllText(Path.Combine(FOLDER_PATH, "Compilation", "Return.ns"));
        int firstNodeId = script.AddRegularNode(code);
        int secondNodeId = script.AddRegularNode(code);
        script.AddOutputNode();
        script.ConnectNodes(inputId, firstNodeId);
        script.ConnectNodes(firstNodeId, secondNodeId);
        script.ConnectNodes(secondNodeId, firstNodeId);

        Assert.IsFalse(script.CompileNodes());
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public void FailedTokenization()
    {
        string folder = Path.Combine(FOLDER_PATH, "Tokenization");
        string[] files = Directory
            .EnumerateFiles(folder, "*.ns")
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        Assert.IsTrue(files.Length > 0, "Expected tokenization fixtures.");

        foreach (string filePath in files)
        {
            string code = File.ReadAllText(filePath);
            bool had_error = false;
            Tokenizer tokenizer = new(code, (_, _) => had_error = true);
            tokenizer.ScanTokens();
            Assert.IsTrue(had_error);
        }
    }

    [TestMethod]
    public void FailedParsing()
    {
        string folder = Path.Combine(FOLDER_PATH, "Parsing");
        string[] files = Directory
            .EnumerateFiles(folder, "*.ns")
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        Assert.IsTrue(files.Length > 0, "Expected parsing fixtures.");

        foreach (string filePath in files)
        {
            string fileName = Path.GetFileName(filePath);
            string code = File.ReadAllText(filePath);
            bool had_error = false;
            Tokenizer tokenizer = new(code, (_, message) => throw new AssertFailedException($"{fileName} : {message}"));
            Token[][] tokens = tokenizer.ScanTokens();
            Parser parser = new(tokens, (_, _) => had_error = true);
            parser.Parse();
            Assert.IsTrue(had_error);
        }
    }
}