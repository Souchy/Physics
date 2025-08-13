using Arch.Core;
using Arch.Core.Extensions;
using Godot;
using Physics.Mains.v1;
using Physics.Mains.v5_Arch;
using Physics.Utils;
using System;
using System.Collections.Generic;

namespace Physics.Mains.v5;

/// <summary>
/// https://arch-ecs.gitbook.io/arch
/// </summary>
public class MainArch(Node mainNode, Vector2 backgroundSize) : IGameLoop //: MainMultimesh(mainNode, backgroundSize)
{
    public const float newSize = 32;
    public const float spriteSize = 32;
    public const double CollisionImmunityTimer = 0.2;

    #region Nodes
    public Node2D SpritesPool { get; set; } = null!;
    #endregion
    public MultiMeshInstance2D MultiMeshInstance;
    public MultiMesh Multimesh;
    public Texture2D texture;

    public int ParticleCount => particles.Count; //particleDict.Count;

    public World world;
    public QueryDescription movementQuery = new QueryDescription().WithAll<Position, Velocity>();

    public IntId ids = new();
    public List<Entity> particles;
    //public Dictionary<int, Entity> particleDict = [];
    public Quadtree<Entity> quadtree;
    private Quadtree<Entity> quadtreeSwap;
    private Quadtree<Entity> quadtreeThread;


    public void OnReady()
    {
        var size = backgroundSize;
        var bounds = new Rect2(Vector2.Zero, size);
        quadtree = new EntityQuadtree(0, bounds);
        particles = new(10_000);

        SpritesPool = mainNode.GetNode<Node2D>("%SpritesPool");
        texture = GD.Load<Texture2D>("res://Assets/right-arrow.png");

        MultiMeshInstance = new MultiMeshInstance2D()
        {
            Texture = texture,
            Multimesh = new MultiMesh()
            {
                TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
                InstanceCount = 0,
                UseColors = true,
                Mesh = new QuadMesh()
                {
                    Size = new Vector2(newSize, newSize),
                },
            },
        };
        Multimesh = MultiMeshInstance.Multimesh;
        SpritesPool.AddChild(MultiMeshInstance);

        world = World.Create();
    }

    public void Start()
    {
        Scheduler.RunTimed(16, (delta) =>
        {
            var bounds = new Rect2(Vector2.Zero, backgroundSize);
            quadtreeSwap = quadtreeThread;
            quadtreeThread = new(0, bounds);
            List<Entity> copy;
            lock (particles)
            {
                copy = new(particles);
            }
            // Update quadtree(s)
            foreach (var p in copy)
            {
                if (p.Get<CollisionLayer>().Value != 0)
                    quadtreeThread.Insert(p, p.Get<Position>().Value);
            }
        });
    }

    public void AddParticles(int count, int team, int detectionMask, int collisionLayer, Color color)
    {
        // Manage visible instances
        {
            int toVisible = Multimesh.InstanceCount - Multimesh.VisibleInstanceCount;
            toVisible = Math.Min(toVisible, count);
            Multimesh.VisibleInstanceCount += toVisible;
            count -= toVisible;

            Multimesh.InstanceCount += count;
            Multimesh.VisibleInstanceCount += count;
        }

        for (int i = 0; i < count; i++)
            AddParticle(team, detectionMask, collisionLayer, color);
    }

    public int AddParticle(int team, int detectionMask, int collisionLayer, Color color)
    {
        int id = ids.GetNextId(); // Get a unique ID for the particle

        Vector2 position = Vector2.Zero;
        // set position randomly on a circle around the origin
        float angle = GD.Randf() * Mathf.Tau;
        position.X = Mathf.Cos(angle);
        position.Y = Mathf.Sin(angle);
        position *= 500;

        // Velocity = -position when centered
        Vector2 velocity = -position.Normalized() * 200f;
        // Offset to center the particles in the background
        position += backgroundSize / 2f;

        var entt = world.Create(
            new Id(id),
            new Position(position),
            new Velocity(velocity),
            new Size(newSize),
            new Modulate(color),
            new Alive(true),
            new Life(10),
            new CollisionImmunityTime(0),
            new CollisionLayer(collisionLayer),
            new DetectionMask(detectionMask),
            new OnCollision((p, p2, deltaPos, distSquared, isInitiator) =>
            {
                ref var velocity = ref p.Get<Velocity>();

                // immune to collisions
                p.Get<CollisionImmunityTime>().Value = CollisionImmunityTimer;
                if (isInitiator)
                {
                    // Bounce the velocity off the collision normal
                    velocity.Value = velocity.Value.Bounce(deltaPos.Normalized());
                }
                else
                {
                    velocity.Value = velocity.Value.Bounce(-deltaPos.Normalized());
                    if (p.Get<CollisionLayer>().Value != p2.Get<CollisionLayer>().Value)
                    {
                        ref var life = ref p.Get<Life>();
                        life.Value -= 1;
                        if (life.Value <= 0)
                            p.Get<Alive>().Value = false;
                    }
                }
            })
        );

        particles.Add(entt);
        //particleDict[id] = entt;
        //AddParticleSprite(p);
        return id;
    }

    public void RemoveParticles(int count)
    {
        Multimesh.VisibleInstanceCount = Math.Max(0, Multimesh.VisibleInstanceCount - count);
        //base.RemoveParticles(count);
        count = Math.Min(count, ParticleCount);
        for (int c = 0; c < count; c++)
        {
            int i = ParticleCount - 1 - c; // Remove from the end

            var p = particles[i];
            RemoveParticle(i, p);
        }
    }
    public void RemoveParticle(int i, Entity p)
    {
        //base.RemoveParticle(i, p);
        Multimesh.VisibleInstanceCount = Math.Max(0, Multimesh.VisibleInstanceCount - 1);
        var id = p.Get<Id>().Value;
        particles.RemoveAt(i);
        //particleDict.Remove(id);
        ids.ReleaseId(id);
    }

    public void PhysicsProcess(double delta)
    {
        //base.PhysicsProcess(delta);
        UpdateQuadtree(delta);
        UpdatePhysics(delta);
        UpdateNodes(delta);
    }

    public virtual void UpdateQuadtree(double delta)
    {
        // Update quadtree(s)
        //quadtree.Clear();
        //foreach (var p in particles)
        //{
        //    if (p.Get<CollisionLayer>().Value != 0)
        //        quadtree.Insert(p, p.Get<Position>().Value); // 160-190 avg fps
        //    //quadtree.Insert(p.id, p.Position, p.Size / 2f); // 30 avg fps
        //}
        if (quadtreeSwap != null)
        {
            // Swap quadtree references
            quadtree = quadtreeSwap;
            quadtreeSwap = null;
        }
    }

    public virtual void UpdatePhysics(double delta)
    {
        lock (particles)
            // Update particle physics + nodes
            foreach (var p in particles)
            {
                if (p.Get<Alive>().Value == false) continue;
                // Physics
                DetectCollisions(p);
                ref var collisionImmunityTime = ref p.Get<CollisionImmunityTime>();
                collisionImmunityTime.Value = Math.Max(0, collisionImmunityTime.Value - delta);
                ref var color = ref p.Get<Modulate>();
                color.Value = new Color(color.Value, (float) (1 - collisionImmunityTime.Value));

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
                if (p.Get<Alive>().Value == false)
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

    public void UpdateParticleNode(int i, Entity p, double delta)
    {
        ref var position = ref p.Get<Position>();
        ref var velocity = ref p.Get<Velocity>();
        // Update position
        position.Value += velocity.Value * (float) delta;

        //// Update sprite
        Multimesh.SetInstanceTransform2D(i, new Transform2D(velocity.Value.Angle(), position.Value));
        Multimesh.SetInstanceColor(i, p.Get<Modulate>().Value);
    }

    public void AddParticleSprite(Entity p)
    {
        // dont.
    }


    public void OnDeath(int i, Entity p)
    {
        // cant tween the multimesh.
        // removing the particle from the list will remove it from the multimesh render.

        // if we want animations,
        //      use customdata + a shader
        //      remove only after the animation is done.
    }

    public virtual void DetectCollisions(Entity p1)
    {
        var (id1, detectionMask1, collisionImmunity1) = p1.Get<Id, DetectionMask, CollisionImmunityTime>();

        if (detectionMask1.Value == 0) return; // Skip if no detection mask
        if (collisionImmunity1.Value > 0) return; // Skip if immune to collisions
        //var node = quadtree.GetNode(p1.Position); // 30 avg fps with inserting in all overlapping nodes

        var (position1, size1) = p1.Get<Position, Size>();
        var nodes = quadtree.QueryNodes(position1.Value, size1.Value / 2f, []);
        foreach (var node in nodes)
        {
            foreach (var p2 in node.Data)
            {
                if (p2.Get<Id>().Value == id1.Value) continue;
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
            p1.Get<OnCollision>().Value(p1, p2, deltaPos, distSquared, true);
            p2.Get<OnCollision>().Value(p2, p1, deltaPos, distSquared, false);
            return true;
        }
        return false;
    }

    public virtual void RespectBounds(Entity p, Vector2 Bounds)
    {
        ref var position = ref p.Get<Position>();
        ref var velocity = ref p.Get<Velocity>();

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
