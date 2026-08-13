using NodeScript;

namespace NodeScriptTest;

[TestClass]
public class NativeFunctionTests
{
    [TestMethod]
    public void EveryNativeFunctionHasASuccessCase()
    {
        AssertResult(NativeFuncs.length(["abc"]), 3);
        AssertResult(NativeFuncs.split([",", "a,b"]), new[] { "a", "b" });
        AssertResult(NativeFuncs.join([",", new[] { "a", "b" }]), "a,b");
        AssertResult(NativeFuncs.index_of(["b", "abc"]), 1);
        AssertResult(NativeFuncs.slice(["abc", 0, 2]), "ab");
        AssertResult(NativeFuncs.element_at([new[] { "a", "b" }, 1]), "b");
        AssertResult(NativeFuncs.to_string([42]), "42");
        AssertResult(NativeFuncs.parse_int(["2147483647"]), int.MaxValue);
        AssertResult(NativeFuncs.can_parse(["-2147483648"]), true);
        AssertResult(NativeFuncs.remove_at([new[] { "a", "b" }, 0]), new[] { "b" });
        AssertResult(NativeFuncs.trim([" \t"]), string.Empty);
    }

    [TestMethod]
    public void EveryNativeFunctionRejectsInvalidInput()
    {
        AssertFailure(NativeFuncs.length([false]));
        AssertFailure(NativeFuncs.split([","]));
        AssertFailure(NativeFuncs.join([",", "not an array"]));
        AssertFailure(NativeFuncs.index_of(["only one"]));
        AssertFailure(NativeFuncs.slice(["abc", -1, 2]));
        AssertFailure(NativeFuncs.element_at(["", 0]));
        AssertFailure(NativeFuncs.to_string([]));
        AssertFailure(NativeFuncs.parse_int(["not an integer"]));
        AssertFailure(NativeFuncs.can_parse([]));
        AssertFailure(NativeFuncs.remove_at([Array.Empty<string>(), 0]));
        AssertFailure(NativeFuncs.trim([1]));
    }

    private static void AssertResult(Result result, object expected)
    {
        Assert.IsTrue(result.Success(), result.message);
        object? actual = result.GetValue();
        if (expected is string[] expectedArray)
            CollectionAssert.AreEqual(expectedArray, (string[])actual!);
        else
            Assert.AreEqual(expected, actual);
    }

    private static void AssertFailure(Result result)
    {
        Assert.IsFalse(result.Success());
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.message));
    }
}
