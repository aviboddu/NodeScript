namespace NodeScriptTest;

[TestClass]
public class Arithmetic
{

    [TestMethod]
    public void ArithmeticTest()
    {
        Test test = new(GetType().Name);
        test.RunTest();
    }
}