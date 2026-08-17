# Benchmark Baseline

Recorded before any runtime optimization work, as a reference point for future regressions/improvements.

- Commit: `513524d15b5bb0a10be95716d34a7e013b2b9818`

- Date (UTC): `2026-08-17`

- Command: `dotnet run -c Release --project ./NodeScriptBenchmark` (default CI-safe `Ci` job)

- Machine: GitHub-hosted CI runner (results will vary by hardware; re-baseline on the actual CI runner when comparing).


See [`README.md`](../README.md#benchmarks) for how to reproduce this run or run the full/local suite.


## Pipeline (tokenize/parse, optimize/validate/compile, cold compile)

```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.302
  [Host] : .NET 9.0.18 (9.0.1826.31522), X64 RyuJIT AVX2
  Ci     : .NET 9.0.18 (9.0.1826.31522), X64 RyuJIT AVX2

Job=Ci  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | InvocationCount | UnrollFactor | Workload            | Mean           | Error            | StdDev          | Median         | Gen0     | Gen1    | Allocated  |
|-------------------------------- |---------------- |------------- |-------------------- |---------------:|-----------------:|----------------:|---------------:|---------:|--------:|-----------:|
| **&#39;Tokenize + Parse&#39;**              | **Default**         | **16**           | **Arithmetic/Max**      |   **439,526.8 ns** |     **27,396.13 ns** |     **1,501.67 ns** |   **439,548.6 ns** |  **70.8008** | **46.8750** | **1160.05 KB** |
| &#39;Cold compile (single node)&#39;    | Default         | 16           | Arithmetic/Max      |   835,210.7 ns |     46,445.41 ns |     2,545.83 ns |   836,585.8 ns |  92.7734 | 49.8047 | 1518.99 KB |
| &#39;Optimize + Validate + Compile&#39; | 1               | 1            | Arithmetic/Max      | 2,520,867.3 ns | 54,555,262.03 ns | 2,990,358.01 ns |   797,866.0 ns |        - |       - |  354.95 KB |
| &#39;Cold compile (full script)&#39;    | 1               | 1            | Arithmetic/Max      | 5,350,263.7 ns | 56,066,249.19 ns | 3,073,180.32 ns | 3,584,169.0 ns |        - |       - | 1610.25 KB |
| **&#39;Tokenize + Parse&#39;**              | **Default**         | **16**           | **Arithmetic/Medium**   |    **56,107.5 ns** |      **2,177.45 ns** |       **119.35 ns** |    **56,169.1 ns** |   **9.7046** |  **1.3428** |   **158.8 KB** |
| &#39;Cold compile (single node)&#39;    | Default         | 16           | Arithmetic/Medium   |   104,068.2 ns |      8,168.87 ns |       447.76 ns |   104,270.0 ns |  12.8174 |  1.5869 |  209.43 KB |
| &#39;Optimize + Validate + Compile&#39; | 1               | 1            | Arithmetic/Medium   |   159,614.7 ns |     86,183.15 ns |     4,723.99 ns |   161,401.0 ns |        - |       - |   50.71 KB |
| &#39;Cold compile (full script)&#39;    | 1               | 1            | Arithmetic/Medium   |   544,284.0 ns |    362,323.99 ns |    19,860.20 ns |   542,781.0 ns |        - |       - |  227.56 KB |
| **&#39;Tokenize + Parse&#39;**              | **Default**         | **16**           | **Arithmetic/Tiny**     |     **4,495.5 ns** |        **465.42 ns** |        **25.51 ns** |     **4,482.8 ns** |   **0.7629** |  **0.0076** |   **12.55 KB** |
| &#39;Cold compile (single node)&#39;    | Default         | 16           | Arithmetic/Tiny     |     9,634.1 ns |      3,302.45 ns |       181.02 ns |     9,541.9 ns |   1.1444 |       - |    18.8 KB |
| &#39;Optimize + Validate + Compile&#39; | 1               | 1            | Arithmetic/Tiny     |    35,255.7 ns |     87,125.94 ns |     4,775.67 ns |    34,094.0 ns |        - |       - |    6.93 KB |
| &#39;Cold compile (full script)&#39;    | 1               | 1            | Arithmetic/Tiny     |   114,740.7 ns |     22,485.12 ns |     1,232.49 ns |   114,153.0 ns |        - |       - |   26.41 KB |
| **&#39;Tokenize + Parse&#39;**              | **Default**         | **16**           | **Branch/Max**          |   **228,376.7 ns** |     **25,007.72 ns** |     **1,370.76 ns** |   **228,155.7 ns** |  **35.4004** | **15.3809** |   **581.8 KB** |
| &#39;Cold compile (single node)&#39;    | Default         | 16           | Branch/Max          |   467,838.1 ns |     55,500.57 ns |     3,042.17 ns |   468,367.3 ns |  54.6875 | 22.9492 |   894.2 KB |
| &#39;Optimize + Validate + Compile&#39; | 1               | 1            | Branch/Max          |   600,368.3 ns |    208,101.33 ns |    11,406.74 ns |   602,642.0 ns |        - |       - |  311.53 KB |
| &#39;Cold compile (full script)&#39;    | 1               | 1            | Branch/Max          | 2,128,078.8 ns |    110,658.24 ns |     6,065.55 ns | 2,126,211.5 ns |        - |       - |  943.41 KB |
| **&#39;Tokenize + Parse&#39;**              | **Default**         | **16**           | **Branch/Medium**       |    **35,630.6 ns** |     **10,987.90 ns** |       **602.28 ns** |    **35,876.2 ns** |   **5.6763** |  **0.6104** |   **93.35 KB** |
| &#39;Cold compile (single node)&#39;    | Default         | 16           | Branch/Medium       |    73,765.1 ns |      3,134.00 ns |       171.79 ns |    73,747.5 ns |   8.6670 |  0.7324 |  143.46 KB |
| &#39;Optimize + Validate + Compile&#39; | 1               | 1            | Branch/Medium       |   139,280.0 ns |    307,991.02 ns |    16,882.03 ns |   141,704.0 ns |        - |       - |   50.28 KB |
| &#39;Cold compile (full script)&#39;    | 1               | 1            | Branch/Medium       |   398,411.7 ns |    620,494.61 ns |    34,011.40 ns |   396,328.0 ns |        - |       - |  156.81 KB |
| **&#39;Tokenize + Parse&#39;**              | **Default**         | **16**           | **Branch/Tiny**         |     **4,649.6 ns** |        **682.34 ns** |        **37.40 ns** |     **4,655.1 ns** |   **0.7401** |  **0.0076** |   **12.16 KB** |
| &#39;Cold compile (single node)&#39;    | Default         | 16           | Branch/Tiny         |    10,905.3 ns |      1,746.06 ns |        95.71 ns |    10,907.4 ns |   1.2360 |  0.0153 |   20.34 KB |
| &#39;Optimize + Validate + Compile&#39; | 1               | 1            | Branch/Tiny         |    45,818.3 ns |    229,418.83 ns |    12,575.22 ns |    39,473.0 ns |        - |       - |    8.85 KB |
| &#39;Cold compile (full script)&#39;    | 1               | 1            | Branch/Tiny         |   134,323.0 ns |     33,350.57 ns |     1,828.06 ns |   134,019.0 ns |        - |       - |    27.5 KB |
| **&#39;Tokenize + Parse&#39;**              | **Default**         | **16**           | **CompileError/Max**    |   **451,832.3 ns** |     **66,600.54 ns** |     **3,650.60 ns** |   **452,668.4 ns** |  **70.8008** | **48.3398** | **1159.14 KB** |
| &#39;Cold compile (single node)&#39;    | Default         | 16           | CompileError/Max    |   669,294.1 ns |     34,059.60 ns |     1,866.92 ns |   668,998.7 ns |  78.1250 | 45.8984 | 1282.32 KB |
| &#39;Optimize + Validate + Compile&#39; | 1               | 1            | CompileError/Max    | 2,593,379.0 ns | 56,871,275.61 ns | 3,117,306.53 ns |   799,069.0 ns |        - |       - |  354.55 KB |
| &#39;Cold compile (full script)&#39;    | 1               | 1            | CompileError/Max    | 4,112,070.3 ns | 40,084,749.97 ns | 2,197,180.41 ns | 2,871,128.0 ns |        - |       - | 1371.16 KB |
| **&#39;Tokenize + Parse&#39;**              | **Default**         | **16**           | **CompileError/Medium** |    **57,330.5 ns** |      **6,397.38 ns** |       **350.66 ns** |    **57,509.8 ns** |   **9.6436** |  **1.3428** |  **157.88 KB** |
| &#39;Cold compile (single node)&#39;    | Default         | 16           | CompileError/Medium |    80,052.1 ns |      3,970.54 ns |       217.64 ns |    79,998.9 ns |  10.6201 |  1.4648 |  175.44 KB |
| &#39;Optimize + Validate + Compile&#39; | 1               | 1            | CompileError/Medium |   164,593.0 ns |     74,729.84 ns |     4,096.19 ns |   165,147.0 ns |        - |       - |   50.31 KB |
| &#39;Cold compile (full script)&#39;    | 1               | 1            | CompileError/Medium |   365,434.2 ns |    134,974.77 ns |     7,398.42 ns |   369,498.5 ns |        - |       - |  191.16 KB |
| **&#39;Tokenize + Parse&#39;**              | **Default**         | **16**           | **CompileError/Tiny**   |       **652.5 ns** |         **71.18 ns** |         **3.90 ns** |       **654.0 ns** |   **0.1011** |       **-** |    **1.66 KB** |
| &#39;Cold compile (single node)&#39;    | Default         | 16           | CompileError/Tiny   |     1,116.3 ns |        273.46 ns |        14.99 ns |     1,116.1 ns |   0.1507 |       - |    2.49 KB |
| &#39;Optimize + Validate + Compile&#39; | 1               | 1            | CompileError/Tiny   |    13,511.3 ns |      8,182.49 ns |       448.51 ns |    13,304.0 ns |        - |       - |     2.2 KB |
| &#39;Cold compile (full script)&#39;    | 1               | 1            | CompileError/Tiny   |    47,068.3 ns |     86,422.85 ns |     4,737.13 ns |    45,335.0 ns |        - |       - |    6.27 KB |
| **&#39;Tokenize + Parse&#39;**              | **Default**         | **16**           | **Native/Max**          |   **422,701.4 ns** |    **141,266.86 ns** |     **7,743.31 ns** |   **418,313.8 ns** |  **68.8477** | **34.6680** | **1126.01 KB** |
| &#39;Cold compile (single node)&#39;    | Default         | 16           | Native/Max          |   975,038.0 ns |    255,371.36 ns |    13,997.77 ns |   979,031.9 ns | 100.5859 | 54.6875 | 1652.63 KB |
| &#39;Optimize + Validate + Compile&#39; | 1               | 1            | Native/Max          | 1,092,823.0 ns |    222,943.95 ns |    12,220.31 ns | 1,092,335.0 ns |        - |       - |  524.13 KB |
| &#39;Cold compile (full script)&#39;    | 1               | 1            | Native/Max          | 4,263,645.0 ns |  2,716,918.19 ns |   148,923.45 ns | 4,184,063.0 ns |        - |       - | 1763.37 KB |
| **&#39;Tokenize + Parse&#39;**              | **Default**         | **16**           | **Native/Medium**       |    **50,689.4 ns** |      **4,466.97 ns** |       **244.85 ns** |    **50,659.9 ns** |   **8.6670** |  **1.0376** |  **142.22 KB** |
| &#39;Cold compile (single node)&#39;    | Default         | 16           | Native/Medium       |   110,062.6 ns |     24,697.91 ns |     1,353.78 ns |   109,510.0 ns |  12.9395 |  1.4648 |  211.75 KB |
| &#39;Optimize + Validate + Compile&#39; | 1               | 1            | Native/Medium       |   177,944.3 ns |    100,329.95 ns |     5,499.42 ns |   176,869.0 ns |        - |       - |   69.78 KB |
| &#39;Cold compile (full script)&#39;    | 1               | 1            | Native/Medium       |   516,308.7 ns |    395,404.41 ns |    21,673.45 ns |   526,912.0 ns |        - |       - |   231.4 KB |
| **&#39;Tokenize + Parse&#39;**              | **Default**         | **16**           | **Native/Tiny**         |    **10,787.1 ns** |      **2,625.08 ns** |       **143.89 ns** |    **10,821.2 ns** |   **1.8158** |  **0.0458** |    **29.8 KB** |
| &#39;Cold compile (single node)&#39;    | Default         | 16           | Native/Tiny         |    24,597.1 ns |      2,925.79 ns |       160.37 ns |    24,539.4 ns |   2.8076 |  0.0610 |   46.21 KB |
| &#39;Optimize + Validate + Compile&#39; | 1               | 1            | Native/Tiny         |    60,316.7 ns |    135,168.35 ns |     7,409.03 ns |    56,400.0 ns |        - |       - |    16.7 KB |
| &#39;Cold compile (full script)&#39;    | 1               | 1            | Native/Tiny         |   176,420.0 ns |     94,199.41 ns |     5,163.39 ns |   177,355.0 ns |        - |       - |   55.45 KB |
| **&#39;Tokenize + Parse&#39;**              | **Default**         | **16**           | **RuntimeError/Max**    |   **451,095.5 ns** |    **160,742.33 ns** |     **8,810.83 ns** |   **451,928.4 ns** |  **70.8008** | **49.8047** | **1162.08 KB** |
| &#39;Cold compile (single node)&#39;    | Default         | 16           | RuntimeError/Max    |   890,719.8 ns |    171,769.37 ns |     9,415.26 ns |   891,060.3 ns |  92.7734 | 49.8047 | 1522.17 KB |
| &#39;Optimize + Validate + Compile&#39; | 1               | 1            | RuntimeError/Max    | 2,575,051.2 ns | 56,028,716.58 ns | 3,071,123.03 ns |   821,690.5 ns |        - |       - |  356.07 KB |
| &#39;Cold compile (full script)&#39;    | 1               | 1            | RuntimeError/Max    | 5,417,922.3 ns | 55,748,812.98 ns | 3,055,780.57 ns | 3,679,326.0 ns |        - |       - | 1613.56 KB |
| **&#39;Tokenize + Parse&#39;**              | **Default**         | **16**           | **RuntimeError/Medium** |    **57,900.5 ns** |     **17,129.53 ns** |       **938.93 ns** |    **58,368.4 ns** |   **9.8267** |  **1.4038** |  **160.82 KB** |
| &#39;Cold compile (single node)&#39;    | Default         | 16           | RuntimeError/Medium |   104,426.6 ns |      6,397.88 ns |       350.69 ns |   104,469.1 ns |  12.9395 |  1.7090 |   212.6 KB |
| &#39;Optimize + Validate + Compile&#39; | 1               | 1            | RuntimeError/Medium |   181,479.0 ns |    258,946.34 ns |    14,193.72 ns |   177,672.0 ns |        - |       - |   51.83 KB |
| &#39;Cold compile (full script)&#39;    | 1               | 1            | RuntimeError/Medium |   514,949.3 ns |     72,018.50 ns |     3,947.58 ns |   516,963.0 ns |        - |       - |  230.88 KB |
| **&#39;Tokenize + Parse&#39;**              | **Default**         | **16**           | **RuntimeError/Tiny**   |     **2,187.2 ns** |        **562.72 ns** |        **30.84 ns** |     **2,194.1 ns** |   **0.3586** |       **-** |    **5.91 KB** |
| &#39;Cold compile (single node)&#39;    | Default         | 16           | RuntimeError/Tiny   |     5,149.4 ns |        700.83 ns |        38.41 ns |     5,157.8 ns |   0.6409 |       - |   10.52 KB |
| &#39;Optimize + Validate + Compile&#39; | 1               | 1            | RuntimeError/Tiny   |    28,115.3 ns |     86,569.65 ns |     4,745.17 ns |    26,389.0 ns |        - |       - |    5.28 KB |
| &#39;Cold compile (full script)&#39;    | 1               | 1            | RuntimeError/Tiny   |   103,881.5 ns |    394,764.26 ns |    21,638.36 ns |    91,715.5 ns |        - |       - |   17.14 KB |
| **&#39;Tokenize + Parse&#39;**              | **Default**         | **16**           | **StringArray/Max**     |   **262,941.2 ns** |     **44,741.11 ns** |     **2,452.41 ns** |   **263,057.7 ns** |  **40.5273** | **19.0430** |  **663.23 KB** |
| &#39;Cold compile (single node)&#39;    | Default         | 16           | StringArray/Max     |   522,671.7 ns |    267,030.55 ns |    14,636.85 ns |   529,436.6 ns |  56.6406 | 21.4844 |  939.75 KB |
| &#39;Optimize + Validate + Compile&#39; | 1               | 1            | StringArray/Max     |   766,625.7 ns |  3,809,430.14 ns |   208,807.72 ns |   660,952.0 ns |        - |       - |  274.38 KB |
| &#39;Cold compile (full script)&#39;    | 1               | 1            | StringArray/Max     | 2,979,959.3 ns | 17,496,755.59 ns |   959,056.22 ns | 2,469,892.0 ns |        - |       - | 1005.46 KB |
| **&#39;Tokenize + Parse&#39;**              | **Default**         | **16**           | **StringArray/Medium**  |    **31,531.9 ns** |      **3,386.60 ns** |       **185.63 ns** |    **31,553.5 ns** |   **5.1270** |  **0.3662** |   **84.35 KB** |
| &#39;Cold compile (single node)&#39;    | Default         | 16           | StringArray/Medium  |    61,553.3 ns |      4,844.32 ns |       265.53 ns |    61,446.7 ns |   7.3242 |  0.4883 |  121.64 KB |
| &#39;Optimize + Validate + Compile&#39; | 1               | 1            | StringArray/Medium  |   114,477.3 ns |     32,213.51 ns |     1,765.73 ns |   114,293.0 ns |        - |       - |   37.33 KB |
| &#39;Cold compile (full script)&#39;    | 1               | 1            | StringArray/Medium  |   387,435.3 ns |    292,474.38 ns |    16,031.51 ns |   379,617.0 ns |        - |       - |  135.67 KB |
| **&#39;Tokenize + Parse&#39;**              | **Default**         | **16**           | **StringArray/Tiny**    |     **6,632.3 ns** |      **1,048.11 ns** |        **57.45 ns** |     **6,657.5 ns** |   **1.1063** |  **0.0153** |   **18.18 KB** |
| &#39;Cold compile (single node)&#39;    | Default         | 16           | StringArray/Tiny    |    15,390.8 ns |      2,688.76 ns |       147.38 ns |    15,444.3 ns |   1.7090 |       - |    28.3 KB |
| &#39;Optimize + Validate + Compile&#39; | 1               | 1            | StringArray/Tiny    |    53,228.3 ns |    141,374.70 ns |     7,749.22 ns |    49,338.0 ns |        - |       - |   10.72 KB |
| &#39;Cold compile (full script)&#39;    | 1               | 1            | StringArray/Tiny    |   142,793.3 ns |     91,732.73 ns |     5,028.18 ns |   140,712.0 ns |        - |       - |   36.42 KB |


## Execution (first execution, warm execution, reset)

```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.302
  [Host] : .NET 9.0.18 (9.0.1826.31522), X64 RyuJIT AVX2
  Ci     : .NET 9.0.18 (9.0.1826.31522), X64 RyuJIT AVX2

Job=Ci  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                          | Workload            | Mean       | Error       | StdDev     | Median     | Allocated |
|-------------------------------- |-------------------- |-----------:|------------:|-----------:|-----------:|----------:|
| **&#39;First execution (cold)&#39;**        | **Arithmetic/Max**      | **179.266 μs** | **282.7155 μs** | **15.4966 μs** | **172.377 μs** |   **15960 B** |
| &#39;Warm execution (steady state)&#39; | Arithmetic/Max      | 156.457 μs | 138.2387 μs |  7.5773 μs | 154.623 μs |   15960 B |
| Reset                           | Arithmetic/Max      |   1.636 μs |   4.7020 μs |  0.2577 μs |   1.743 μs |    1360 B |
| **&#39;First execution (cold)&#39;**        | **Arithmetic/Medium**   |  **37.903 μs** | **137.4752 μs** |  **7.5355 μs** |  **34.089 μs** |    **3480 B** |
| &#39;Warm execution (steady state)&#39; | Arithmetic/Medium   |  48.949 μs | 196.4805 μs | 10.7698 μs |  44.142 μs |    3480 B |
| Reset                           | Arithmetic/Medium   |   1.484 μs |   4.1176 μs |  0.2257 μs |   1.364 μs |    1360 B |
| **&#39;First execution (cold)&#39;**        | **Arithmetic/Tiny**     |  **12.815 μs** |  **22.2040 μs** |  **1.2171 μs** |  **12.534 μs** |    **1656 B** |
| &#39;Warm execution (steady state)&#39; | Arithmetic/Tiny     |  10.623 μs |   6.6762 μs |  0.3659 μs |  10.470 μs |    1656 B |
| Reset                           | Arithmetic/Tiny     |   4.385 μs |   5.7905 μs |  0.3174 μs |   4.288 μs |    1360 B |
| **&#39;First execution (cold)&#39;**        | **Branch/Max**          | **124.311 μs** | **262.7446 μs** | **14.4019 μs** | **130.302 μs** |    **6360 B** |
| &#39;Warm execution (steady state)&#39; | Branch/Max          | 110.710 μs | 206.7247 μs | 11.3313 μs | 107.280 μs |    6360 B |
| Reset                           | Branch/Max          |   1.798 μs |   1.6318 μs |  0.0894 μs |   1.828 μs |    1360 B |
| **&#39;First execution (cold)&#39;**        | **Branch/Medium**       |  **30.157 μs** |   **9.9942 μs** |  **0.5478 μs** |  **30.026 μs** |    **2280 B** |
| &#39;Warm execution (steady state)&#39; | Branch/Medium       |  28.788 μs |   7.7571 μs |  0.4252 μs |  28.558 μs |    2280 B |
| Reset                           | Branch/Medium       |   1.586 μs |   0.3770 μs |  0.0207 μs |   1.592 μs |    1360 B |
| **&#39;First execution (cold)&#39;**        | **Branch/Tiny**         |  **12.616 μs** |  **13.1884 μs** |  **0.7229 μs** |  **12.764 μs** |    **1608 B** |
| &#39;Warm execution (steady state)&#39; | Branch/Tiny         |  18.375 μs | 252.1656 μs | 13.8220 μs |  10.521 μs |    1608 B |
| Reset                           | Branch/Tiny         |   1.600 μs |   1.1866 μs |  0.0650 μs |   1.603 μs |    1072 B |
| **&#39;First execution (cold)&#39;**        | **Native/Max**          |  **15.409 μs** |   **6.9778 μs** |  **0.3825 μs** |  **15.600 μs** |    **1928 B** |
| &#39;Warm execution (steady state)&#39; | Native/Max          |  14.988 μs |  10.3658 μs |  0.5682 μs |  15.088 μs |    1928 B |
| Reset                           | Native/Max          |   3.724 μs |  27.5969 μs |  1.5127 μs |   4.458 μs |    1360 B |
| **&#39;First execution (cold)&#39;**        | **Native/Medium**       |  **16.693 μs** |  **14.3675 μs** |  **0.7875 μs** |  **16.636 μs** |    **1928 B** |
| &#39;Warm execution (steady state)&#39; | Native/Medium       |  16.217 μs |  17.0640 μs |  0.9353 μs |  15.840 μs |    1928 B |
| Reset                           | Native/Medium       |   1.554 μs |   1.4900 μs |  0.0817 μs |   1.517 μs |    1360 B |
| **&#39;First execution (cold)&#39;**        | **Native/Tiny**         |  **16.811 μs** |  **14.5324 μs** |  **0.7966 μs** |  **16.561 μs** |    **1640 B** |
| &#39;Warm execution (steady state)&#39; | Native/Tiny         |  14.516 μs |  18.7507 μs |  1.0278 μs |  14.041 μs |    1928 B |
| Reset                           | Native/Tiny         |   1.396 μs |   3.1086 μs |  0.1704 μs |   1.303 μs |    1360 B |
| **&#39;First execution (cold)&#39;**        | **RuntimeError/Max**    | **153.386 μs** | **161.3004 μs** |  **8.8414 μs** | **148.336 μs** |   **15952 B** |
| &#39;Warm execution (steady state)&#39; | RuntimeError/Max    | 181.634 μs | 529.1890 μs | 29.0066 μs | 166.686 μs |   15952 B |
| Reset                           | RuntimeError/Max    |   2.058 μs |   1.0794 μs |  0.0592 μs |   2.034 μs |    1360 B |
| **&#39;First execution (cold)&#39;**        | **RuntimeError/Medium** |  **36.044 μs** | **117.3567 μs** |  **6.4327 μs** |  **32.461 μs** |    **3472 B** |
| &#39;Warm execution (steady state)&#39; | RuntimeError/Medium |  33.662 μs |   7.4445 μs |  0.4081 μs |  33.873 μs |    3472 B |
| Reset                           | RuntimeError/Medium |   1.516 μs |   1.5293 μs |  0.0838 μs |   1.543 μs |    1360 B |
| **&#39;First execution (cold)&#39;**        | **RuntimeError/Tiny**   |  **10.670 μs** |  **27.2564 μs** |  **1.4940 μs** |  **11.451 μs** |    **1496 B** |
| &#39;Warm execution (steady state)&#39; | RuntimeError/Tiny   |   8.094 μs |  13.8828 μs |  0.7610 μs |   7.660 μs |    1496 B |
| Reset                           | RuntimeError/Tiny   |   4.445 μs |   7.8021 μs |  0.4277 μs |   4.209 μs |    1360 B |
| **&#39;First execution (cold)&#39;**        | **StringArray/Max**     | **143.369 μs** |  **19.1913 μs** |  **1.0519 μs** | **143.572 μs** |   **24528 B** |
| &#39;Warm execution (steady state)&#39; | StringArray/Max     | 140.931 μs | 708.4661 μs | 38.8334 μs | 118.766 μs |   23856 B |
| Reset                           | StringArray/Max     |   1.348 μs |   1.1866 μs |  0.0650 μs |   1.351 μs |    1360 B |
| **&#39;First execution (cold)&#39;**        | **StringArray/Medium**  |  **26.067 μs** |   **6.2259 μs** |  **0.3413 μs** |  **26.093 μs** |    **4368 B** |
| &#39;Warm execution (steady state)&#39; | StringArray/Medium  |  26.429 μs |  85.1831 μs |  4.6692 μs |  23.844 μs |    4368 B |
| Reset                           | StringArray/Medium  |   1.806 μs |   4.8727 μs |  0.2671 μs |   1.913 μs |    1360 B |
| **&#39;First execution (cold)&#39;**        | **StringArray/Tiny**    |  **16.841 μs** |  **27.7105 μs** |  **1.5189 μs** |  **17.552 μs** |    **2064 B** |
| &#39;Warm execution (steady state)&#39; | StringArray/Tiny    |  16.166 μs |  63.6883 μs |  3.4910 μs |  15.369 μs |    2064 B |
| Reset                           | StringArray/Tiny    |   2.521 μs |  31.4150 μs |  1.7220 μs |   1.582 μs |      64 B |


## Graph shape compile (linear, fan-out, fan-in, disconnected, cycle)

```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.302
  [Host] : .NET 9.0.18 (9.0.1826.31522), X64 RyuJIT AVX2
  Ci     : .NET 9.0.18 (9.0.1826.31522), X64 RyuJIT AVX2

Job=Ci  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method          | CompileShape | Mean       | Error      | StdDev     | Allocated |
|---------------- |------------- |-----------:|-----------:|-----------:|----------:|
| **&#39;Compile graph&#39;** | **Linear**       | **100.069 μs** | **530.227 μs** | **29.0635 μs** |  **23.48 KB** |
| **&#39;Compile graph&#39;** | **FanOut**       |  **91.147 μs** | **215.298 μs** | **11.8012 μs** |  **23.95 KB** |
| **&#39;Compile graph&#39;** | **FanIn**        |  **95.996 μs** | **150.687 μs** |  **8.2597 μs** |  **24.59 KB** |
| **&#39;Compile graph&#39;** | **Disconnected** | **106.235 μs** | **189.219 μs** | **10.3718 μs** |  **36.19 KB** |
| **&#39;Compile graph&#39;** | **Cycle**        |   **6.989 μs** |   **9.352 μs** |  **0.5126 μs** |   **1.27 KB** |


## Graph shape run (warm)

```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.302
  [Host] : .NET 9.0.18 (9.0.1826.31522), X64 RyuJIT AVX2
  Ci     : .NET 9.0.18 (9.0.1826.31522), X64 RyuJIT AVX2

Job=Ci  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method             | RunShape     | Mean     | Error     | StdDev    | Allocated |
|------------------- |------------- |---------:|----------:|----------:|----------:|
| **&#39;Run graph (warm)&#39;** | **Linear**       | **7.305 μs** |  **8.977 μs** | **0.4920 μs** |    **1136 B** |
| **&#39;Run graph (warm)&#39;** | **FanOut**       | **8.087 μs** |  **2.965 μs** | **0.1625 μs** |    **1136 B** |
| **&#39;Run graph (warm)&#39;** | **FanIn**        | **7.423 μs** | **10.089 μs** | **0.5530 μs** |     **848 B** |
| **&#39;Run graph (warm)&#39;** | **Disconnected** | **8.230 μs** |  **3.354 μs** | **0.1838 μs** |    **1136 B** |

