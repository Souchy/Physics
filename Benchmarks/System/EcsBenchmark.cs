using Arch.Core;
using Arch.System;
using BenchmarkDotNet.Attributes;
using Benchmarks.Physics;
using Godot;
using Physics.Mains.v5_Arch;
using PhysicsLib.GodotArch;
using Schedulers;
using System.Runtime.CompilerServices;

namespace Benchmarks.System;

[MemoryDiagnoser]
public class EcsBenchmark : SystemBenchmarkParams
{
    public static QueryDescription query = new QueryDescription().WithAll<Position, Velocity, Color>();
    public static QueryDescription destroyQuery = new QueryDescription().WithAll<Alive>();

    #region ForeachParticleEntitySystemThread
    private World worldSystemThread;
    private Group<float> SystemThreads;
    [GlobalSetup(Target = nameof(ForeachParticleEntitySystemThread))]
    public void ForeachParticleEntitySystemThreadSetup()
    {
        worldSystemThread = World.Create();
        SystemThreads = new Group<float>(
            "Moving",
            new MoveSystemThread(worldSystemThread)
            );
        // Create Scheduler and assign it to world
        var jobScheduler = new JobScheduler(
          new JobScheduler.Config
          {
              ThreadPrefixName = "Arch.Threads",
              ThreadCount = 8,
              MaxExpectedConcurrentJobs = 64,
              StrictAllocationMode = false,
          }
        );
        World.SharedJobScheduler = jobScheduler;


        SystemThreads.Initialize();
        for (int i = 0; i < COUNT; i++)
            worldSystemThread.Create(new Position(), new Velocity(new(rng.NextSingle(), rng.NextSingle())), new Color(1, 1, 1));

    }
    [Benchmark]
    public void ForeachParticleEntitySystemThread()
    {
        SystemThreads.BeforeUpdate(in delta);
        SystemThreads.Update(in delta);
        SystemThreads.AfterUpdate(in delta);
    }
    [GlobalCleanup(Target = nameof(ForeachParticleEntitySystemThread))]
    public void ForeachParticleEntitySystemThreadCleanup()
    {
        worldSystemThread.Query(destroyQuery, worldSystemThread.Destroy);
        SystemThreads.Dispose();
        worldSystemThread.Dispose();
    }
    #endregion

    #region ForeachParticleEntitySystem
    private World worldSystem;
    private Group<float> systems;
    private MoveSystem moveSystem;
    [GlobalSetup(Target = nameof(ForeachParticleEntitySystem))]
    public void ForeachParticleEntitySystemSetup()
    {
        worldSystem = World.Create();
        moveSystem = new MoveSystem(worldSystem);
        systems = new Group<float>(
            "Moving",
            moveSystem
            );
        systems.Initialize();
        for (int i = 0; i < COUNT; i++)
            worldSystem.Create(new Position(), new Velocity(new(rng.NextSingle(), rng.NextSingle())), new Color(1, 1, 1));
    }
    [Benchmark]
    public void ForeachParticleEntitySystem()
    {
        systems.BeforeUpdate(in delta);
        systems.Update(in delta);
        systems.AfterUpdate(in delta);
    }
    [GlobalCleanup(Target = nameof(ForeachParticleEntitySystem))]
    public void ForeachParticleEntitySystemCleanup()
    {
        worldSystem.Query(destroyQuery, worldSystem.Destroy);
        systems.Dispose();
        worldSystem.Dispose();
        Console.WriteLine($"System Counter: {MoveSystem.counter}");
        MoveSystem.counter = 0;
    }
    #endregion


    #region ForeachParticleEntityLowLevel
    private World worldLowLevel = World.Create();
    [GlobalSetup(Target = nameof(ForeachParticleEntityLowLevel))]
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
    [GlobalCleanup(Target = nameof(ForeachParticleEntityLowLevel))]
    public void ForeachParticleEntityLowLevelCleanup()
    {
        worldLowLevel.Query(destroyQuery, worldLowLevel.Destroy);
        worldLowLevel.Dispose();
        worldLowLevel = World.Create();
    }
    #endregion

    #region ForeachParticleEntity
    private World worldBasic = World.Create();
    [GlobalSetup(Target = nameof(ForeachParticleEntity))]
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
    [GlobalCleanup(Target = nameof(ForeachParticleEntity))]
    public void ForeachParticleEntityCleanup()
    {
        worldBasic.Query(destroyQuery, worldBasic.Destroy);
        worldBasic.Dispose();
        worldBasic = World.Create();
    }
    #endregion


}
