using Arch.Core;
using BenchmarkDotNet.Attributes;
using Godot;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using System.Runtime.Intrinsics.X86;
using static Godot.HttpClient;

namespace Benchmarks.Physics;

public record struct CollisionDirection(Vector2 DeltaPosition);
public record struct CollisionEvent(Particle Particle1, Particle Particle2, Vector2 DeltaPosition, float DistanceSquared);

[InProcess]
[MemoryDiagnoser]
public class PhysicsClassThreadBenchmark : PhysicsClassBenchmark
{

    public Dictionary<int, Vector2> collisions;

    public override void GlobalSetup()
    {
        base.GlobalSetup();
        collisions = new(COUNT);
    }

    public override void GlobalCleanup()
    {
        particles.Clear();
        particleDict.Clear();
        quadtree.Clear();
        //base.GlobalCleanup();
        collisions.Clear();

        //Class Thread Frames: 9217, entity iterations: 101387000, avg per frame: 11000, quadtreeInserts: 9217000, destroyed: 0, collisions: 2207302
        //| Type                        | Method               | Mean       | Error    | StdDev    | Gen0    | Gen1    | Allocated |
        //| --------------------------- | -------------------- | ----------:| --------:| ---------:| -------:| -------:| ---------:|
        //| PhysicsClassBenchmark       | 'Class Frame'        | 2,475.5 us | 48.87 us | 106.23 us | 78.1250 | 15.6250 | 962.77 KB |
        //| PhysicsClassThreadBenchmark | 'Class Thread Frame' | 474.9 us   | 9.42 us  | 12.58 us  | 79.1016 | 18.5547 | 969.65 KB |
        Console.WriteLine("Finished PhysicClassThreadBenchmark.");
        Console.WriteLine($"Class Thread Frames: {frame}, entity iterations: {entityIterations}, avg per frame: {(float) entityIterations / frame}, quadtreeInserts: {quadtreeInserts}, destroyed: {destroyed}, collisions: {collisionEvents}");
    }

    [BenchmarkCategory("PhysicsFrame")]
    [Benchmark(Description = "Class Thread Frame")]
    public override void PhysicsFrame()
    {
        frame++;

        quadtree.Clear();
        for (int i = 0; i < particles.Count; i++)
        {
            var p = particles[i];
            if (p.CollisionLayer != 0)
            {
                quadtreeInserts++;
                quadtree.Insert(new(p.Id, p.Position), p.Position);
            }
        }

        Parallel.ForEach(particles, p =>
        {
            //Interlocked.Increment(ref entityIterations);
            if (p.Alive == false) return;
            RespectBounds(p, backgroundSize);
            // Move
            p.Position += p.Velocity * delta;
            p.CollisionImmunityTime = Math.Max(0, p.CollisionImmunityTime - delta);
            p.Color = new Color(p.Color, (float) (1 - p.CollisionImmunityTime));
            // Physics
            DetectCollisionEvents(p); // Cant parallelize because multiple Team2 can affect the same Team1 particle. Need a collision queue.
        });

        // every particle affected by collisions
        foreach (var entry in collisions)
        {
            if (particleDict.TryGetValue(entry.Key, out var p))
            {
                collisionEvents++;
                var deltaPos = entry.Value; //collisions[id]; //.DeltaPosition;
                p.Velocity = p.Velocity.Bounce(deltaPos.Normalized());
                //RespectBounds(p, backgroundSize);
            }
        }
        collisions.Clear();

        // Cleanup dead particles
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            var p = particles[i];
            // Remove dead particles
            if (!p.Alive)
            {
                destroyed++;
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

    public void DetectCollisionEvents(Particle p1)
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
                if (p2.CollisionLayer == 0) continue; // Skip if p2 doesnt have collisions
                if (p2.CollisionImmunityTime > 0) continue; // Skip if immune to collisions
                if ((p1.DetectionMask & p2.CollisionLayer) == 0) continue; // Skip masks dont match
                CheckCollisionEvent(p1, p2);
                if (p1.CollisionImmunityTime > 0) return; // Cancel the rest because it becomes immune to collisions
            }
        }
    }

    public void CheckCollisionEvent(Particle p1, Particle p2)
    {
        Vector2 deltaPos = p1.Position - p2.Position;
        float distSquared = deltaPos.LengthSquared();
        float particleRadiusSum = p1.Size; // p1.Size / 2 + p2.Size / 2;

        bool collided = distSquared < particleRadiusSum * particleRadiusSum;
        if (collided)
        {
            //var collision = new CollisionEvent(p1, p2, deltaPos, distSquared);
            //var collision = new CollisionDirection(deltaPos);
            p2.CollisionImmunityTime = CollisionImmunityTimer;
            p1.CollisionImmunityTime = CollisionImmunityTimer;
            collisions[p1.Id] = deltaPos.Normalized(); // could add the opposite particle id
            collisions[p2.Id] = -deltaPos.Normalized();
            //    //p2.Life--;
            //    //if (p2.Life <= 0)
            //    //    p2.Alive = false;
            //return collision;
        }
        //return null;
    }

}
