using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;

namespace NodeScriptBenchmark;

/// <summary>
/// Shared configuration for every benchmark class.
/// <para>
/// The default job (<c>Ci</c>) is intentionally short so that the benchmark suite stays fast
/// enough to run on every pull request. For a statistically robust local/full run, override the
/// job from the command line, e.g.:
/// <c>dotnet run -c Release --project ./NodeScriptBenchmark -- --job Medium</c>
/// </para>
/// Every run reports managed allocations and GC collections via <see cref="MemoryDiagnoser"/>, and
/// BenchmarkDotNet always records the host environment (runtime, architecture, GC mode, tiered
/// compilation and ReadyToRun settings) alongside the results.
/// </summary>
public sealed class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddJob(Job.ShortRun.WithId("Ci"));
        AddDiagnoser(MemoryDiagnoser.Default);
        AddColumnProvider(BenchmarkDotNet.Columns.DefaultColumnProviders.Instance);
    }
}
