using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Godot;
using Godot.Sharp.Extras;
using Physics.Utils;
using System;
using System.Collections.Generic;
using Xunit;

namespace Benchmarks.Quadtree;

public class QuadtreeBenchmarksXunit
{
    [Fact]
    public void RunQuadtreeBenchmark()
    {
        // This will run the benchmarks and output results to the console and BenchmarkDotNet's output folder.
        var summary = BenchmarkRunner.Run<QuadtreeBenchmark>();

        // Optionally, add a simple assertion to prevent xUnit from optimizing away the call.
        Assert.NotNull(summary);
    }
}

[MemoryDiagnoser]
public class QuadtreeBenchmark
{
    [Params(10_000, 25_000, 50_000)]
    public int ItemCount { get; set; }

    private Quadtree<int> quadtree = null!;
    private List<Vector2> positions = null!;
    private Rect2 bounds = new(0, 0, 1280, 720);
    private readonly Random rng = new();

    [GlobalSetup]
    public void Setup()
    {
        quadtree = new Quadtree<int>(0, bounds);
        positions = new List<Vector2>(ItemCount);
        for (int i = 0; i < ItemCount; i++)
        {
            positions.Add(new Vector2(
                (float) rng.NextDouble() * bounds.Size.X,
                (float) rng.NextDouble() * bounds.Size.Y
            ));
        }
    }

    #region Insert
    [IterationSetup(Target = nameof(InsertAll))]
    public void IterationSetupInsert()
    {
        quadtree.Clear();
    }
    [Benchmark]
    public void InsertAll()
    {
        for (int i = 0; i < ItemCount; i++)
        {
            quadtree.Insert(i, positions[i]);
        }
    }
    #endregion

    #region Query
    [IterationSetup(Target = nameof(QueryAll))]
    public void IterationSetupQuery()
    {
        quadtree.Clear();
        for (int i = 0; i < ItemCount; i++)
        {
            quadtree.Insert(i, positions[i]);
        }
    }

    [Benchmark]
    public void QueryAll()
    {
        int total = 0;
        for (int i = 0; i < ItemCount; i++)
        {
            var entityId = rng.Next(positions.Count);
            var pos = positions[entityId];
            var nodes = quadtree.QueryNodes(pos, 32f, []);
            total += nodes.Count;
        }
    }
    #endregion

}

