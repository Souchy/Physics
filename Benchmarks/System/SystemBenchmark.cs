using Arch.Core;
using BenchmarkDotNet.Attributes;
using Godot;
using Physics.Mains.v1;
using Physics.Mains.v5_Arch;

namespace Benchmarks.System;

public class SystemBenchmark
{

    [GlobalSetup]
    public void Setup()
    {

    }

    #region ForeachParticleEntity
    private World world = World.Create();
    public QueryDescription query = new QueryDescription().WithAll<Position, Velocity, Color>();
    [IterationSetup(Target = nameof(ForeachParticleClass))]
    public void ForeachParticleEntitySetup()
    {
        for (int i = 0; i < 10_000; i++)
            world.Create(new Position(), new Velocity(new(GD.Randf(), GD.Randf())), new Color(1, 1, 1));
    }
    [Benchmark]
    public void ForeachParticleEntity()
    {
        world.Query(query, (ref Position position, ref Velocity velocity, ref Color color) =>
        {
            position.Value += velocity.Value;
            color = new Color(GD.Randf(), GD.Randf(), GD.Randf());
        });
    }
    public void ForeachParticleEntityCleanup()
    {
        world.Query(query, (Entity entt) =>
        {
            world.Destroy(entt);
        });
        world.Dispose();
        world = World.Create();
    }
    #endregion

    #region ForeachParticleClass
    private List<Particle> particlesClass = new();
    [IterationSetup(Target = nameof(ForeachParticleClass))]
    public void ForeachParticleClassSetup()
    {
        for (int i = 0; i < 10_000; i++)
            particlesClass.Add(new Particle(Vector2.Zero, new(GD.Randf(), GD.Randf()), new(1, 1, 1)));
    }
    [Benchmark]
    public void ForeachParticleClass()
    {
        for (int i = 0; i < particlesClass.Count; i++)
        {
            var particle = particlesClass[i];
            particle.Position += particle.Velocity;
            particle.Color = new Color(GD.Randf(), GD.Randf(), GD.Randf());
        }
    }
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
        for (int i = 0; i < 10_000; i++)
            particlesStruct.Add(new ParticleStruct(Vector2.Zero, new(GD.Randf(), GD.Randf()), new(1, 1, 1)));
    }
    [Benchmark]
    public void ForeachParticleStruct()
    {
        for (int i = 0; i < particlesStruct.Count; i++)
        {
            var particle = particlesStruct[i];
            particle.Position += particle.Velocity;
            particle.Color = new Color(GD.Randf(), GD.Randf(), GD.Randf());
            particlesStruct[i] = particle; // Reassign to update the list
        }
    }
    public void ForeachParticleStructCleanup()
    {
        particlesStruct.Clear();
    }
    #endregion

}
