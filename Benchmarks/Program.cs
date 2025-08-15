using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Benchmarks.Physics;
using Benchmarks.Quadtree;
using Benchmarks.System;

namespace Benchmarks;

internal class Program
{
    static void Main(string[] args)
    {
        //BenchmarkRunner.Run<QuadtreeBenchmark>();

        //BenchmarkRunner.Run<SystemBenchmark>();

        var config = ManualConfig.Create(DefaultConfig.Instance)
          .WithOptions(ConfigOptions.JoinSummary | ConfigOptions.DisableLogFile | ConfigOptions.StopOnFirstError);

        BenchmarkRunner.Run([
            BenchmarkConverter.TypeToBenchmarks( typeof(PhysicsClassBenchmark), config),
            BenchmarkConverter.TypeToBenchmarks( typeof(PhysicsEcsQueryBenchmark), config),
            BenchmarkConverter.TypeToBenchmarks( typeof(PhysicsEcsLowLevelBenchmark), config)
            ]);
    }
}
