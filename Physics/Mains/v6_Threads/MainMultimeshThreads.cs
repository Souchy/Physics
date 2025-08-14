using Godot;
using Physics.Mains.v1;
using Physics.Mains.v2;
using Physics.Mains.v3_Multimesh;
using Physics.Mains.v5_Arch;
using Physics.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Physics.Mains.v3;

public class MainMultimeshThreads(Node mainNode, Vector2 backgroundSize) : MainThreadInsert(mainNode, backgroundSize)
{
    public MultimeshSpawner spawner;

    public override void OnReady()
    {
        base.OnReady();
        spawner = new(texture, new Vector2(newSize, newSize), MultimeshSpawnerFlags.Color);
        SpritesPool.AddChild(spawner.MultiMeshInstance);
    }


    #region Updates
    public override void PhysicsProcess(double delta)
    {
        UpdateQuadtree(delta);
        UpdatePhysics(delta);
        UpdateNodes(delta);
    }

    public override void UpdateQuadtree(double delta)
    {
        if (quadtreeSwap != null)
        {
            // Swap quadtree references
            quadtree = quadtreeSwap;
            quadtreeSwap = null;
        }
    }

    private Collision[] allCollisions = new Collision[500];
    public override void UpdatePhysics(double delta)
    {
        //lock (particles)
        //    // Update particle physics + nodes
        //    foreach (var p in particles)
        //    {
        //        if (p.Alive == false) continue;
        //        // Physics
        //        DetectCollisions(p);
        //        p.CollisionImmunityTime = Math.Max(0, p.CollisionImmunityTime - delta);
        //        p.Color = new Color(p.Color, (float) (1 - p.CollisionImmunityTime));

        //        RespectBounds(p, backgroundSize);
        //    }

        //int chunkSize = 500;
        //int chunkCount = (ParticleCount + chunkSize - 1) / chunkSize;

        Parallel.For(0, ParticleCount, i =>
        //Parallel.For(0, chunkCount, chunkIdx =>
        {
            //int start = chunkIdx * chunkSize;
            //int end = Math.Min(start + chunkSize, ParticleCount);

            //for (int i = start; i < end; i++)
            //{
                var p1 = particles[i];
                if (!p1.Alive) return; // pense qu'on en a plus besoin vu que les collisions qui kill arrivent après

                // 2
                RespectBounds(p1, backgroundSize);

                // 3
                p1.CollisionImmunityTime = Math.Max(0, p1.CollisionImmunityTime - delta);
                p1.Color = new Color(p1.Color, (float) (1 - p1.CollisionImmunityTime));
                // Update position
                p1.Position += p1.Velocity * (float) delta;

                // 1
                var nodesInArea = GetParticlesInArea(p1);
                if (nodesInArea != null)
                {
                    var collisions = nodesInArea.SelectMany(n => n.Data)
                        .Where(id => CanCollide(p1, id))
                        .Select(id => particleDict[id])
                        .Select(p2 => GetCollision(p1, p2))
                        .Where(collision => collision.distSquared < p1.Size)
                        .FirstOrDefault();
                    allCollisions[i] = collisions;
                }
            //}
        }
        );

        foreach (var collision in allCollisions)
        {
            if (collision.p1 == null) continue;

            var p1 = collision.p1;
            var p2 = collision.p2;

            if (p1.CollisionImmunityTime > 0) continue;
            if (p2.CollisionImmunityTime > 0 || !p2.Alive) continue;

            p1.OnCollision(p1, p2, collision.delta, collision.distSquared, true);
            p2.OnCollision(p2, p1, collision.delta, collision.distSquared, false);
        }
    }

    public record struct Collision(Particle p1, Particle p2, Vector2 delta, float distSquared);

    public override void UpdateNodes(double delta)
    {
        int mmi = 0;
        lock (particles)
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];
                // Remove dead particles
                if (!p.Alive)
                {
                    // remove from data (stop iterating it)
                    RemoveParticle(i, p);
                    // remove from sprites nodes after timer
                    OnDeath(i, p);
                }
                // Update alive particles
                else
                {
                    UpdateParticleNode(mmi++, p, delta);
                }
            }
    }

    public override void UpdateParticleNode(int i, Particle p, double delta)
    {
        // Update position
        //p.Position += p.Velocity * (float) delta;
        //// Update sprite
        spawner.UpdateInstance(i, p.Position, p.Velocity, p.Color);
    }

    #endregion

    public override void AddParticles(int count, int team, int detectionMask, int collisionLayer, Color color)
    {
        // Manage visible instances
        spawner.AddInstances(count);
        base.AddParticles(count, team, detectionMask, collisionLayer, color);
    }

    public override void RemoveParticles(int count)
    {
        spawner.RemoveInstances(count);
        base.RemoveParticles(count);
    }

    public override void RemoveParticle(int i, Particle p)
    {
        spawner.RemoveInstance();
        base.RemoveParticle(i, p);
    }

    public override void AddParticleSprite(Particle p)
    {
        // dont.
    }

    public override void OnDeath(int i, Particle p)
    {
        // cant tween the multimesh.
        // removing the particle from the list will remove it from the multimesh render.

        // if we want animations,
        //      use customdata + a shader
        //      remove only after the animation is done.
    }

    public IEnumerable<Quadtree<int>> GetParticlesInArea(Particle p1)
    {
        if (p1.DetectionMask == 0) return null; // Skip if no detection mask
        if (p1.CollisionImmunityTime > 0) return null; // Skip if immune to collisions
        var nodes = quadtree.QueryNodes(p1.Position, p1.Size, []);
        return nodes;
    }

    public void DetectCollisionsLater(Particle p1)
    {
        if (p1.DetectionMask == 0) return; // Skip if no detection mask
        if (p1.CollisionImmunityTime > 0) return; // Skip if immune to collisions
        //var node = quadtree.GetNode(p1.Position); // 30 avg fps with inserting in all overlapping nodes
        var nodes = quadtree.QueryNodes(p1.Position, p1.Size / 2f, []);
        var particles = nodes.SelectMany(n => n.Data)
            .ToList();
        foreach (var node in nodes)
        {
            foreach (var id in node.Data)
            {
                if (particleDict.TryGetValue(id, out var p2) == false) continue; // Skip if particle not found

                bool collided = CheckCollision(p1, p2);
                if (p1.CollisionImmunityTime > 0) return; // Cancel the rest because it becomes immune to collisions
            }
        }
    }

    public bool CanCollide(Particle p1, int id) //Particle p2)
    {
        if (!particleDict.TryGetValue(id, out var p2)) return false; // Skip if particle not found
        if (p1.id == p2.id) return false;
        //if (particleDict.TryGetValue(id, out var p2) == false) return; // Skip if particle not found
        if (p2.Alive == false) return false;
        if (p2.CollisionImmunityTime > 0) return false; // Skip if immune to collisions
        if (p2.CollisionLayer == 0) return false; // Skip if p2 doesnt have collisions
        if ((p1.DetectionMask & p2.CollisionLayer) == 0) return false; // Skip masks dont match
        return true;
    }

    public Collision GetCollision(Particle p1, Particle p2)
    {
        Vector2 deltaPos = p1.Position - p2.Position;
        float distSquared = deltaPos.LengthSquared();
        return new(p1, p2, deltaPos, distSquared);
    }

}
