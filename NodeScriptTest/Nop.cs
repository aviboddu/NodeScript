namespace NodeScriptTest;

[TestClass]
public class Nop
{

    [TestMethod]
    public void NopTest()
    {
        Test test = new(GetType().Name);
        test.RunTest();
    }
}