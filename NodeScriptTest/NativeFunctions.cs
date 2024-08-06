namespace NodeScriptTest;

[TestClass]
public class NativeFunctions
{

    [TestMethod]
    public void NativeFunctionsTest()
    {
        Test test = new(GetType().Name);
        test.RunTest();
    }
}