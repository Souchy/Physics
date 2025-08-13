using BenchmarkDotNet.Attributes;
using Godot;
using Physics.Utils;

namespace Benchmarks.Quadtree;

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

