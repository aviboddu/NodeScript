using BenchmarkDotNet.Attributes;
using NodeScript;

namespace NodeScriptBenchmark;

/// <summary>
/// Benchmarks for complete graph runs across different topologies: linear chains, fan-out, fan-in
/// (via a combiner), disconnected/unreachable nodes, and an illegal cycle (which is expected to fail
/// compilation, since <see cref="Script.CompileNodes"/> rejects graphs that contain one).
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class GraphShapeBenchmarks
{
    [ParamsSource(nameof(AllShapes))]
    public GraphShape CompileShape { get; set; }

    public static IEnumerable<GraphShape> AllShapes => GraphShapeBuilder.AllShapes();

    private Script scriptToCompile = null!;

    [IterationSetup(Targets = [nameof(CompileGraph)])]
    public void SetupUncompiledGraph()
    {
        scriptToCompile = GraphShapeBuilder.Build(CompileShape);
    }

    [Benchmark(Description = "Compile graph")]
    public bool CompileGraph() => scriptToCompile.CompileNodes();
}

/// <summary>
/// Complete (warm) run of each compilable graph shape. Kept separate from
/// <see cref="GraphShapeBenchmarks"/> because the illegal <see cref="GraphShape.Cycle"/> shape can
/// never be run.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class GraphShapeRunBenchmarks
{
    [ParamsSource(nameof(RunnableShapes))]
    public GraphShape RunShape { get; set; }

    public static IEnumerable<GraphShape> RunnableShapes => GraphShapeBuilder.CompilableShapes();

    private Script script = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        script = GraphShapeBuilder.Build(RunShape);
        script.CompileNodes();
    }

    [IterationSetup]
    public void ResetScript() => script.Reset();

    [Benchmark(Description = "Run graph (warm)")]
    public string? RunGraph()
    {
        script.Run();
        return script.GetOutput();
    }
}
