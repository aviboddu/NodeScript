namespace NodeScriptTest;

using NodeScript;

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
    public void FailedTokenization()
    {
        string code = File.ReadAllText(Path.Combine(FOLDER_PATH, "Tokenization", "UnterminatedString.ns"));
        bool had_error = false;
        Tokenizer tokenizer = new(code, (int line, string message) => had_error = true);
        tokenizer.ScanTokens();
        Assert.IsTrue(had_error);

        code = File.ReadAllText(Path.Combine(FOLDER_PATH, "Tokenization", "UnexpectedCharacter.ns"));
        had_error = false;
        tokenizer = new(code, (int line, string message) => had_error = true);
        tokenizer.ScanTokens();
        Assert.IsTrue(had_error);
    }
}