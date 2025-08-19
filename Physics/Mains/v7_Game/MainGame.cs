using Godot;
using Physics.Mains.v3_Multimesh;
using PhysicsLib.Godot;
using PhysicsLib.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Godot.BaseMaterial3D;

namespace Physics.Mains.v7_Game;

public record struct PositionId(int Id, Vector2 Position) : IHasPosition;
public record struct Spatial(Vector2 Position, Vector2 Velocity);
//public record struct Life(int Current, int Max);
public record struct CollisionShape(float Radius, int CollisionLayer, int CollisionMask, float CollisionImmunityTimer);
public record struct TextureParam(string Path, Vector2 StepsCount);
public record struct Collision(int Id1, int Id2, Vector2 Normal);

public record struct ValueAnimation<T>(T Current, T Target, float Duration, float Elapsed)
{
    public readonly bool IsFinished => Elapsed >= Duration;
    public ValueAnimation<T> Update(float delta)
    {
        if (IsFinished)
        {
            Current = Target;
            Elapsed = Duration; // Ensure Elapsed is set to Duration when finished
        }
        else
        {
            float remaining = Duration - Elapsed;
            Current = Interpolate(Current, Target, delta / remaining);
            Elapsed += delta;
        }
        return this;
    }
    private static T Interpolate(T current, T target, float t)
    {
        // Assuming T is a struct with a method to interpolate
        // This needs to be implemented based on the type of T
        if (current is float floatCurrent && target is float floatTarget)
        {
            return (T) (object) Mathf.Lerp(floatCurrent, floatTarget, t);
        }
        else if (current is Vector2 vectorCurrent && target is Vector2 vectorTarget)
        {
            return (T) (object) new Vector2(
                Mathf.Lerp(vectorCurrent.X, vectorTarget.X, t),
                Mathf.Lerp(vectorCurrent.Y, vectorTarget.Y, t)
            );
        }
        else if (current is Color colorCurrent && target is Color colorTarget)
        {
            return (T) (object) new Color(
                Mathf.Lerp(colorCurrent.R, colorTarget.R, t),
                Mathf.Lerp(colorCurrent.G, colorTarget.G, t),
                Mathf.Lerp(colorCurrent.B, colorTarget.B, t),
                Mathf.Lerp(colorCurrent.A, colorTarget.A, t)
            );
        }
        else
        {
            throw new InvalidOperationException($"Interpolation not defined for type {typeof(T)}");
        }
    }
}

public enum CollisionLayers
{
    None = 0,
    Player = 1 << 0,
    Enemy = 1 << 1,
    Projectile = 1 << 2,
    Terrain = 1 << 3,
    All = Player | Enemy | Projectile | Terrain
}

public class MainGame(Node mainNode, Vector2 backgroundSize) : IGameLoop
{
    #region Nodes
    public Node2D SpritesPool { get; set; } = null!;
    #endregion

    public const float newSize = 32;
    public const float spriteSize = 32;
    public const int CAPACITY = 10_000;

    public int ParticleCount => activeEntities.Count;


    public IntId ids = new();
    public List<int> activeEntities;

    public Spatial[] spatials;
    public bool[] active;
    public CollisionShape[] collisionShapes;
    public TextureParam[] textures;
    public Color[] colors;
    public Dictionary<int, int> entityLife;
    public Dictionary<int, int> entityHurtAnimation;
    public Collision[] allCollisions;


    public Dictionary<int, QuadtreeWithPosStruct<PositionId>> quadtrees;
    public Dictionary<string, MultimeshSpawner> spawners;

    public void OnReady()
    {
        activeEntities = new(CAPACITY);
        spatials = new Spatial[CAPACITY];
        collisionShapes = new CollisionShape[CAPACITY];
        textures = new TextureParam[CAPACITY];
        colors = new Color[CAPACITY];
        allCollisions = new Collision[1000];

        entityLife = new(CAPACITY);
        quadtrees = new(10)
        {
            [(int) CollisionLayers.Player] = new(0, new Rect2(Vector2.Zero, backgroundSize)),
            [(int) CollisionLayers.Enemy] = new(0, new Rect2(Vector2.Zero, backgroundSize)),
            [(int) (CollisionLayers.Player | CollisionLayers.Projectile)] = new(0, new Rect2(Vector2.Zero, backgroundSize)),
            [(int) (CollisionLayers.Enemy | CollisionLayers.Projectile)] = new(0, new Rect2(Vector2.Zero, backgroundSize)),
        };
        spawners = new(10);

        SpritesPool = mainNode.GetNode<Node2D>("%SpritesPool");

        //var texture = GD.Load<Texture2D>("res://Assets/right-arrow.png");
        //var spawner = new MultimeshSpawner(texture, new Vector2(newSize, newSize), MultimeshSpawnerFlags.Color);
        //SpritesPool.AddChild(spawner.MultiMeshInstance);
    }

    public void Start()
    {
    }



    public void PhysicsProcess(double delta)
    {
        //allCollisions.Clear();
        //allCollisions = new Collision[ParticleCount];
        allCollisions = new Collision[1000];
        UpdateQuadtree();
        UpdatePhysics(delta);
        UpdateNodes(delta);
        UpdateCollisions(delta);
    }

    public void UpdateQuadtree()
    {
        foreach (var quadtree in quadtrees.Values)
            quadtree.Clear();
        foreach (int id in activeEntities) //int i = 0; i < ParticleCount; i++)
        {
            var shape = collisionShapes[id];
            if (shape.CollisionLayer == 0)
                continue;
            var spatial = spatials[id];
            var positionid = new PositionId(id, spatial.Position);
            quadtrees[shape.CollisionLayer].Insert(positionid);
        }
    }

    public void UpdatePhysics(double delta)
    {
        Parallel.For(0, activeEntities.Count, i =>
        {
            int id1 = activeEntities[i];

            ref var spatial1 = ref spatials[id1];

            RespectBounds(ref spatial1, backgroundSize);

            spatial1.Position += spatial1.Velocity * (float) delta;

            //allCollisions[id1] = new();

            ref var shape1 = ref collisionShapes[id1];
            if (shape1.CollisionImmunityTimer > 0f)
            {
                shape1.CollisionImmunityTimer -= (float) delta;
                if (shape1.CollisionImmunityTimer < 0f)
                    shape1.CollisionImmunityTimer = 0f;
            }
            else
            {
                if (shape1.CollisionMask == 0)
                    return; //continue; // No collisions to check

                // Check collissions
                var quadtree = quadtrees[shape1.CollisionMask];
                var results = quadtree.QueryNodes(spatial1.Position, shape1.Radius, []);
                foreach (var p2 in results.SelectMany(n => n.Data))
                {
                    if (p2.Id == id1) //entities[i])
                        continue;
                    //var otherIndex = entities.IndexOf(p2.Id);
                    int id2 = p2.Id;

                    var shape2 = collisionShapes[id2];
                    if (shape2.CollisionImmunityTimer > 0f)
                        continue;
                    var spatial2 = spatials[id2];
                    var deltaPos = spatial1.Position - spatial2.Position;
                    var distSquared = deltaPos.LengthSquared();
                    float maxDist = shape1.Radius + shape2.Radius;
                    if (distSquared < maxDist * maxDist)
                    {
                        var normal = deltaPos.Normalized();
                        allCollisions[id1] = new Collision(id1, id2, normal);
                        collisionShapes[id1].CollisionImmunityTimer = 0.1f;
                        collisionShapes[id2].CollisionImmunityTimer = 0.1f;
                        break;
                    }
                }
            }
            //}
        }
        );
    }

    public virtual void RespectBounds(ref Spatial spatial, Vector2 Bounds)
    {
        float vx = spatial.Velocity.X;
        float vy = spatial.Velocity.Y;

        if (spatial.Position.X > Bounds.X)
            vx = Math.Abs(spatial.Velocity.X) * -1;
        if (spatial.Position.X < 0)
            vx = Math.Abs(spatial.Velocity.X);

        if (spatial.Position.Y > Bounds.Y)
            vy = Math.Abs(spatial.Velocity.Y) * -1;
        if (spatial.Position.Y < 0)
            vy = Math.Abs(spatial.Velocity.Y);

        spatial.Velocity = new Vector2(vx, vy);
    }

    public void UpdateCollisions(double delta)
    {
        foreach (var (Id1, Id2, Normal) in allCollisions)
        {
            if (Id1 == Id2) continue;
            ref var spatial1 = ref spatials[Id1];
            ref var spatial2 = ref spatials[Id2];
            spatial1.Velocity = spatial1.Velocity.Bounce(Normal);
            spatial2.Velocity = spatial2.Velocity.Bounce(-Normal);
            //colors[Id1] = new Color(1, 0, 0); // Red for damage
            //colors[Id2] = new Color(1, 0, 0); // Red for damage

            // Damage
            if (entityLife.TryGetValue(Id1, out var lifeValue1))
            {
                lifeValue1--;
                entityLife[Id1] = lifeValue1;
            }
            if (entityLife.TryGetValue(Id2, out var lifeValue2))
            {
                lifeValue2--;
                entityLife[Id2] = lifeValue2;
            }
        }
    }

    public void UpdateNodes(double delta)
    {
        foreach (var spawner in spawners.Values)
        {
            spawner.CurrentIndex = 0;
        }

        for (int i = ParticleCount - 1; i >= 0; i--)
        {
            int id = activeEntities[i];
            var textureParams = textures[id];
            var hasSpawner = spawners.TryGetValue(textureParams.Path, out var spawner);

            if (entityLife.TryGetValue(id, out int life) && life <= 0)
            {
                // Remove particle
                ids.ReleaseId(id);
                activeEntities.RemoveAt(i); //.Remove(id);
                //entities.RemoveAt(i);
                //spatials.RemoveAt(i);
                //collisionShapes.RemoveAt(i);
                //textures.RemoveAt(i);
                //colors.RemoveAt(i);
                entityLife.Remove(id);
                if (hasSpawner)
                    spawner.RemoveInstance();
                continue;
            }
            else
            {
                if (hasSpawner)
                {
                    var spatial = spatials[id];
                    var color = colors[id];
                    spawner.UpdateInstance(spatial.Position, spatial.Velocity, color);
                }
            }
        }
    }

    public void AddParticle(int team, int detectionMask, int collisionLayer, Color color)
    {
        Vector2 position = Vector2.Zero;
        // set position randomly on a circle around the origin
        float angle = GD.Randf() * Mathf.Tau;
        position.X = Mathf.Cos(angle);
        position.Y = Mathf.Sin(angle);
        position *= 500;
        Vector2 velocity = -position.Normalized() * 200f;
        position += backgroundSize / 2f;


        int id = ids.GetNextId();
        // collision shape
        if (collisionShapes.Length <= id) Array.Resize(ref collisionShapes, id + 1000);
        collisionShapes[id] = new CollisionShape(newSize / 2f, collisionLayer, detectionMask, 0f);
        // color
        if (colors.Length <= id) Array.Resize(ref colors, id + 1000);
        //colors[id] = color;
        // spatial
        if (spatials.Length <= id) Array.Resize(ref spatials, id + 1000);
        spatials[id] = new Spatial(position, velocity);

        TextureParam textureParam = new();
        float tint = 0.7f;
        if (team == 2) // 500 ally projectiles
        {
            textureParam = new TextureParam("res://Assets/right-arrow.png", new Vector2(1, 1));
            collisionShapes[id] = new CollisionShape(newSize / 2f, 0, 1, 0f); // (int) (CollisionLayers.Player | CollisionLayers.Projectile)
            colors[id] = new Color(0, tint, 0);
            if (allCollisions.Length <= id) Array.Resize(ref allCollisions, id + 500);
        }
        else
        if (team == 1) // 4000 enemies
        {
            textureParam = new TextureParam("res://Assets/right-arrow.png", new Vector2(1, 1));
            collisionShapes[id] = new CollisionShape(newSize / 2f, 1, 0, 0f); // (int) (CollisionLayers.Player | CollisionLayers.Projectile)
            colors[id] = new Color(tint, 0, tint);
            // life
            entityLife[id] = 10;
        }
        // texture
        if (textures.Length <= id) Array.Resize(ref textures, id + 1000);
        textures[id] = textureParam;

        // multimesh spawner
        if (!spawners.TryGetValue(textureParam.Path, out var spawner))
        {
            var texture = GD.Load<Texture2D>(textureParam.Path);
            spawner = new MultimeshSpawner(texture, new Vector2(newSize, newSize), MultimeshSpawnerFlags.Color);
            spawners[textureParam.Path] = spawner;
            SpritesPool.AddChild(spawner.MultiMeshInstance);
        }
        // spawwner
        spawner.AddInstances(1);

        // active entity
        activeEntities.Add(id);
    }

    public void AddParticles(int count, int team, int detectionMask, int collisionLayer, Color color)
    {
        for (int i = 0; i < count; i++)
            AddParticle(team, detectionMask, collisionLayer, color);
    }

    public void RemoveParticles(int count)
    {

    }
}
