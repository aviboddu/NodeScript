using NodeScript;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Engines;

namespace NodeScriptBenchmark
{
    [SimpleJob(RunStrategy.Throughput)]
    public class Compilation
    {
        private readonly string code;

        public Compilation()
        {
            code = File.ReadAllText("../../../../../../../TestData/LongestString.ns");
        }

        [Benchmark]
        public void Compile()
        {
            RegularNode? node = NodeFactory.CreateRegularNode(code, [], Program.CompileErrorHandler, Program.RuntimeErrorHandler);
        }
    }

    [SimpleJob(RunStrategy.Throughput)]
    public class Execution
    {
        private readonly InputNode input;
        private readonly RegularNode regular;
        private readonly OutputNode output;

        public Execution()
        {
            string input_data = File.ReadAllText("../../../../../../../TestData/LongestString.in");
            string code = File.ReadAllText("../../../../../../../TestData/LongestString.ns");
            output = new();
            regular = NodeFactory.CreateRegularNode(code, [output], Program.CompileErrorHandler, Program.RuntimeErrorHandler)!;
            input = new(input_data, regular);
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
            var summary = BenchmarkRunner.Run<Compilation>();
            summary = BenchmarkRunner.Run<Execution>();
        }

        public static void CompileErrorHandler(int line, string msg)
        {
            Console.Error.WriteLine($"Error at line {line}: {msg}");
        }

        public static void RuntimeErrorHandler(Node node, int line, string msg)
        {
            Console.Error.WriteLine($"Error at line {line}: {msg}");
        }
    }
}