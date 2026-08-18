using NodeScript;

namespace NodeScriptTest;

[TestClass]
public class NativeFunctionTests
{
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
    public void RuntimeValuesPreserveEqualitySemanticsAcrossKinds()
    {
        const string code = """
            SET intVal, 1 + 2
            SET arrVal, split(",", input)
            SET x, element_at(arrVal, 0)
            SET one, parse_int(element_at(arrVal, 1))
            SET boolVal, one == one
            SET strVal, "ab" + "c"
            SET truth, can_parse(input)
            PRINT 0, to_string(intVal)
            PRINT 0, to_string(boolVal)
            PRINT 0, strVal
            PRINT 0, to_string(length(arrVal))
            PRINT 0, to_string(arrVal == arrVal)
            PRINT 0, to_string(arrVal == split(",", input))
            PRINT 0, to_string(x == x)
            PRINT 0, to_string(one == one)
            PRINT 0, to_string(truth == truth)
            """;
        List<string> diagnostics = [];
        Script script = ScriptTestHelpers.CreateLinearScript(
            code,
            "x,1",
            compileError: (_, _, message) => diagnostics.Add($"compile:{message}"),
            runtimeError: (_, _, message) => diagnostics.Add(message));

        Assert.IsTrue(script.CompileNodes(), string.Join(Environment.NewLine, diagnostics));
        script.Run();

        string expected = string.Join(Environment.NewLine, ["3", "True", "abc", "2", "True", "False", "True", "True", "True"]) + Environment.NewLine;
        Assert.AreEqual(0, diagnostics.Count);
        Assert.AreEqual(expected, script.GetOutput());
    }

    [TestMethod]
    public void RepeatedResetAndRunCyclesRemainStable()
    {
        const string code = """
            SET parts, split(",", input)
            SET left, parse_int(element_at(parts, 0))
            SET right, parse_int(element_at(parts, 1))
            PRINT 0, to_string(left + right)
            """;
        List<string> diagnostics = [];
        Script script = ScriptTestHelpers.CreateLinearScript(
            code,
            "2,40",
            runtimeError: (_, _, message) => diagnostics.Add(message));
        Assert.IsTrue(script.CompileNodes());

        for (int i = 0; i < 8; i++)
        {
            script.Run();
            Assert.AreEqual($"42{Environment.NewLine}", script.GetOutput(), $"failed on iteration {i}");
            script.Reset();
            Assert.AreEqual(string.Empty, script.GetOutput(), $"reset failed on iteration {i}");
        }

        Assert.AreEqual(0, diagnostics.Count);
    }
}
