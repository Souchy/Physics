using BenchmarkDotNet.Attributes;
using Godot;
using Physics.Utils;
using static Godot.WebSocketPeer;

namespace Benchmarks.Physics;

public class EventBus
{
    public Dictionary<Type, List<Delegate>> subscribers = new();
}
public record class Stat(int Value);
public class Item
{
    public Dictionary<int, Stat> stats = [];
}

public record struct PositionId(int Id, Vector2 Position) : IHasPosition;
public class Particle(int id, Vector2 position, Vector2 velocity, Color color, int mask, int layer)
{
    public int Id { get; set; } = id;
    public Vector2 Position { get; set; } = position;
    public Vector2 Velocity { get; set; } = velocity;
    public Color Color { get; set; } = color;
    public int DetectionMask = mask; // Mask to detect other objects. If >0, detect objects with matching layers.
    public int CollisionLayer = layer; // Layers this object is in

    public int Life { get; set; } = 10;
    public bool Alive { get; set; } = true;
    public double CollisionImmunityTime = 0.5;
    public float Size = 32f;

    //public Node2D? Sprite { get; set; }
    public List<Item> items = [];
    public Dictionary<int, Stat> stats = [];
}

/*
Collisions:

enemies -> player skills
enemies -> player
player -> enemy skills

walls -> anything

//player -> enemies
//player -> walls
//walls -> player skills
//walls -> enemies
//walls -> enemy skills

everything is one way.
so threadable because A affects B, but B does not affect A.
 */

[MemoryDiagnoser]
public class PhysicsClassBenchmark
{

    public const int TEAM_1 = 1_000;
    public const int TEAM_2 = 10_000;
    public const int COUNT = TEAM_1 + TEAM_2;
    public static readonly Vector2 backgroundSize = new Vector2(1280, 720);
    public const float delta = 1f / 60f; // Simulating a frame time of 1/60 seconds
    public const double CollisionImmunityTimer = 0.1; // seconds

    public IntId ids = new();
    public List<Particle> particles = new(COUNT);
    public Dictionary<int, Particle> particleDict = new(COUNT);
    public QuadtreeWithPosStruct<PositionId> quadtree = new(0, new Rect2(Vector2.Zero, backgroundSize));
    public Random rnd = new();


    [GlobalSetup]
    public void GlobalSetup()
    {
        AddTeam1(TEAM_1);
        AddTeam2(TEAM_2);
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
        var p = new Particle(ids.GetNextId(), position, velocity, color, detectionMask, collisionLayer);
        particles.Add(p);
        particleDict[p.Id] = p;
    }

    //[IterationSetup]
    //public void IterationSetup()
    //{
    //}

    [BenchmarkCategory("PhysicsFrame")]
    [Benchmark(Description = "Class Frame")]
    public void PhysicsClassFrame()
    {
        quadtree.Clear();
        for (int i = 0; i < particles.Count; i++)
        {
            var p = particles[i];
            if (p.CollisionLayer != 0)
                quadtree.Insert(new(p.Id, p.Position), p.Position);
        }

        for (int i = 0; i < particles.Count; i++)
        {
            var p = particles[i];
            if (p.Alive == false) continue;
            // Physics
            DetectCollisions(p); // Cant parallelize because multiple Team2 can affect the same Team1 particle. Need a collision queue.
            p.CollisionImmunityTime = Math.Max(0, p.CollisionImmunityTime - delta);
            p.Color = new Color(p.Color, (float) (1 - p.CollisionImmunityTime));
            RespectBounds(p, backgroundSize);
            // Move
            p.Position += p.Velocity * delta;
        }

        // Cleanup dead particles
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            var p = particles[i];
            // Remove dead particles
            if (!p.Alive)
            {
                // remove from data (stop iterating it)
                particles.RemoveAt(i);
                particleDict.Remove(p.Id);
                ids.ReleaseId(p.Id);
            }
            //else
            //{
            //    // Update particle rendering
            //    if (p.Sprite != null)
            //    {
            //        p.Sprite.Position = p.Position;
            //        p.Sprite.Modulate = p.Color;
            //    }
            //}
        }
    }

    public virtual void DetectCollisions(Particle p1)
    {
        if (p1.DetectionMask == 0) return; // Skip if no detection mask
        if (p1.CollisionImmunityTime > 0) return; // Skip if immune to collisions
        //var node = quadtree.GetNode(p1.Position); // 30 avg fps with inserting in all overlapping nodes
        var nodes = quadtree.QueryNodes(p1.Position, p1.Size / 2f, []);
        foreach (var node in nodes)
        {
            foreach (var id2 in node.Data)
            {
                if (id2.Id == p1.Id) continue;
                if (particleDict.TryGetValue(id2.Id, out var p2) == false) continue; // Skip if particle not found
                if (p2.Alive == false) continue;
                if (p2.CollisionImmunityTime > 0) continue; // Skip if immune to collisions
                if (p2.CollisionLayer == 0) continue; // Skip if p2 doesnt have collisions
                if ((p1.DetectionMask & p2.CollisionLayer) == 0) continue; // Skip masks dont match
                bool collided = CheckCollision(p1, p2);
                if (p1.CollisionImmunityTime > 0) return; // Cancel the rest because it becomes immune to collisions
            }
        }
    }

    public virtual bool CheckCollision(Particle p1, Particle p2)
    {
        Vector2 deltaPos = p1.Position - p2.Position;
        float distSquared = deltaPos.LengthSquared();
        float particleRadiusSum = p1.Size; // + p2.Size;
        if (distSquared < particleRadiusSum * particleRadiusSum)
        {
            //p1.OnCollision(p1, p2, deltaPos, distSquared, true);
            //p2.OnCollision(p2, p1, deltaPos, distSquared, false);

            // p1
            p1.Velocity = p1.Velocity.Bounce(deltaPos.Normalized());
            p1.CollisionImmunityTime = CollisionImmunityTimer;

            // p2
            p2.Velocity = p2.Velocity.Bounce(-deltaPos.Normalized());
            p2.CollisionImmunityTime = CollisionImmunityTimer;
            //p2.Life--;
            //if (p2.Life <= 0)
            //    p2.Alive = false;

            return true;
        }
        return false;
    }

    public virtual void RespectBounds(Particle p, Vector2 Bounds)
    {
        float vx = p.Velocity.X;
        float vy = p.Velocity.Y;

        if (p.Position.X > Bounds.X)
            vx = Math.Abs(p.Velocity.X) * -1;
        if (p.Position.X < 0)
            vx = Math.Abs(p.Velocity.X);

        if (p.Position.Y > Bounds.Y)
            vy = Math.Abs(p.Velocity.Y) * -1;
        if (p.Position.Y < 0)
            vy = Math.Abs(p.Velocity.Y);

        p.Velocity = new Vector2(vx, vy);
    }

}
