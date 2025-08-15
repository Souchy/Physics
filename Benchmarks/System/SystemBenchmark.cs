using Arch.Core;
using BenchmarkDotNet.Attributes;
using Godot;
using Physics.Mains.v5_Arch;
using System.Runtime.CompilerServices;

namespace Benchmarks.System;

[MemoryDiagnoser]
public class SystemBenchmark
{
    private class ParticleClass(Vector2 Position, Vector2 Velocity, Color Color)
    {
        public Vector2 Position { get; set; } = Position;
        public Vector2 Velocity { get; set; } = Velocity;
        public Color Color { get; set; } = Color;
    }
    private record struct ParticleStruct(Vector2 Position, Vector2 Velocity, Color Color);


    private Random rng = new();
    private const int COUNT = 1_000_000;

    [GlobalSetup]
    public void Setup()
    {
    }

    #region ForeachParticleEntityLowLevel
    private World worldLowLevel = World.Create();
    [IterationSetup(Target = nameof(ForeachParticleEntityLowLevel))]
    public void ForeachParticleEntityLowLevelSetup()
    {
        for (int i = 0; i < COUNT; i++)
            worldLowLevel.Create(new Position(), new Velocity(new(rng.NextSingle(), rng.NextSingle())), new Color(1, 1, 1));
    }
    [Benchmark]
    public void ForeachParticleEntityLowLevel()
    {
        var queryDestroy = worldLowLevel.Query(in query);
        foreach (ref var chunk in queryDestroy.GetChunkIterator())
        {
            var references = chunk.GetFirst<Position, Velocity, Color>();
            foreach (var entity in chunk)
            {
                ref var position = ref Unsafe.Add(ref references.t0, entity);
                ref var velocity = ref Unsafe.Add(ref references.t1, entity);
                ref var color = ref Unsafe.Add(ref references.t2, entity);

                position.Value += velocity.Value;
                color = new Color(rng.NextSingle(), rng.NextSingle(), rng.NextSingle());
            }
        }
    }
    [IterationCleanup(Target = nameof(ForeachParticleEntityLowLevel))]
    public void ForeachParticleEntityLowLevelCleanup()
    {
        worldLowLevel.Query(query, (Entity entt) =>
        {
            worldLowLevel.Destroy(entt);
        });
        worldLowLevel.Dispose();
        worldLowLevel = World.Create();
    }
    #endregion

    #region ForeachParticleEntity
    private World worldBasic = World.Create();
    public QueryDescription query = new QueryDescription().WithAll<Position, Velocity, Color>();
    public QueryDescription destroyQuery = new QueryDescription().WithAll<Alive>();
    [IterationSetup(Target = nameof(ForeachParticleEntity))]
    public void ForeachParticleEntitySetup()
    {
        for (int i = 0; i < COUNT; i++)
            worldBasic.Create(new Position(), new Velocity(new(rng.NextSingle(), rng.NextSingle())), new Color(1, 1, 1));
    }
    [Benchmark]
    public void ForeachParticleEntity()
    {
        worldBasic.Query(query, (ref Position position, ref Velocity velocity, ref Color color) =>
        {
            position.Value += velocity.Value;
            color = new Color(rng.NextSingle(), rng.NextSingle(), rng.NextSingle());
        });
    }
    [IterationCleanup(Target = nameof(ForeachParticleEntity))]
    public void ForeachParticleEntityCleanup()
    {
        worldBasic.Query(query, (Entity entt) =>
        {
            worldBasic.Destroy(entt);
        });
        worldBasic.Dispose();
        worldBasic = World.Create();
    }
    #endregion


    #region ForeachParticleClass
    private List<ParticleClass> particlesClass = new();
    [IterationSetup(Target = nameof(ForeachParticleClass))]
    public void ForeachParticleClassSetup()
    {
        for (int i = 0; i < COUNT; i++)
            particlesClass.Add(new ParticleClass(Vector2.Zero, new(rng.NextSingle(), rng.NextSingle()), new(1, 1, 1)));
    }
    [Benchmark]
    public void ForeachParticleClass()
    {
        for (int i = 0; i < particlesClass.Count; i++)
        {
            var particle = particlesClass[i];
            particle.Position += particle.Velocity;
            particle.Color = new Color(rng.NextSingle(), rng.NextSingle(), rng.NextSingle());
        }
    }
    [IterationCleanup(Target = nameof(ForeachParticleClass))]
    public void ForeachParticleClassCleanup()
    {
        particlesClass.Clear();
    }
    #endregion


    #region ForeachParticleStruct
    private List<ParticleStruct> particlesStruct = new();
    [IterationSetup(Target = nameof(ForeachParticleStruct))]
    public void ForeachParticleStructSetup()
    {
        for (int i = 0; i < COUNT; i++)
            particlesStruct.Add(new ParticleStruct(Vector2.Zero, new(rng.NextSingle(), rng.NextSingle()), new(1, 1, 1)));
    }
    [Benchmark]
    public void ForeachParticleStruct()
    {
        for (int i = 0; i < particlesStruct.Count; i++)
        {
            var particle = particlesStruct[i];
            particle.Position += particle.Velocity;
            particle.Color = new Color(rng.NextSingle(), rng.NextSingle(), rng.NextSingle());
            particlesStruct[i] = particle; // Reassign to update the list
        }
    }
    [IterationCleanup(Target = nameof(ForeachParticleStruct))]
    public void ForeachParticleStructCleanup()
    {
        particlesStruct.Clear();
    }
    #endregion


    #region ForeachParticleList
    private List<Vector2> particlesListPosition = new();
    private List<Vector2> particlesListVelocity = new();
    private List<Color> particlesListColor = new();
    [IterationSetup(Target = nameof(ForeachParticleList))]
    public void ForeachParticleListSetup()
    {
        for (int i = 0; i < COUNT; i++)
        {
            particlesListPosition.Add(Vector2.Zero);
            particlesListVelocity.Add(new(rng.NextSingle(), rng.NextSingle()));
            particlesListColor.Add(new Color(1, 1, 1));
        }
    }
    [Benchmark]
    public void ForeachParticleList()
    {
        for (int i = 0; i < particlesListPosition.Count; i++)
        {
            particlesListPosition[i] += particlesListVelocity[i];
            particlesListColor[i] = new Color(rng.NextSingle(), rng.NextSingle(), rng.NextSingle());
        }
    }
    [IterationCleanup(Target = nameof(ForeachParticleList))]
    public void ForeachParticleListCleanup()
    {
        particlesListPosition.Clear();
        particlesListVelocity.Clear();
        particlesListColor.Clear();
    }
    #endregion

    #region ForeachParticleArray
    private Vector2[] particlesArrayPosition = new Vector2[COUNT];
    private Vector2[] particlesArrayVelocity = new Vector2[COUNT];
    private Color[] particlesArrayColor = new Color[COUNT];
    [IterationSetup(Targets = [nameof(ForeachParticleArrayCombinedLoops), nameof(ForeachParticleArraySeparateLoops)])]
    public void ForeachParticleArraySetup()
    {
        for (int i = 0; i < COUNT; i++)
        {
            particlesArrayPosition[i] = Vector2.Zero;
            particlesArrayVelocity[i] = new(rng.NextSingle(), rng.NextSingle());
            particlesArrayColor[i] = new Color(1, 1, 1);
        }
    }
    [Benchmark]
    public void ForeachParticleArrayCombinedLoops()
    {
        for (int i = 0; i < particlesArrayPosition.Length; i++)
        {
            particlesArrayPosition[i] += particlesArrayVelocity[i];
            particlesArrayColor[i] = new Color(rng.NextSingle(), rng.NextSingle(), rng.NextSingle());
        }
    }
    [Benchmark]
    public void ForeachParticleArraySeparateLoops()
    {
        for (int i = 0; i < particlesArrayPosition.Length; i++)
        {
            particlesArrayPosition[i] += particlesArrayVelocity[i];
        }
        for (int i = 0; i < particlesArrayColor.Length; i++)
        {
            particlesArrayColor[i] = new Color(rng.NextSingle(), rng.NextSingle(), rng.NextSingle());
        }
    }
    [IterationCleanup(Targets = [nameof(ForeachParticleArrayCombinedLoops), nameof(ForeachParticleArraySeparateLoops)])]
    public void ForeachParticleArrayCleanup()
    {
        particlesArrayPosition = new Vector2[COUNT];
        particlesArrayVelocity = new Vector2[COUNT];
        particlesArrayColor = new Color[COUNT];
    }
    #endregion

}
