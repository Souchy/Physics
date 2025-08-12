using Godot;
using Godot.Sharp.Extras;
using Physics.Utils;
using System;
using System.Collections.Generic;

namespace Physics.Mains.v2;

public class Particle(Vector2 position, Vector2 velocity, Color color)
{
    public int id;
    public int Life { get; set; } = 10;
    public bool Alive { get; set; } = true;
    public double CollisionImmunityTime = 0.5;
    public Vector2 Position { get; set; } = position;
    public Vector2 Velocity { get; set; } = velocity;
    public Color Color { get; set; } = color;
    public Node2D? Sprite { get; set; }
    public float Size = 32f;
    public int DetectionMask = 0; // Mask to detect other objects. If >0, detect objects with matching layers.
    public int CollisionLayer = 0; // Layers this object is in

    public const double CollisionImmunityTimer = 0.2;
    public Action<Particle, Particle, Vector2, float, bool> OnCollision = (p, p2, deltaPos, distSquared, isInitiator) => { };
}

public class MainThreadInsert(Node mainNode, Vector2 backgroundSize) : IGameLoop
{
    #region Nodes
    private Node2D SpritesPool { get; set; } = null!;
    #endregion

    private IntId ids = new();
    public Dictionary<int, Particle> particleDict = [];
    public List<Particle> particles;
    public Quadtree<int> quadtree;

    public int ParticleCount => particles.Count;

    private Quadtree<int> quadtreeSwap;
    private Quadtree<int> quadtreeThread;

    public void OnReady()
    {
        var size = backgroundSize;
        var bounds = new Rect2(Vector2.Zero, size);
        quadtree = new Quadtree<int>(0, bounds);
        particles = new(10_000);
        SpritesPool = mainNode.GetNode<Node2D>("%SpritesPool");
    }

    public void Start()
    {
        Scheduler.RunTimed(16, (delta) =>
        {
            var bounds = new Rect2(Vector2.Zero, backgroundSize);
            quadtreeSwap = quadtreeThread;
            quadtreeThread = new(0, bounds);
            // Update quadtree(s)
            foreach (var p in particles)
            {
                if (p?.CollisionLayer != 0)
                    quadtreeThread.Insert(p.id, p.Position);
            }
        });
    }

    public void AddParticles(int count, int team, int detectionMask, int collisionLayer, Color color)
    {
        var texture = GD.Load<Texture2D>("res://Assets/right-arrow.png");
        int baseCount = particles.Count;
        for (int i = 0; i < count; i++) //int id = baseCount; id < baseCount + count; id++)
        {
            Vector2 position = Vector2.Zero;
            // set position randomly on a circle around the origin
            float angle = GD.Randf() * Mathf.Tau;
            position.X = Mathf.Cos(angle);
            position.Y = Mathf.Sin(angle);
            position *= 500;

            Vector2 velocity = -position.Normalized() * 200f;

            position += backgroundSize / 2f; // Offset to center the particles in the background

            int id = ids.GetNextId(); // Get a unique ID for the particle

            const float newSize = 32;
            const float spriteSize = 32;
            var p = new Particle(position, velocity, color)
            {
                id = id,
                Size = newSize,
                DetectionMask = detectionMask,
                CollisionLayer = collisionLayer,
                Sprite = new Sprite2D()
                {
                    Texture = texture,
                    Position = position,
                    Modulate = color,
                    Scale = Vector2.One * newSize / spriteSize
                }
            };
            if (team == 2)
            {
                p.OnCollision = (p, p2, deltaPos, distSquared, isInitiator) =>
                {
                    // Bounce the velocity off the collision normal
                    p.Velocity = p.Velocity.Bounce(deltaPos.Normalized());
                    // immune to collisions
                    p.CollisionImmunityTime = Particle.CollisionImmunityTimer;
                };
            }
            if (team == 1)
            {
                p.OnCollision = (p, p2, deltaPos, distSquared, isInitiator) =>
                {
                    // Bounce + immune
                    p.Velocity = p.Velocity.Bounce(-deltaPos.Normalized());
                    p.CollisionImmunityTime = Particle.CollisionImmunityTimer;

                    //if (p.CollisionLayer != p2.CollisionLayer)
                    //{
                    //    p.Life -= 1;
                    //    if (p.Life <= 0)
                    //        p.Alive = false;
                    //}
                };
            }
            particles.Add(p);
            particleDict[p.id] = p;
            SpritesPool.AddChild(p.Sprite);
        }
    }
    public void RemoveParticles(int count)
    {
        count = Math.Min(count, particles.Count);
        for (int i = 0; i < count; i++)
        {
            var p = particles[particles.Count - 1];
            p.Sprite?.QueueFree();
            particles.RemoveAt(particles.Count - 1);
            particleDict.Remove(p.id);
            ids.ReleaseId(p.id);
            //quadtree.Remove(p.id, p.Position);
        }
    }

    public void PhysicsProcess(double delta)
    {
        if (quadtreeSwap != null)
        {
            // Swap quadtree references
            quadtree = quadtreeSwap;
            quadtreeSwap = null;
        }

        // Update particle physics + nodes
        foreach (var p in particles)
        {
            if (p.Alive == false) continue;
            // Physics
            DetectCollisions(p);
            p.CollisionImmunityTime = Math.Max(0, p.CollisionImmunityTime - delta);
            p.Color = new Color(p.Color, (float) (1 - p.CollisionImmunityTime));

            RespectBounds(p, backgroundSize);
        }

        // Remove dead particles
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            var p = particles[i];
            if (!p.Alive)
            {
                // remove from data (stop iterating it)
                particles.RemoveAt(i);
                particleDict.Remove(p.id);
                ids.ReleaseId(p.id);

                // remove from sprites nodes after timer
                var tween = p.Sprite?.CreateTween();
                var prop = tween.TweenProperty(p.Sprite, "modulate", new Color(1, 1, 1, 0), 0.5f);
                tween.SetEase(Tween.EaseType.Out);
                prop.Finished += () =>
                {
                    p.Sprite?.QueueFree();
                };
            }
            else
            {
                // Update position
                p.Position += p.Velocity * (float) delta;
                // Update sprite
                p.Sprite.Position = p.Position;
                p.Sprite.Rotation = p.Velocity.Angle();
                p.Sprite.Modulate = p.Color;
            }
        }
    }

    public void DetectCollisions(Particle p1)
    {
        if (p1.DetectionMask == 0) return; // Skip if no detection mask
        if (p1.CollisionImmunityTime > 0) return; // Skip if immune to collisions
        //var node = quadtree.GetNode(p1.Position); // 30 avg fps with inserting in all overlapping nodes
        var nodes = quadtree.QueryNodes(p1.Position, p1.Size / 2f, []);
        foreach (var node in nodes)
        {
            foreach (var id in node.Data)
            {
                if (id == p1.id) continue;
                var p2 = particleDict[id];
                if (p2.Alive == false) continue;
                if (p2.CollisionImmunityTime > 0) continue; // Skip if immune to collisions
                if (p2.CollisionLayer == 0) continue; // Skip if p2 doesnt have collisions
                if ((p1.DetectionMask & p2.CollisionLayer) == 0) continue; // Skip masks dont match
                bool collided = CheckCollision(p1, p2);
                if (p1.CollisionImmunityTime > 0) return; // Cancel the rest because it becomes immune to collisions
            }
        }
    }

    public static bool CheckCollision(Particle p1, Particle p2)
    {
        Vector2 deltaPos = p1.Position - p2.Position;
        float distSquared = deltaPos.LengthSquared();
        float particleRadiusSum = p1.Size; // + p2.Size;
        if (distSquared < particleRadiusSum * particleRadiusSum)
        {
            p1.OnCollision(p1, p2, deltaPos, distSquared, true);
            p2.OnCollision(p2, p1, deltaPos, distSquared, false);
            return true;
        }
        return false;
    }

    public static void RespectBounds(Particle p, Vector2 Bounds)
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
