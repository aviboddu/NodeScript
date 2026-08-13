using System.Text;
using System.Text.Json;
using NodeScript;

namespace NodeScriptTest;

[TestClass]
public class GeneratedPropertyTests
{
    private static readonly int[] Seeds = [17, 23, 41, 59, 83, 109, 131, 157, 181, 211];

    [TestMethod]
    public void ArbitraryGeneratedSourceCompilesOrReportsDiagnosticsWithoutEscapingExceptions()
    {
        foreach (int seed in Seeds)
        {
            Random random = new(seed);
            string source = GenerateArbitrarySource(random, maxLines: 6, maxTokenLength: 8);
            List<string> compileDiagnostics = [];
            List<string> runtimeDiagnostics = [];
            Script script = ScriptTestHelpers.CreateLinearScript(
                source,
                "seed-input",
                compileError: (_, line, message) => compileDiagnostics.Add($"{line}:{message}"),
                runtimeError: (_, line, message) => runtimeDiagnostics.Add($"{line}:{message}"));

            bool compiled = false;
            try
            {
                compiled = script.CompileNodes();
                if (compiled)
                    script.Run();
            }
            catch (Exception ex)
            {
                Assert.Fail($"Seed {seed} escaped an exception:{Environment.NewLine}{ex}{Environment.NewLine}{source}");
            }

            if (!compiled)
                Assert.IsTrue(compileDiagnostics.Count > 0, $"Seed {seed} failed without diagnostics.{Environment.NewLine}{source}");
        }
    }

    [TestMethod]
    public void GeneratedSourcesHaveDeterministicCompilationResults()
    {
        foreach (int seed in Seeds)
        {
            Random random = new(seed * 3);
            string source = GenerateArbitrarySource(random, maxLines: 5, maxTokenLength: 6);
            List<string> firstDiagnostics = [];
            List<string> secondDiagnostics = [];
            Script script = ScriptTestHelpers.CreateLinearScript(source, "input");
            script.CompileError += (_, line, message) => firstDiagnostics.Add($"{line}:{message}");
            bool firstResult = script.CompileNodes();

script.CompileError = (_, line, message) => secondDiagnostics.Add($"{line}:{message}");
            bool secondResult = script.CompileNodes();

            Assert.AreEqual(firstResult, secondResult, $"Seed {seed} changed compile result.{Environment.NewLine}{source}");
            CollectionAssert.AreEqual(firstDiagnostics, secondDiagnostics, $"Seed {seed} changed diagnostics.");
        }
    }

    [TestMethod]
    public void GeneratedValidProgramsPreserveOutputAfterSerializationAndReset()
    {
        foreach (int seed in Seeds)
        {
            (string source, string input) = GenerateSafeProgram(seed);
            Script original = ScriptTestHelpers.CreateLinearScript(source, input);

            Assert.IsTrue(original.CompileNodes(), $"Seed {seed} should compile.{Environment.NewLine}{source}");
            original.Run();
            string firstOutput = original.GetOutput() ?? string.Empty;

            string json = JsonSerializer.Serialize(original);
            Script roundTripped = JsonSerializer.Deserialize<Script>(json)!;
            List<string> diagnostics = [];
            roundTripped.CompileError += (_, line, message) => diagnostics.Add($"{line}:{message}");
            roundTripped.RuntimeError += (_, line, message) => diagnostics.Add($"{line}:{message}");

            Assert.IsTrue(roundTripped.CompileNodes(), $"Seed {seed} round-trip compile failed.");
            roundTripped.Run();
            string secondOutput = roundTripped.GetOutput() ?? string.Empty;
            Assert.AreEqual(firstOutput, secondOutput, $"Seed {seed} changed output after round-trip.");

            roundTripped.Reset();
            roundTripped.Run();
            Assert.AreEqual(secondOutput, roundTripped.GetOutput(), $"Seed {seed} changed output after reset.");
            Assert.AreEqual(0, diagnostics.Count, $"Seed {seed} produced diagnostics.");
        }
    }

    private static (string Source, string Input) GenerateSafeProgram(int seed)
    {
        Random random = new(seed * 7 + 3);
        string text = $"v{random.Next(1000)}";
        return (seed % 5) switch
        {
            0 => ("PRINT 0, input", text),
            1 => ("PRINT 0, trim(input)", $"  {text}  "),
            2 => ("PRINT 0, to_string(length(input))", text),
            3 => ("SET values, split(\",\", input)\nPRINT 0, join(\"-\", values)", $"{text},x"),
            _ => ("IF can_parse(input)\nPRINT 0, to_string(parse_int(input) + 1)\nELSE\nPRINT 0, \"0\"\nENDIF", random.Next(1, 100).ToString())
        };
    }

    private static string GenerateArbitrarySource(Random random, int maxLines, int maxTokenLength)
    {
        string[] tokenPool =
        [
            "SET", "PRINT", "IF", "ELSE", "ENDIF", "RETURN", "NOP",
            "input", "value", "x", "0", "1", "\"text\"", ",", "(", ")", "+", "-", "/", "!"
        ];

        int lineCount = random.Next(1, maxLines + 1);
        StringBuilder builder = new();
        for (int line = 0; line < lineCount; line++)
        {
            int tokenCount = random.Next(1, 6);
            List<string> lineTokens = new(tokenCount);
            for (int tokenIndex = 0; tokenIndex < tokenCount; tokenIndex++)
            {
                string token = tokenPool[random.Next(tokenPool.Length)];
                if (token.All(char.IsLetter) && random.NextDouble() < 0.25)
                    token = token[..Math.Min(token.Length, random.Next(1, maxTokenLength + 1))];
                lineTokens.Add(token);
            }

            builder.Append(string.Join(' ', lineTokens));
            if (line < lineCount - 1)
                builder.AppendLine();
        }

        return builder.ToString();
    }
}
