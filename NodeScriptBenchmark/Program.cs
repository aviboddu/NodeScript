using NodeScript;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace NodeScriptBenchmark
{
    [ShortRunJob]
    public class Benchmark
    {
        private readonly Script script;

        public Benchmark()
        {
            string input_data = File.ReadAllText("../../../../../../../TestData/LongestString.in");
            string code = File.ReadAllText("../../../../../../../TestData/LongestString.ns");
            script = new();
            int input_id = script.AddInputNode(input_data);
            int node_id = script.AddRegularNode(code);
            int output_id = script.AddOutputNode();
            script.ConnectNodes(input_id, node_id);
            script.ConnectNodes(node_id, output_id);
            script.CompileNodes();
        }

        [Benchmark]
        public void Compile()
        {
            script.CompileNodes();
        }

        [Benchmark]
        public void Execute()
        {
            script.Run();
            script.Reset();
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<Benchmark>();
        }

        public static void CompileErrorHandler(int line, string msg)
        {
            Console.Error.WriteLine($"Error at line {line}: {msg}");
        }

        public static void RuntimeErrorHandler(Node _, int line, string msg)
        {
            Console.Error.WriteLine($"Error at line {line}: {msg}");
        }
    }
}