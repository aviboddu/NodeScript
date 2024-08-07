namespace NodeScriptTest;

[TestClass]
public class Index
{

    [TestMethod]
    public void IndexTest()
    {
        Test test = new(GetType().Name);
        test.RunTest();
    }
}