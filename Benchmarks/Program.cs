using BenchmarkDotNet.Running;
using Benchmarks.Quadtree;

namespace Benchmarks;

internal class Program
{
    static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<QuadtreeBenchmark>();
    }
}
