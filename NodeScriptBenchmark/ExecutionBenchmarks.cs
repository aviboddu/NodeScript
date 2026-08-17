using BenchmarkDotNet.Attributes;
using NodeScript;

namespace NodeScriptBenchmark;

/// <summary>
/// Benchmarks for cold (first) execution, warm (steady-state) execution, and <see cref="Script.Reset"/>,
/// kept isolated from each other so repeated <c>Run()</c>/<c>Reset()</c> calls don't blur cold-start
/// latency (relevant to puzzle-game interactivity) with tier-1 steady-state throughput (relevant to
/// repeated graph execution).
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class ExecutionBenchmarks
{
    [ParamsSource(nameof(Workloads))]
    public WorkloadArgs Workload { get; set; }

    // CompileError workloads can't execute, so they are excluded here (see PipelineBenchmarks).
    public static IEnumerable<WorkloadArgs> Workloads => WorkloadCatalog.ExecutableWorkloads();

    private string code = string.Empty;
    private string input = string.Empty;

    // Compiled once in GlobalSetup and reset (untimed) before every warm-execution iteration.
    private Script warmScript = null!;

    // Rebuilt and recompiled (untimed) before every cold-execution iteration.
    private Script coldScript = null!;

    // Run once (untimed) before every Reset iteration so there is state to clear.
    private Script scriptToReset = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        (code, input) = WorkloadCatalog.Load(Workload.Kind, Workload.Size);
        warmScript = BuildScript();
        warmScript.CompileNodes();
    }

    [IterationSetup(Targets = [nameof(WarmExecution)])]
    public void ResetWarmScript() => warmScript.Reset();

    [IterationSetup(Targets = [nameof(FirstExecution)])]
    public void SetupColdScript()
    {
        coldScript = BuildScript();
        coldScript.CompileNodes();
    }

    [IterationSetup(Targets = [nameof(Reset)])]
    public void SetupScriptToReset()
    {
        scriptToReset = BuildScript();
        scriptToReset.CompileNodes();
        scriptToReset.Run();
    }

    [Benchmark(Description = "First execution (cold)")]
    public string? FirstExecution()
    {
        coldScript.Run();
        return coldScript.GetOutput();
    }

    [Benchmark(Description = "Warm execution (steady state)")]
    public string? WarmExecution()
    {
        warmScript.Run();
        return warmScript.GetOutput();
    }

    [Benchmark(Description = "Reset")]
    public void Reset() => scriptToReset.Reset();

    private Script BuildScript()
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
