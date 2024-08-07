namespace NodeScriptTest;

[TestClass]
public class ConstantPropogation
{

    [TestMethod]
    public void ConstantPropogationTest()
    {
        Test test = new(GetType().Name);
        test.RunTest();
    }
}