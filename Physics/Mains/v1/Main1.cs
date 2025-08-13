using Godot;
using Godot.Sharp.Extras;
using Physics.Utils;
using System;
using System.Collections.Generic;

namespace Physics.Mains.v1;

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

    public Dictionary<string, object> CustomData { get; set; } = [];

    public T Get<T>(string key = null)
    {
        if (CustomData.TryGetValue(key ?? typeof(T).Name, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default!;
    }
    public void Set<T>(T value)
    {
        CustomData[typeof(T).Name] = value;
    }
    public void Set<T>(string key, T value)
    {
        CustomData[key] = value;
    }

    public const double CollisionImmunityTimer = 0.2;
    public Action<Particle, Particle, Vector2, float, bool> OnCollision = (p, p2, deltaPos, distSquared, isInitiator) => { };
}

public class Main1(Node mainNode, Vector2 backgroundSize) : IGameLoop
{

    #region Nodes
    public Node2D SpritesPool { get; set; } = null!;
    #endregion

    public const float newSize = 32;
    public const float spriteSize = 32;

    public IntId ids = new();
    public Dictionary<int, Particle> particleDict = [];
    public List<Particle> particles;
    public Quadtree<int> quadtree;

    public int ParticleCount => particles.Count;

    public Texture2D texture;

    public virtual void OnReady()
    {
        var size = backgroundSize;
        var bounds = new Rect2(Vector2.Zero, size);
        quadtree = new Quadtree<int>(0, bounds);
        particles = new(10_000);
        SpritesPool = mainNode.GetNode<Node2D>("%SpritesPool");
        texture = GD.Load<Texture2D>("res://Assets/right-arrow.png");
    }

    public virtual void Start()
    {
    }

    public virtual void AddParticles(int count, int team, int detectionMask, int collisionLayer, Color color)
    {
        for (int i = 0; i < count; i++) //int id = baseCount; id < baseCount + count; id++)
        {
            AddParticle(team, detectionMask, collisionLayer, color);
        }
    }

    public virtual int AddParticle(int team, int detectionMask, int collisionLayer, Color color)
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

        var p = new Particle(position, velocity, color)
        {
            id = id,
            Size = newSize,
            DetectionMask = detectionMask,
            CollisionLayer = collisionLayer,
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

                if (p.CollisionLayer != p2.CollisionLayer)
                {
                    p.Life -= 1;
                    if (p.Life <= 0)
                        p.Alive = false;
                }
            };
        }
        particles.Add(p);
        particleDict[p.id] = p;
        AddParticleSprite(p);
        return id;
    }

    public virtual void AddParticleSprite(Particle p)
    {
        var sprite = new Sprite2D()
        {
            Texture = texture,
            Position = p.Position,
            Modulate = p.Color,
            Scale = Vector2.One * p.Size / spriteSize
        };
        p.Sprite = sprite;
        SpritesPool.AddChild(sprite);
    }

    public virtual void RemoveParticles(int count)
    {
        count = Math.Min(count, particles.Count);
        for (int c = 0; c < count; c++)
        {
            int i = particles.Count - 1 - c; // Remove from the end

            var p = particles[i];
            p.Sprite?.QueueFree();
            RemoveParticle(i, p);
        }
    }

    public virtual void PhysicsProcess(double delta)
    {
        UpdateQuadtree(delta);
        UpdatePhysics(delta);
        UpdateNodes(delta);
    }

    public virtual void UpdateQuadtree(double delta)
    {
        // Update quadtree(s)
        quadtree.Clear();
        foreach (var p in particles)
        {
            if (p.CollisionLayer != 0)
                quadtree.Insert(p.id, p.Position); // 160-190 avg fps
            //quadtree.Insert(p.id, p.Position, p.Size / 2f); // 30 avg fps
        }
    }

    public virtual void UpdatePhysics(double delta)
    {
        lock (particles)
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
    }

    public virtual void UpdateNodes(double delta)
    {
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
                    UpdateParticleNode(i, p, delta);
                }
            }
    }

    public virtual void RemoveParticle(int i, Particle p)
    {
        // remove from data (stop iterating it)
        particles.RemoveAt(i);
        particleDict.Remove(p.id);
        ids.ReleaseId(p.id);
    }

    public virtual void OnDeath(int i, Particle p)
    {
        // remove from sprites nodes after timer
        var tween = p.Sprite?.CreateTween();
        var prop = tween.TweenProperty(p.Sprite, "modulate", new Color(1, 1, 1, 0), 0.5f);
        tween.SetEase(Tween.EaseType.Out);
        prop.Finished += () =>
        {
            p.Sprite?.QueueFree();
        };
    }

    public virtual void UpdateParticleNode(int i, Particle p, double delta)
    {
        // Update position
        p.Position += p.Velocity * (float) delta;
        // Update sprite
        p.Sprite.Position = p.Position;
        p.Sprite.Rotation = p.Velocity.Angle();
        p.Sprite.Modulate = p.Color;
    }

    public virtual void DetectCollisions(Particle p1)
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
                if(particleDict.TryGetValue(id, out var p2) == false) continue; // Skip if particle not found
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
            p1.OnCollision(p1, p2, deltaPos, distSquared, true);
            p2.OnCollision(p2, p1, deltaPos, distSquared, false);
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
