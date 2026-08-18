# NodeScript
![Tests](https://github.com/aviboddu/NodeScript/actions/workflows/dotnet.yml/badge.svg?branch=master&event=push) ![Benchmark](https://github.com/aviboddu/NodeScript/actions/workflows/benchmark.yml/badge.svg?branch=master&event=push) ![NuGet Version](https://img.shields.io/nuget/v/NodeScript) ![Benchmark Performance](https://img.shields.io/endpoint?url=https%3A%2F%2Fraw.githubusercontent.com%2Faviboddu%2FNodeScript%2Fbadges%2Fbenchmark.json)

NodeScript is a rudimentary programming language designed to function on 'nodes', intended for use in puzzle games. Find more details [here](https://github.com/aviboddu/NodeScript/wiki)

## Nodes
- Input nodes will continuously attempt to send the next line. It has only one output.
- Regular nodes can take in one string at a time, process it and send it to a set number of outputs. Each node can store a single string in `mem`. `mem` is the only variable which will persist between executions.
- Combiner nodes can merge multiple inputs into one output. It offers no options to choose which string goes through first, picking whatever comes first.
- Output nodes will consume the lines sent to it, storing it in a string.

## Features
- Basic arithmetic and boolean logic
- Variables - All variables are global in scope
- Indexing
    - Element_of: `v[i]`
    - Slice: `v[i:j]`
- Basic control flow (if-else)
- Native functions for things like:
    - String manipulation
    - Data conversion and parsing
- Dynamic typing between:
    - string
    - string[]
    - int
    - bool

Notably, there are **NO** loops within the scripting itself. No while. No for.
Execution will occur line by line and will only start when a node receives an input string to be processed.

## Syntax
Every line contains a single statement. All statements will start with a relevant keyword for the operation.
- SET: Sets a variable to a certain value. Variables do not need to be declared. Syntax: `SET <variable_name>, <expression>`
- PRINT: Sends a string to a specific output node, denoted by an index. Syntax: `PRINT <output_idx>, <expression>`
- RETURN: Ends the program (until the next input comes). Syntax: `RETURN`
- IF: Executes the following code if the given expression is true. Syntax `IF <expression>`
- ELSE: Executes the following code if the previous if statement was false. Syntax `ELSE`
- ENDIF: Marks the end of the IF clause. Either ends the IF code section or the ELSE code section. Only one is needed per IF/ELSE statement. Syntax `ENDIF`
- NOP: Does nothing. Helpful for synchronizing the timing of multiple nodes. Syntax `NOP`

## Benchmarks
`NodeScriptBenchmark` measures each phase of the compile pipeline (tokenize/parse, optimize/validate/compile),
cold vs. warm execution, `Reset`, and complete graph runs across several representative workloads
(straight-line arithmetic, branch-heavy code, native calls, string/array operations, compile errors, and
runtime errors, each in tiny/medium/max sizes) and graph shapes (linear, fan-out, fan-in, disconnected, and
cycle). All benchmark inputs are deterministic and checked into `NodeScriptBenchmark/TestData`. Results report
mean/median, throughput, allocated bytes and GC collections via `MemoryDiagnoser`, alongside the host runtime,
architecture and GC settings that BenchmarkDotNet records automatically. A baseline run is recorded in
[`NodeScriptBenchmark/Baseline/README.md`](NodeScriptBenchmark/Baseline/README.md).

The **Benchmark Performance** badge at the top of this file shows the headline numbers of the latest
benchmark run on `master`: the geometric mean of every benchmark's mean execution time and of the
allocated bytes per operation. It is produced by
[`.github/scripts/benchmark_badge.py`](.github/scripts/benchmark_badge.py), which summarizes the JSON
reports emitted by BenchmarkDotNet, and the resulting [shields.io endpoint](https://shields.io/badges/endpoint-badge)
payload is committed to the `badges` branch by the Benchmark workflow. Since the numbers come from a
shared CI runner they are only meaningful as a trend, not as an absolute measurement.

- CI-safe subset (fast, used by the Benchmark workflow): `dotnet run -c Release --project ./NodeScriptBenchmark`
- Full/local run (more iterations, statistically robust): `dotnet run -c Release --project ./NodeScriptBenchmark -- --job Medium`
- A subset of benchmarks: `dotnet run -c Release --project ./NodeScriptBenchmark -- --filter *PipelineBenchmarks*`

Development and CI details are documented in the [wiki](https://github.com/aviboddu/NodeScript/wiki).
