using NodeScript;

namespace NodeScriptTest;

[TestClass]
public class NativeFunctionTests
{
    [TestMethod]
    public void NativeRegistryUsesStableIdsAndTypedOverloads()
    {
        Assert.AreEqual(24, NativeFunctionRegistry.Count);

        Assert.IsTrue(NativeFunctionRegistry.TryResolveForCompile(
            "join",
            [typeof(string), typeof(string[])],
            out NativeFunctionDescriptor typedJoin,
            out _));
        Assert.AreEqual(6, typedJoin.Id);
        Assert.IsFalse(typedJoin.IsDynamicBoundary);

        Assert.IsTrue(NativeFunctionRegistry.TryResolveForCompile(
            "join",
            [typeof(object), typeof(object)],
            out NativeFunctionDescriptor dynamicJoin,
            out _));
        Assert.AreEqual(5, dynamicJoin.Id);
        Assert.IsTrue(dynamicJoin.IsDynamicBoundary);
    }

    [DataTestMethod]
    [DataRow("PRINT 0, to_string(length(input))", "abc", "3")]
    [DataRow("PRINT 0, to_string(length(split(\",\", input)))", "a,b", "2")]
    [DataRow("PRINT 0, join(\"-\", split(\",\", input))", "a,b", "a-b")]
    [DataRow("PRINT 0, to_string(index_of(\"b\", input))", "abc", "1")]
    [DataRow("PRINT 0, slice(input, 0, 2)", "abc", "ab")]
    [DataRow("PRINT 0, element_at(input, 1)", "abc", "b")]
    [DataRow("PRINT 0, to_string(parse_int(input))", "42", "42")]
    [DataRow("PRINT 0, to_string(can_parse(input))", "42", "True")]
    [DataRow("PRINT 0, join(\",\", remove_at(split(\",\", input), 0))", "a,b", "b")]
    [DataRow("PRINT 0, trim(input)", " abc ", "abc")]
    public void NativeFunctionsCompileAndExecute(string code, string input, string expected)
    {
        List<string> diagnostics = [];
        Script script = ScriptTestHelpers.CreateLinearScript(
            code,
            input,
            (_, _, message) => diagnostics.Add(message),
            (_, _, message) => diagnostics.Add(message));

        Assert.IsTrue(script.CompileNodes());
        script.Run();

        Assert.AreEqual(0, diagnostics.Count);
        Assert.AreEqual($"{expected}{Environment.NewLine}", script.GetOutput());
    }

    [DataTestMethod]
    [DataRow("SET value, length(1)")]
    [DataRow("SET value, split(\",\", 1)")]
    [DataRow("SET value, join(\",\", \"not an array\")")]
    [DataRow("SET value, index_of(\"a\", 1)")]
    [DataRow("SET value, slice(\"abc\", 0)")]
    [DataRow("SET value, element_at(\"abc\", \"0\")")]
    [DataRow("SET value, to_string()")]
    [DataRow("SET value, parse_int(1)")]
    [DataRow("SET value, can_parse()")]
    [DataRow("SET value, remove_at(\"abc\", 0)")]
    [DataRow("SET value, trim(1)")]
    public void InvalidNativeCallsFailCompilation(string code)
    {
        List<(int Node, int Line, string Message)> diagnostics = [];
        Script script = ScriptTestHelpers.CreateLinearScript(
            code,
            compileError: (node, line, message) => diagnostics.Add((node, line, message)));

        Assert.IsFalse(script.CompileNodes());
        Assert.IsTrue(diagnostics.Any(d =>
            d.Node == 1 && d.Line == 0 && !string.IsNullOrWhiteSpace(d.Message)));
        script.Run();
        Assert.AreEqual(string.Empty, script.GetOutput());
    }

    [TestMethod]
    public void InvalidNonLiteralNativeSignatureFailsCompilation()
    {
        const string code = """
            SET sep, 0
            SET arr, split(",", input)
            SET value, join(sep, arr)
            """;
        List<(int Node, int Line, string Message)> diagnostics = [];
        Script script = ScriptTestHelpers.CreateLinearScript(
            code,
            compileError: (node, line, message) => diagnostics.Add((node, line, message)));

        Assert.IsFalse(script.CompileNodes());
        Assert.IsTrue(diagnostics.Any(d =>
            d.Node == 1 &&
            d.Line == 2 &&
            d.Message.Contains("No overload for function join", StringComparison.Ordinal)));
    }
}
