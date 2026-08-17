using BenchmarkDotNet.Running;

namespace NodeScriptBenchmark;

/// <summary>
/// Entry point for the NodeScript benchmark suite.
/// <para>
/// CI-safe run (fast, used by the Benchmark workflow): <c>dotnet run -c Release --project ./NodeScriptBenchmark</c>
/// </para>
/// <para>
/// Full/local run (statistically robust, longer): <c>dotnet run -c Release --project ./NodeScriptBenchmark -- --job Medium</c>
/// </para>
/// <para>
/// Run a subset (e.g. only compile benchmarks): <c>dotnet run -c Release --project ./NodeScriptBenchmark -- --filter *PipelineBenchmarks*</c>
/// </para>
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        // RunAll (rather than Run) runs every benchmark class non-interactively, which keeps the
        // CI workflow's argument-less invocation from blocking on an interactive selection prompt.
        // CLI options such as --job, --filter or --anyCategories are still honored.
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).RunAll(args: args);
    }
}
