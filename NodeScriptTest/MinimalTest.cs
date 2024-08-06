namespace NodeScriptTest;

[TestClass]
public class MinimalTest
{

    [TestMethod]
    public void MinimalNodeTest()
    {
        Test test = new(GetType().Name);
        test.RunTest();
    }
}