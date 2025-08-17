using Arch.Core;
using Arch.System;
using BenchmarkDotNet.Attributes;
using Godot;
using Physics.Mains.v5_Arch;

namespace Benchmarks.Physics;

public partial class QuadtreeSystem(PhysicsEcsSystemBenchmark bench) : BaseSystem<World, float>(bench.world)
{
    public PhysicsEcsSystemBenchmark bench = bench;
    //public static PhysicsEcsSystemBenchmark bench => PhysicsEcsSystemBenchmark.Instance;

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Query]
    [All(typeof(Position), typeof(CollisionLayer))]
    public void InsertEntity(in Entity entt, Position position, CollisionLayer collisionLayer)
    {
        if (collisionLayer.Value != 0)
        {
            bench.quadtree.Insert(entt, position.Value);
            bench.quadtreeInserts++;
        }
    }
}

public partial class PhysicsSystem(PhysicsEcsSystemBenchmark bench) : BaseSystem<World, float>(bench.world)
{
    public PhysicsEcsSystemBenchmark bench = bench;
    //public static PhysicsEcsSystemBenchmark bench => PhysicsEcsSystemBenchmark.Instance;

    //[Query(Parallel = true)]
    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Query]
    [All(typeof(Alive), typeof(Position), typeof(Velocity), typeof(CollisionImmunityTime), typeof(Modulate))]
    public void MoveEntity([Data] in float delta, in Entity entt, Alive alive, Position position, CollisionImmunityTime collisionImmunityTime, Velocity velocity, Modulate color)
    {
        bench.entityIterations++;
        if (alive.Value == false) return;
        // Physics
        bench.DetectCollisions(entt);
        collisionImmunityTime.Value = Math.Max(0, collisionImmunityTime.Value - delta);
        color.Value = new Color(color.Value, (float) (1 - collisionImmunityTime.Value));

        bench.RespectBounds(ref position, ref velocity, bench.backgroundSize);

        // Update pos
        position.Value += velocity.Value * delta;
    }
}

public partial class DestroySystem(PhysicsEcsSystemBenchmark bench) : BaseSystem<World, float>(bench.world)
{
    public PhysicsEcsSystemBenchmark bench = bench;
    //public static PhysicsEcsSystemBenchmark bench => PhysicsEcsSystemBenchmark.Instance;

    //[Query(Parallel = true)]
    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Query]
    [All(typeof(Alive))]
    public void MoveEntity([Data] in float delta, in Entity entt, Alive alive)
    {
        //if (!logged)
        //    Console.WriteLine($"Low level entity3: {entt}, alive ref: {alive}, alive: {entt.Get<Alive>().Value}");
        if (alive.Value == false)
        {
            bench.world.Destroy(entt);
            bench.destroyed++;
        }
    }
}


[MemoryDiagnoser]
public class PhysicsEcsSystemBenchmark : PhysicsEcsQueryBenchmark
{
    public static PhysicsEcsSystemBenchmark Instance { get; private set; }

    public Group<float> systems;

    //[GlobalSetup]
    public override void GlobalSetup()
    {
        Instance = this;
        Console.WriteLine("Starting PhysicsEcsSystemBenchmark...");
        quadtree = new(0, new Rect2(Vector2.Zero, backgroundSize));
        world = World.Create();
        systems = new Group<float>(
            "Physics",
            new QuadtreeSystem(this),
            new PhysicsSystem(this),
            new DestroySystem(this)
        //new MovementSystem(world),
        //new PhysicsSystem(world),
        //new DamageSystem(world),
        //new WorldGenerationSystem(world),
        //new OtherGroup(world)
        );
        systems.Initialize();
        AddTeam1(TEAM_1);
        AddTeam2(TEAM_2);
    }

    //[GlobalCleanup]
    public override void GlobalCleanup()
    {
        systems.Dispose();
        world.Dispose();
        Console.WriteLine($"System Frames: {frame}, entity iterations: {entityIterations}, avg per frame: {(float) entityIterations / frame}, quadtreeInserts: {quadtreeInserts}, destroyed: {destroyed}");
    }

    [BenchmarkCategory("PhysicsFrame")]
    [Benchmark(Description = "ECS system Frame")]
    public override void PhysicsEcsFrame()
    {
        frame++;
        if (!logged)
        {
            Console.WriteLine($"Ecs System logged {world.CountEntities(in destroyQuery)} entities");
        }

        // Quadtree
        quadtree.Clear();

        systems.BeforeUpdate(in delta);    // Calls .BeforeUpdate on all systems ( can be overriden )
        systems.Update(in delta);          // Calls .Update on all systems ( can be overriden )
        systems.AfterUpdate(in delta);     // Calls .AfterUpdate on all System ( can be overriden )

        logged = true;
    }

}
