namespace NodeScriptTest;

[TestClass]
public class Comments
{

    [TestMethod]
    public void CommentsTest()
    {
        Test test = new(GetType().Name);
        test.RunTest();
    }
}