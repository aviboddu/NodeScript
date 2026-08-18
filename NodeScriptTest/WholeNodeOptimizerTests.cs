using NodeScript;
using static NodeScript.TokenType;

namespace NodeScriptTest;

[TestClass]
public class WholeNodeOptimizerTests
{
    [TestMethod]
    public void PropagatesConstantsAtControlFlowJoin()
    {
        Operation?[] operations = Optimize("""
            SET value, 1
            IF input == "left"
            SET value, 2
            ELSE
            SET value, 2
            ENDIF
            PRINT 0, to_string(value)
            RETURN
            """);

        Call call = (Call)operations[6]!.expressions[1];
        Assert.IsInstanceOfType<Literal>(call.Arguments[0]);
        Assert.AreEqual(2, ((Literal)call.Arguments[0]).Value);
    }

    [TestMethod]
    public void FoldsBranchesAndDeadLiteralStoresWithoutChangingStepLines()
    {
        Script script = ScriptTestHelpers.CreateLinearScript("""
            IF true
            SET unused, 1
            ELSE
            PRINT 0, "unreachable"
            ENDIF
            NOP
            PRINT 0, "reachable"
            RETURN
            """);

        Assert.IsTrue(script.CompileNodes());
        script.StepLine();
        Assert.AreEqual(0, script.GetCurrentLine(1));
        script.Run();
        Assert.AreEqual($"reachable{Environment.NewLine}", script.GetOutput());
    }

    [TestMethod]
    public void DeadLiteralStoreProducesSmallerPlan()
    {
        const string source = """
            SET unused, "constant"
            RETURN
            """;
        Compiler.CompiledData before = Compile(Parse(source));
        Compiler.CompiledData after = Compile(Optimize(source));

        Assert.IsTrue(after.Code.Length < before.Code.Length);
        Assert.IsTrue(after.NumVariables < before.NumVariables);
    }

    [TestMethod]
    public void RetainsValueNeededWhenConditionalAssignmentIsSkipped()
    {
        Script script = ScriptTestHelpers.CreateLinearScript(
            """
            SET value, "default"
            IF length(input) > 1
            SET value, "branch"
            ENDIF
            PRINT 0, value
            """,
            "x");

        Assert.IsTrue(script.CompileNodes());
        script.Run();

        Assert.AreEqual($"default{Environment.NewLine}", script.GetOutput());
    }

    [TestMethod]
    public void DoesNotRemoveEagerBooleanOperand()
    {
        List<string> diagnostics = [];
        Script script = ScriptTestHelpers.CreateLinearScript(
            """
            SET result, false AND can_parse()
            RETURN
            """,
            compileError: (_, _, message) => diagnostics.Add(message));

        Assert.IsFalse(script.CompileNodes());
        Assert.IsTrue(diagnostics.Count > 0);
    }

    private static Operation?[] Optimize(string source)
    {
        Operation?[] operations = Parse(source);
        Optimizer.PropogateConstants(operations, (_, message) => Assert.Fail(message));
        Validator.Validate(operations, (_, message) => Assert.Fail(message));
        return operations;
    }

    private static Operation?[] Parse(string source)
    {
        Tokenizer tokenizer = new(source, (_, message) => Assert.Fail(message));
        Parser parser = new(tokenizer.ScanTokens(), (_, message) => Assert.Fail(message));
        return parser.Parse();
    }

    private static Compiler.CompiledData Compile(Operation?[] operations)
    {
        Validator.Validate(operations, (_, message) => Assert.Fail(message));
        Compiler compiler = new(operations, (_, message) => Assert.Fail(message));
        return compiler.Compile();
    }
}
