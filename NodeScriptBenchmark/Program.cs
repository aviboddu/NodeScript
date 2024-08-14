using NodeScript;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace NodeScriptBenchmark
{
    [ShortRunJob]
    [JsonExporter(indentJson: true, excludeMeasurements: true)]
    public class Benchmark
    {
        private readonly string code;
        private readonly InputNode input;
        private readonly RegularNode regular;
        private readonly OutputNode output;

        public Benchmark()
        {
            string input_data = File.ReadAllText("../../../../../../../TestData/LongestString.in");
            code = File.ReadAllText("../../../../../../../TestData/LongestString.ns");
            output = new();
            regular = NodeFactory.CreateRegularNode(code, [output], Program.CompileErrorHandler, Program.RuntimeErrorHandler)!;
            input = new(input_data, regular);
        }

        [Benchmark]
        public void Compile()
        {
            RegularNode? node = NodeFactory.CreateRegularNode(code, [], Program.CompileErrorHandler, Program.RuntimeErrorHandler);
        }

        [Benchmark]
        public void Execute()
        {
            while (input.State == NodeState.RUNNING || regular.State == NodeState.RUNNING)
            {
                input.StepLine();
                regular.StepLine();
            }
            input.Reset();
            output.Reset();
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