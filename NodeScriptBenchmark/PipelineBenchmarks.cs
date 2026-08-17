using BenchmarkDotNet.Attributes;
using NodeScript;

namespace NodeScriptBenchmark;

/// <summary>
/// Benchmarks for each phase of the compile pipeline: tokenize/parse, optimize/validate/compile,
/// and the full cold compile of a single node and of a brand-new <see cref="Script"/>.
/// <para>
/// Setup work (loading source text, tokenizing/parsing for the compile-only phase, and building a
/// fresh <see cref="Script"/>) is performed in <see cref="GlobalSetup"/>/<see cref="IterationSetupAttribute"/>
/// methods so it is not accidentally included in the measured time.
/// </para>
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class PipelineBenchmarks
{
    [ParamsSource(nameof(Workloads))]
    public WorkloadArgs Workload { get; set; }

    public static IEnumerable<WorkloadArgs> Workloads => WorkloadCatalog.AllWorkloads();

    private string code = string.Empty;
    private string input = string.Empty;

    // Pre-parsed state for the OptimizeValidateCompile benchmark; rebuilt fresh every iteration
    // so the optimizer/validator/compiler always operate on unmodified operations.
    private Token[][] parsedTokens = [];
    private Operation?[] parsedOperations = [];

    [GlobalSetup]
    public void GlobalSetup()
    {
        (code, input) = WorkloadCatalog.Load(Workload.Kind, Workload.Size);
    }

    [IterationSetup(Targets = [nameof(OptimizeValidateCompile)])]
    public void SetupParsedOperations()
    {
        Tokenizer tokenizer = new(code, WorkloadCatalog.IgnoreInternalError);
        parsedTokens = tokenizer.ScanTokens();
        Parser parser = new(parsedTokens, WorkloadCatalog.IgnoreInternalError);
        parsedOperations = parser.Parse();
    }

    [IterationSetup(Targets = [nameof(CompileScriptCold)])]
    public void SetupFreshScript()
    {
        scriptForCompile = BuildUncompiledScript();
    }

    private Script scriptForCompile = null!;

    [Benchmark(Description = "Tokenize + Parse")]
    public int TokenizeAndParse()
    {
        Tokenizer tokenizer = new(code, WorkloadCatalog.IgnoreInternalError);
        Token[][] tokens = tokenizer.ScanTokens();
        Parser parser = new(tokens, WorkloadCatalog.IgnoreInternalError);
        Operation?[] operations = parser.Parse();
        return operations.Length;
    }

    [Benchmark(Description = "Optimize + Validate + Compile")]
    public int OptimizeValidateCompile()
    {
        Optimizer.PropogateConstants(parsedOperations, WorkloadCatalog.IgnoreInternalError);
        Validator.Validate(parsedOperations, WorkloadCatalog.IgnoreInternalError);
        Compiler compiler = new(parsedOperations, WorkloadCatalog.IgnoreInternalError);
        Compiler.CompiledData data = compiler.Compile();
        return data.Code.Length;
    }

    [Benchmark(Description = "Cold compile (single node)")]
    public bool CompileNodeCold()
    {
        RegularNode? node = NodeFactory.CreateRegularNode(code, WorkloadCatalog.IgnoreInternalError, WorkloadCatalog.IgnoreInternalError);
        return node is not null;
    }

    [Benchmark(Description = "Cold compile (full script)")]
    public bool CompileScriptCold() => scriptForCompile.CompileNodes();

    private Script BuildUncompiledScript()
    {
        Script script = new(WorkloadCatalog.IgnoreError, WorkloadCatalog.IgnoreError);
        int inputId = script.AddInputNode(input);
        int nodeId = script.AddRegularNode(code);
        int outputId = script.AddOutputNode();
        script.ConnectNodes(inputId, nodeId);
        script.ConnectNodes(nodeId, outputId);
        return script;
    }
}
