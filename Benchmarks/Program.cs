using BenchmarkDotNet.Running;
using Physics.Tests;

namespace Benchmarks;

internal class Program
{
    static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<QuadtreeBenchmarks>();
    }
}
