using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Godot;
using Godot.Sharp.Extras;
using Physics.Utils;
using System;
using System.Collections.Generic;
using Xunit;

namespace Physics.Tests;

public class QuadtreeBenchmarksXunit
{
    [Fact]
    public void RunQuadtreeBenchmark()
    {
        // This will run the benchmarks and output results to the console and BenchmarkDotNet's output folder.
        var summary = BenchmarkRunner.Run<QuadtreeBenchmarks>();

        // Optionally, add a simple assertion to prevent xUnit from optimizing away the call.
        Assert.NotNull(summary);
    }
}

[MemoryDiagnoser]
public class QuadtreeBenchmarks
{
    private Quadtree<int> quadtree;
    private List<Vector2> positions;
    private const int ItemCount = 10_000;
    private Rect2 bounds = new Rect2(0, 0, 1280, 720);
    private Random rng = new Random();

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

    [Benchmark]
    public void InsertAll()
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
        // Ensure the quadtree is populated
        if (quadtree.Data.Count == 0)
        {
            InsertAll();
        }
        int total = 0;
        for (int i = 0; i < 1000; i++)
        {
            var nodes = quadtree.QueryNodes(positions[i], 32f, new List<Quadtree<int>>());
            total += nodes.Count;
        }
    }
}

