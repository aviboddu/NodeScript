using NodeScript;

namespace NodeScriptTest;

[TestClass]
public class ScriptConstructionTests
{
    [TestMethod]
    public void RegularNodesSupportFanOutButRejectDuplicateTargets()
    {
        Script script = new();
        int inputId = script.AddInputNode("input");
        int regularId = script.AddRegularNode("RETURN");
        int secondRegularId = script.AddRegularNode("RETURN");
        int outputId = script.AddOutputNode();

        script.ConnectNodes(inputId, regularId);
        script.ConnectNodes(regularId, secondRegularId);
        script.ConnectNodes(regularId, outputId);

        Assert.ThrowsException<ArgumentException>(() => script.ConnectNodes(regularId, outputId));
    }

    [TestMethod]
    public void CombinerRejectsASecondOutput()
    {
        Script script = new();
        int inputId = script.AddInputNode("input");
        int combinerId = script.AddCombinerNode();
        int regularId = script.AddRegularNode("RETURN");
        int outputId = script.AddOutputNode();

        script.ConnectNodes(inputId, combinerId);
        script.ConnectNodes(combinerId, regularId);

        Assert.ThrowsException<ArgumentException>(() => script.ConnectNodes(combinerId, outputId));
    }

    [DataTestMethod]
    [DataRow(-1)]
    [DataRow(3)]
    public void EveryIdPositionRejectsOutOfRangeValues(int invalidId)
    {
        Script script = ScriptTestHelpers.CreateLinearScript("RETURN");

        Assert.ThrowsException<ArgumentException>(() => script.ConnectNodes(invalidId, 1));
        Assert.ThrowsException<ArgumentException>(() => script.ConnectNodes(1, invalidId));
        Assert.ThrowsException<ArgumentException>(() => script.UpdateData(invalidId, "RETURN"));
        Assert.ThrowsException<ArgumentException>(() => script.GetCurrentLine(invalidId));
    }

    [TestMethod]
    public void PublicExecutionMethodsSupportCompileStepResetAndReuse()
    {
        Script script = ScriptTestHelpers.CreateLinearScript("PRINT 0, input", "first");

        Assert.IsTrue(script.CompileNodes());
        script.StepLine();
        Assert.AreEqual(0, script.GetCurrentLine(1));
        script.Run();
        Assert.AreEqual($"first{Environment.NewLine}", script.GetOutput());

        script.Reset();
        Assert.AreEqual(string.Empty, script.GetOutput());
        script.Run();
        Assert.AreEqual($"first{Environment.NewLine}", script.GetOutput());
    }
}
