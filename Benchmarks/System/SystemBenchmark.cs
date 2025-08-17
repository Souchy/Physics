using BenchmarkDotNet.Attributes;
using Godot;
using System;
using System.Collections.Generic;
using System.Text;

namespace Benchmarks.System;

internal class ParticleClass(Vector2 Position, Vector2 Velocity, Color Color)
{
    public Vector2 Position { get; set; } = Position;
    public Vector2 Velocity { get; set; } = Velocity;
    public Color Color { get; set; } = Color;
}
internal record struct ParticleStruct(Vector2 Position, Vector2 Velocity, Color Color);

public class SystemBenchmarkParams
{
    [Params(1_000_000)]
    public int COUNT { get; set; }

    public Random rng = new();
    public float delta = 1f / 60f;
}


[MemoryDiagnoser]
public class SystemBenchmark : SystemBenchmarkParams
{

    #region ForeachParticleClass
    private List<ParticleClass> particlesClass;
    [GlobalSetup(Target = nameof(ForeachParticleClass))]
    public void ForeachParticleClassSetup()
    {
        particlesClass = new(COUNT);
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
    [GlobalCleanup(Target = nameof(ForeachParticleClass))]
    public void ForeachParticleClassCleanup()
    {
        particlesClass.Clear();
    }
    #endregion


    #region ForeachParticleStruct
    private List<ParticleStruct> particlesStruct;
    [GlobalSetup(Target = nameof(ForeachParticleStruct))]
    public void ForeachParticleStructSetup()
    {
        particlesStruct = new(COUNT);
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
    [GlobalCleanup(Target = nameof(ForeachParticleStruct))]
    public void ForeachParticleStructCleanup()
    {
        particlesStruct.Clear();
    }
    #endregion


    #region ForeachParticleList
    private List<Vector2> particlesListPosition;
    private List<Vector2> particlesListVelocity;
    private List<Color> particlesListColor;
    [GlobalSetup(Target = nameof(ForeachParticleList))]
    public void ForeachParticleListSetup()
    {
        particlesListPosition = new(COUNT);
        particlesListVelocity = new(COUNT);
        particlesListColor = new(COUNT);
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
    [GlobalCleanup(Target = nameof(ForeachParticleList))]
    public void ForeachParticleListCleanup()
    {
        particlesListPosition.Clear();
        particlesListVelocity.Clear();
        particlesListColor.Clear();
    }
    #endregion

    #region ForeachParticleArray
    private Vector2[] particlesArrayPosition;
    private Vector2[] particlesArrayVelocity;
    private Color[] particlesArrayColor;
    [GlobalSetup(Targets = [nameof(ForeachParticleArrayCombinedLoops), nameof(ForeachParticleArraySeparateLoops), nameof(ForeachParticleArrayCombinedThreadsLoops)])]
    public void ForeachParticleArraySetup()
    {
        particlesArrayPosition = new Vector2[COUNT];
        particlesArrayVelocity = new Vector2[COUNT];
        particlesArrayColor = new Color[COUNT];
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
    public void ForeachParticleArrayCombinedThreadsLoops()
    {
        var result = Parallel.For(0, particlesArrayPosition.Length, i =>
        {
            particlesArrayPosition[i] += particlesArrayVelocity[i];
            particlesArrayColor[i] = new Color(rng.NextSingle(), rng.NextSingle(), rng.NextSingle());
        });
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
    [GlobalCleanup(Targets = [nameof(ForeachParticleArrayCombinedLoops), nameof(ForeachParticleArraySeparateLoops), nameof(ForeachParticleArrayCombinedThreadsLoops)])]
    public void ForeachParticleArrayCleanup()
    {
        particlesArrayPosition = new Vector2[COUNT];
        particlesArrayVelocity = new Vector2[COUNT];
        particlesArrayColor = new Color[COUNT];
    }
    #endregion
}
