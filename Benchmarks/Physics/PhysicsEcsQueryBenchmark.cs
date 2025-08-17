using Arch.Core;
using Arch.Core.Extensions;
using BenchmarkDotNet.Attributes;
using Godot;
using Physics.Mains.v5_Arch;

namespace Benchmarks.Physics;

[MemoryDiagnoser]
public class PhysicsEcsQueryBenchmark : PhysicsParameters
{

    public EntityQuadtree quadtree;
    public World world;
    public QueryDescription destroyQuery = new QueryDescription().WithAll<Alive, Life>();
    public QueryDescription quadtreeQuery = new QueryDescription().WithAll<Position, CollisionLayer>();
    public QueryDescription physicsQuery = new QueryDescription().WithAll<Alive, Position, Velocity, CollisionImmunityTime, Modulate>();
    public Random rnd = new();

    [GlobalSetup]
    public virtual void GlobalSetup()
    {
        Console.WriteLine("Starting PhysicsEcsQueryBenchmark...");
        world = World.Create();
        quadtree = new(0, new Rect2(Vector2.Zero, backgroundSize));
        AddTeam1(TEAM_1);
        AddTeam2(TEAM_2);
    }

    [GlobalCleanup]
    public virtual void GlobalCleanup()
    {
        world.Dispose();
        //Query Frames: 2433, entity iterations: 26763000, avg per frame: 11000, quadtreeInserts: 2433000, destroyed: 0
        Console.WriteLine($"Query Frames: {frame}, entity iterations: {entityIterations}, avg per frame: {(float) entityIterations / frame}, quadtreeInserts: {quadtreeInserts}, destroyed: {destroyed}");
    }

    public void AddTeam1(int count)
    {
        for (int i = 0; i < count; i++)
            AddParticle(0, 1, new Color(1, 0, 0));
    }
    public void AddTeam2(int count)
    {
        for (int i = 0; i < count; i++)
            AddParticle(1, 0, new Color(0, 0, 1));
    }
    public void AddParticle(int detectionMask, int collisionLayer, Color color)
    {
        Vector2 position = Vector2.Zero;
        float angle = rnd.NextSingle() * Mathf.Tau;
        position.X = Mathf.Cos(angle);
        position.Y = Mathf.Sin(angle);
        position *= 500;
        Vector2 velocity = -position.Normalized() * 200f;
        position += backgroundSize / 2f; // Offset to center the particles in the background
        _ = world.Create(
            //new Id(ids.GetNextId()),
            new Position(position),
            new Velocity(velocity),
            new Modulate(color),
            new Size(32f),
            new Life(10),
            new Alive(true),
            new CollisionImmunityTime(CollisionImmunityTimer),
            new DetectionMask(detectionMask),
            new CollisionLayer(collisionLayer)
        );
    }

    [BenchmarkCategory("PhysicsFrame")]
    [Benchmark(Description = "ECS basic query Frame")]
    public virtual void PhysicsEcsFrame()
    {
        frame++;
        if (!logged)
        {
            Console.WriteLine($"Basic query logged {world.CountEntities(in destroyQuery)} entities");
            logged = true;
        }

        quadtree.Clear();
        world.Query(quadtreeQuery, (Entity entt, ref Position position, ref CollisionLayer collisionLayer) =>
        {
            if (collisionLayer.Value != 0)
            {
                quadtree.Insert(entt, position.Value);
                quadtreeInserts++;
            }
        });

        //int mmi = 0;
        world.Query(physicsQuery, (Entity entt, ref Alive alive, ref Position position, ref Velocity velocity,
            ref CollisionImmunityTime collisionImmunityTime, ref Modulate color) =>
        {
            entityIterations++;

            if (alive.Value == false) return;
            // Physics
            DetectCollisions(entt);
            collisionImmunityTime.Value = Math.Max(0, collisionImmunityTime.Value - delta);
            color.Value = new Color(color.Value, (float) (1 - collisionImmunityTime.Value));

            RespectBounds(ref position, ref velocity, backgroundSize);

            // Update pos
            position.Value += velocity.Value * delta;

            // Update particle rendering
            //if (alive.Value == false)
            //{
            //    //spawner.RemoveInstance();
            //}
            //else
            //{
            //    // update rendering
            //    // spawner.UpdateInstance(mmi, position, velocity, color);
            //    mmi++;
            //}
        });

        // Cleanup dead particles
        world.Query(destroyQuery, (Entity entt, ref Alive alive) => //, ref Id id) =>
        {
            if (alive.Value == false)
            {
                // remove from data (stop iterating it)
                world.Destroy(entt);
                //ids.ReleaseId(id.Value);
                destroyed++;
            }
        });
    }

    public virtual void DetectCollisions(Entity p1)
    {
        //var (detectionMask1, collisionImmunity1) = p1.Get<DetectionMask, CollisionImmunityTime>();
        var detectionMask1 = p1.Get<DetectionMask>();
        var collisionImmunity1 = p1.Get<CollisionImmunityTime>();

        if (detectionMask1.Value == 0) return; // Skip if no detection mask
        if (collisionImmunity1.Value > 0) return; // Skip if immune to collisions

        //var (position1, size1) = p1.Get<Position, Size>();
        var position1 = p1.Get<Position>();
        var size1 = p1.Get<Size>();
        var nodes = quadtree.QueryNodes(position1.Value, size1.Value, []);
        foreach (var node in nodes)
        {
            foreach (var p2 in node.Data)
            {
                if (p1 == p2) continue; // Skip self-collision
                //if (!p2.IsAlive()) continue;
                //if (p2.Get<Id>().Value == id1.Value) continue;
                //if (particleDict.TryGetValue(id, out var p2) == false) continue; // Skip if particle not found
                if (p2.Get<Alive>().Value == false) continue;
                if (p2.Get<CollisionImmunityTime>().Value > 0) continue; // Skip if immune to collisions
                int collisionLayer2 = p2.Get<CollisionLayer>().Value;
                if (collisionLayer2 == 0) continue; // Skip if p2 doesnt have collisions
                if ((detectionMask1.Value & collisionLayer2) == 0) continue; // Skip masks dont match

                bool collided = CheckCollision(p1, p2);
                if (p1.Get<CollisionImmunityTime>().Value > 0) return; // Cancel the rest because it becomes immune to collisions
            }
        }
    }

    public virtual bool CheckCollision(Entity p1, Entity p2)
    {
        Vector2 deltaPos = p1.Get<Position>().Value - p2.Get<Position>().Value;
        float distSquared = deltaPos.LengthSquared();
        float particleRadiusSum = p1.Get<Size>().Value; // + p2.Size;
        if (distSquared < particleRadiusSum * particleRadiusSum)
        {
            //p1.Get<OnCollision>().Value(p1, p2, deltaPos, distSquared, true);
            //p2.Get<OnCollision>().Value(p2, p1, deltaPos, distSquared, false);

            ref var velocity1 = ref p1.Get<Velocity>();
            ref var velocity2 = ref p2.Get<Velocity>();

            // p1
            velocity1.Value = velocity1.Value.Bounce(deltaPos.Normalized());
            p1.Get<CollisionImmunityTime>().Value = CollisionImmunityTimer;

            // p2
            velocity1.Value = velocity1.Value.Bounce(-deltaPos.Normalized());
            p2.Get<CollisionImmunityTime>().Value = CollisionImmunityTimer;
            //p2.Life--;
            //if (p2.Life <= 0)
            //    p2.Alive = false;
            return true;
        }
        return false;
    }

    public virtual void RespectBounds(ref Position position, ref Velocity velocity, Vector2 Bounds)
    {
        float vx = velocity.Value.X;
        float vy = velocity.Value.Y;

        if (position.Value.X > Bounds.X)
            vx = Math.Abs(velocity.Value.X) * -1;
        if (position.Value.X < 0)
            vx = Math.Abs(velocity.Value.X);

        if (position.Value.Y > Bounds.Y)
            vy = Math.Abs(velocity.Value.Y) * -1;
        if (position.Value.Y < 0)
            vy = Math.Abs(velocity.Value.Y);

        velocity.Value = new Vector2(vx, vy);
    }

}
