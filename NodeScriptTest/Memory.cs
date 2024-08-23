namespace NodeScriptTest;

[TestClass]
public class Memory
{

    [TestMethod]
    public void MemoryTest()
    {
        Test test = new(GetType().Name);
        test.RunTest();
    }
}