using BenchmarkDotNet.Running;
using Benchmarks.Quadtree;
using Benchmarks.System;

namespace Benchmarks;

internal class Program
{
    static void Main(string[] args)
    {
        //var summary = BenchmarkRunner.Run<QuadtreeBenchmark>();
        var summary2 = BenchmarkRunner.Run<SystemBenchmark>();
    }
}
