namespace NodeScriptTest;

[TestClass]
public class Variables
{

    [TestMethod]
    public void VariablesTest()
    {
        Test test = new(GetType().Name);
        test.RunTest();
    }
}