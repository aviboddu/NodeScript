using NodeScript;

namespace NodeScriptBenchmark;

/// <summary>
/// Code shapes used to build representative benchmark inputs. Each kind is checked into
/// <c>TestData/&lt;Kind&gt;</c> as a deterministic <c>.ns</c>/<c>.in</c> pair per <see cref="WorkloadSize"/>.
/// </summary>
public enum WorkloadKind
{
    /// <summary>Straight-line integer arithmetic.</summary>
    Arithmetic,

    /// <summary>Branch-heavy code using IF/ELSE/ENDIF.</summary>
    Branch,

    /// <summary>Chains of native function calls (split, join, slice, etc.).</summary>
    Native,

    /// <summary>String/array indexing and slicing using NodeScript's built-in syntax.</summary>
    StringArray,

    /// <summary>Code that fails during compilation (references an undefined variable).</summary>
    CompileError,

    /// <summary>Code that compiles successfully but fails during execution (divide by zero).</summary>
    RuntimeError,
}

/// <summary>Relative size of a workload's source, from a single line to the largest loop-free program checked in.</summary>
public enum WorkloadSize
{
    Tiny,
    Medium,
    Max,
}

/// <summary>Loads the deterministic, checked-in NodeScript source/input pairs used by the benchmarks.</summary>
internal static class WorkloadCatalog
{
    private static readonly string TestDataRoot = FindTestDataRoot();

    /// <summary>All (kind, size) combinations, used as a shared BenchmarkDotNet <c>ParamsSource</c>.</summary>
    public static IEnumerable<WorkloadArgs> AllWorkloads()
    {
        foreach (WorkloadKind kind in Enum.GetValues<WorkloadKind>())
            foreach (WorkloadSize size in Enum.GetValues<WorkloadSize>())
                yield return new WorkloadArgs(kind, size);
    }

    /// <summary>Workloads that compile successfully, i.e. everything except <see cref="WorkloadKind.CompileError"/>.</summary>
    public static IEnumerable<WorkloadArgs> ExecutableWorkloads() =>
        AllWorkloads().Where(w => w.Kind != WorkloadKind.CompileError);

    public static (string Code, string Input) Load(WorkloadKind kind, WorkloadSize size)
    {
        string dir = Path.Combine(TestDataRoot, kind.ToString());
        string code = File.ReadAllText(Path.Combine(dir, $"{size}.ns"));
        string input = File.ReadAllText(Path.Combine(dir, $"{size}.in"));
        return (code, input);
    }

    private static string FindTestDataRoot()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            string candidate = Path.Combine(dir, "TestData");
            if (Directory.Exists(candidate))
                return candidate;
            dir = Path.GetDirectoryName(dir);
        }
        throw new DirectoryNotFoundException("Could not locate the NodeScriptBenchmark TestData directory.");
    }

    /// <summary>Public error handler that swallows errors. Used where errors are expected and not the subject of the benchmark.</summary>
    public static void IgnoreError(int nodeId, int line, string message) { }

    /// <summary>Internal (per-node) error handler that swallows errors, used by the tokenizer/parser/optimizer/validator/compiler.</summary>
    internal static void IgnoreInternalError(int line, string message) { }
}

/// <summary>A single (kind, size) benchmark parameter. Overrides <see cref="ToString"/> for readable BenchmarkDotNet reports.</summary>
public readonly record struct WorkloadArgs(WorkloadKind Kind, WorkloadSize Size)
{
    public override string ToString() => $"{Kind}/{Size}";
}

/// <summary>The shape of the node graph, independent of the code each node runs.</summary>
public enum GraphShape
{
    /// <summary>input -> n0 -> n1 -> n2 -> n3 -> output</summary>
    Linear,

    /// <summary>input -> n0 -> {n1, n2, n3} -> output (each branch writes directly to the output node).</summary>
    FanOut,

    /// <summary>input -> n0 -> {n1, n2, n3} -> combiner -> output</summary>
    FanIn,

    /// <summary>A linear chain plus extra nodes that are never connected to the input.</summary>
    Disconnected,

    /// <summary>input -> n0 -> n1 -> n2 -> n0 (illegal cycle; expected to fail compilation).</summary>
    Cycle,
}

/// <summary>Builds <see cref="Script"/> graphs with a fixed, trivial passthrough node so only the graph topology varies.</summary>
internal static class GraphShapeBuilder
{
    private const string PassthroughCode = "PRINT 0, input";
    private const string Input = "line";

    /// <summary>Shapes for which the graph is legal and can be both compiled and run.</summary>
    public static IEnumerable<GraphShape> CompilableShapes() =>
        Enum.GetValues<GraphShape>().Where(s => s != GraphShape.Cycle);

    /// <summary>All shapes, including the illegal cycle, for compile-only benchmarks.</summary>
    public static IEnumerable<GraphShape> AllShapes() => Enum.GetValues<GraphShape>();

    public static Script Build(GraphShape shape)
    {
        Script script = new(WorkloadCatalog.IgnoreError, WorkloadCatalog.IgnoreError);
        int inputId = script.AddInputNode(Input);

        switch (shape)
        {
            case GraphShape.Linear:
                {
                    int outputId = script.AddOutputNode();
                    int prev = inputId;
                    for (int i = 0; i < 4; i++)
                    {
                        int node = script.AddRegularNode(PassthroughCode);
                        script.ConnectNodes(prev, node);
                        prev = node;
                    }
                    script.ConnectNodes(prev, outputId);
                    break;
                }
            case GraphShape.FanOut:
                {
                    int outputId = script.AddOutputNode();
                    int root = script.AddRegularNode(PassthroughCode);
                    script.ConnectNodes(inputId, root);
                    for (int i = 0; i < 3; i++)
                    {
                        int branch = script.AddRegularNode(PassthroughCode);
                        script.ConnectNodes(root, branch);
                        script.ConnectNodes(branch, outputId);
                    }
                    break;
                }
            case GraphShape.FanIn:
                {
                    int outputId = script.AddOutputNode();
                    int combinerId = script.AddCombinerNode();
                    int root = script.AddRegularNode(PassthroughCode);
                    script.ConnectNodes(inputId, root);
                    for (int i = 0; i < 3; i++)
                    {
                        int branch = script.AddRegularNode(PassthroughCode);
                        script.ConnectNodes(root, branch);
                        script.ConnectNodes(branch, combinerId);
                    }
                    script.ConnectNodes(combinerId, outputId);
                    break;
                }
            case GraphShape.Disconnected:
                {
                    int outputId = script.AddOutputNode();
                    int prev = inputId;
                    for (int i = 0; i < 4; i++)
                    {
                        int node = script.AddRegularNode(PassthroughCode);
                        script.ConnectNodes(prev, node);
                        prev = node;
                    }
                    script.ConnectNodes(prev, outputId);

                    // Extra nodes that are never wired to the input; must not affect compile/execution.
                    for (int i = 0; i < 3; i++)
                        script.AddRegularNode(PassthroughCode);
                    break;
                }
            case GraphShape.Cycle:
                {
                    int outputId = script.AddOutputNode();
                    int n0 = script.AddRegularNode(PassthroughCode);
                    int n1 = script.AddRegularNode(PassthroughCode);
                    int n2 = script.AddRegularNode(PassthroughCode);
                    script.ConnectNodes(inputId, n0);
                    script.ConnectNodes(n0, n1);
                    script.ConnectNodes(n1, n2);
                    script.ConnectNodes(n2, n0); // Creates the cycle n0 -> n1 -> n2 -> n0
                    script.ConnectNodes(n2, outputId);
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(shape));
        }

        return script;
    }
}
