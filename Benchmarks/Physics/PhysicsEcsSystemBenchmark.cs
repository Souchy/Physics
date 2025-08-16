using Arch.Core;
using Arch.Core.Extensions;
using BenchmarkDotNet.Attributes;
using Godot;
using Physics.Mains.v5_Arch;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace Benchmarks.Physics;

[MemoryDiagnoser]
public class PhysicsEcsSystemBenchmark : PhysicsEcsQueryBenchmark
{

    //[GlobalSetup]
    public override void GlobalSetup()
    {
        Console.WriteLine("Starting PhysicsEcsSystemBenchmark...");
        world = World.Create();
        AddTeam1(TEAM_1);
        AddTeam2(TEAM_2);
    }

    //[GlobalCleanup]
    public override void GlobalCleanup()
    {
        world.Dispose();
        Console.WriteLine($"Low level Frames: {frame}, entity iterations: {entityIterations}, avg per frame: {(float) entityIterations / frame}, quadtreeInserts: {quadtreeInserts}, destroyed: {destroyed}");
    }

    [BenchmarkCategory("PhysicsFrame")]
    [Benchmark(Description = "ECS low level Frame")]
    public override void PhysicsEcsFrame()
    {
        frame++;
        if (!logged)
        {
            Console.WriteLine($"Low level logged {world.CountEntities(in destroyQuery)} entities");
        }

        // Quadtree
        quadtree.Clear();
        var queryQuadtree = world.Query(in quadtreeQuery);
        foreach (ref var chunk in queryQuadtree.GetChunkIterator())
        {
            var references = chunk.GetFirst<Position, CollisionLayer>();
            foreach (var i in chunk)
            {
                ref var position = ref Unsafe.Add(ref references.t0, i);
                ref var collisionLayer = ref Unsafe.Add(ref references.t1, i);
                if (collisionLayer.Value != 0)
                {
                    var entt = chunk.Entity(i);
                    quadtree.Insert(entt, position.Value);
                    quadtreeInserts++;

                    //if (!logged)
                    //{
                    //    Console.WriteLine($"Low level entity: {entt}, getPos: {entt.Get<Position>().Value}, alive: {entt.Get<Alive>().Value}");
                    //}
                }
            }
        }

        // Physics
        var queryPhysics = world.Query(in physicsQuery);
        foreach (ref var chunk in queryPhysics.GetChunkIterator())
        {
            var references = chunk.GetFirst<Alive, Position, Velocity, CollisionImmunityTime, Modulate>();
            foreach (var i in chunk)
            {
                ref var alive = ref Unsafe.Add(ref references.t0, i);
                ref var position = ref Unsafe.Add(ref references.t1, i);
                ref var velocity = ref Unsafe.Add(ref references.t2, i);
                ref var collisionImmunityTime = ref Unsafe.Add(ref references.t3, i);
                ref var color = ref Unsafe.Add(ref references.t4, i);

                entityIterations++;
                if (alive.Value == false) continue;
                // Physics
                var entt = chunk.Entity(i);
                DetectCollisions(entt);
                collisionImmunityTime.Value = Math.Max(0, collisionImmunityTime.Value - delta);
                color.Value = new Color(color.Value, (float) (1 - collisionImmunityTime.Value));

                RespectBounds(ref position, ref velocity, backgroundSize);

                // Update pos
                position.Value += velocity.Value * delta;

                //if (!logged && entityIterations < 100)
                //{
                //    Console.WriteLine($"Low level entity2: {entt}, getPos: {entt.Get<Position>().Value}, alive: {entt.Get<Alive>().Value}");
                //}
            }
        }

        // Cleanup dead particles
        var queryDestroy = world.Query(in destroyQuery);
        foreach (ref var chunk in queryDestroy.GetChunkIterator())
        {
            var references = chunk.GetFirst<Alive, Life>();
            foreach (var i in chunk)
            {
                var entt = chunk.Entity(i);
                ref var alive = ref Unsafe.Add(ref references.t0, i);
                //if (!logged)
                //    Console.WriteLine($"Low level entity3: {entt}, alive ref: {alive}, alive: {entt.Get<Alive>().Value}");
                if (alive.Value == false)
                {
                    world.Destroy(entt);
                    destroyed++;
                }
            }
        }

        logged = true;
    }

}
