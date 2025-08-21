using Arch.Core;
using Godot;
using Physics.Mains.v3_Multimesh;
using Physics.Mains.v5_Arch;
using PhysicsLib.Godot;
using PhysicsLib.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Vector2G = Godot.Vector2;
//using Vector2N = Godot.Vector2;
using Vector2N = System.Numerics.Vector2;


namespace Physics.Mains.v7_Game;

public static class NumericsExtensions
{
    public static System.Numerics.Vector2 Bounce(this System.Numerics.Vector2 vector, System.Numerics.Vector2 normal)
    {
        return vector - 2 * System.Numerics.Vector2.Dot(vector, normal) * normal;
    }
    public static System.Numerics.Vector2 Normalized(this System.Numerics.Vector2 vector)
    {
        float length = vector.Length();
        if (length == 0) return System.Numerics.Vector2.Zero;
        return vector / length;
    }
}


public record struct PositionId(int Id, Vector2N Position) : IHasPosition;
public record struct Spatial(Vector2N Position, Vector2N Velocity);
//public record struct Life(int Current, int Max);
public record struct CollisionShape(float Radius, int CollisionLayer, int CollisionMask, float CollisionImmunityTimer);
public record struct TextureParam(string Path);
public record struct ShaderSpriteAnimParam(string shader, Vector2G StepsCount, Vector2G StepAnimStart, Vector2G StepAnimSize, float AnimationDuration = 1f);
public record struct Collision(int Id1, int Id2, Vector2N Normal);

public record struct AnimationTimer(float CurrentTime, float Duration);

public record struct ValueAnimation<T>(T Current, T Target, float Duration, float Elapsed, bool pingPong)
{
    public readonly bool IsFinished => Elapsed >= Duration;
    public ValueAnimation<T> Update(float delta)
    {
        if (IsFinished)
        {
            if (pingPong)
            {
                // Swap Current and Target for ping-pong effect
                var temp = Current;
                Current = Target;
                Target = temp;
                Elapsed = 0f; // Reset Elapsed for the next cycle
            }
            else
            {
                Current = Target;
                Elapsed = Duration; // Ensure Elapsed is set to Duration when finished
            }
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
        else if (current is Vector2N vectorCurrent && target is Vector2N vectorTarget)
        {
            return (T) (object) new Vector2N(
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

public class MainGame(Node mainNode, Vector2G backgroundSize) : IGameLoop
{
    #region Nodes
    public Node2D SpritesPool { get; set; } = null!;
    #endregion

    public const float newSize = 32;
    public const float spriteSize = 32;
    public const int CAPACITY = 10_000;
    public const bool threadedInsert = true;
    public const float HurtAnimationDuration = 0.2f;
    public int MAX_LIFE = 10;
    public const string LIFEBAR_NAME = "lifebar";
    public const bool CAN_DIE = false;

    public MultimeshSpawner LifebarSpawner
    {
        get => spawners[LIFEBAR_NAME];
        set => spawners[LIFEBAR_NAME] = value;
    }

    public int ParticleCount => activeEntities.Count;


    public IntId ids = new();
    public List<int> activeEntities;

    public Spatial[] spatials;
    public bool[] active;
    public CollisionShape[] collisionShapes;
    public TextureParam[] textures;
    public Color[] colors;
    public Dictionary<int, int> entityLife;
    public Dictionary<int, float> entityHurtAnimationTime;
    public Dictionary<int, AnimationTimer> entitySpriteAnimationTime;
    public Collision[] allCollisions;


    public Dictionary<string, MultimeshSpawner> spawners;
    public Dictionary<int, NumericsQuadtree<PositionId>> quadtrees;
    public Dictionary<int, NumericsQuadtree<PositionId>> quadtreesSwap;
    public bool quadtreeReady = false;

    public void OnReady()
    {
        activeEntities = new(CAPACITY);
        spatials = new Spatial[CAPACITY];
        collisionShapes = new CollisionShape[CAPACITY];
        textures = new TextureParam[CAPACITY];
        colors = new Color[CAPACITY];
        entityLife = new(CAPACITY);
        entitySpriteAnimationTime = new(CAPACITY);
        entityHurtAnimationTime = new(CAPACITY);
        allCollisions = new Collision[1000];

        quadtrees = new(10)
        {
            [(int) CollisionLayers.Player] = new(0, new Rect2(Vector2G.Zero, backgroundSize)),
            [(int) CollisionLayers.Enemy] = new(0, new Rect2(Vector2G.Zero, backgroundSize)),
            [(int) (CollisionLayers.Player | CollisionLayers.Projectile)] = new(0, new Rect2(Vector2G.Zero, backgroundSize)),
            [(int) (CollisionLayers.Enemy | CollisionLayers.Projectile)] = new(0, new Rect2(Vector2G.Zero, backgroundSize)),
        };
        quadtreesSwap = new(10)
        {
            [(int) CollisionLayers.Player] = new(0, new Rect2(Vector2G.Zero, backgroundSize)),
            [(int) CollisionLayers.Enemy] = new(0, new Rect2(Vector2G.Zero, backgroundSize)),
            [(int) (CollisionLayers.Player | CollisionLayers.Projectile)] = new(0, new Rect2(Vector2G.Zero, backgroundSize)),
            [(int) (CollisionLayers.Enemy | CollisionLayers.Projectile)] = new(0, new Rect2(Vector2G.Zero, backgroundSize)),
        };

        spawners = new(10);
        SpritesPool = mainNode.GetNode<Node2D>("%SpritesPool");
    }

    public void Start()
    {

        if (threadedInsert)
            Scheduler.RunTimed(16, (delta) =>
            {
                if (quadtreeReady)
                    return;

                foreach (var quadtree in quadtreesSwap.Values)
                    quadtree.Clear();
                //foreach (var key in quadtreesSwap.Keys)
                //{
                //    quadtreesSwap[key] = new(0, new Rect2(Vector2.Zero, backgroundSize));
                //}

                List<int> copy = null;
                lock (activeEntities)
                {
                    copy = [.. activeEntities];
                }
                if (copy == null) return;

                // Update quadtree(s)
                foreach (var p in copy)
                {
                    int layer = collisionShapes[p].CollisionLayer;
                    if (layer == 0)
                        continue;
                    if (entityLife.TryGetValue(p, out var lifeValue) && lifeValue <= 0)
                        continue;
                    var spatial = spatials[p];
                    var positionid = new PositionId(p, spatial.Position);
                    quadtreesSwap[layer].Insert(positionid);
                }

                quadtreeReady = true;
            });
    }



    public void PhysicsProcess(double delta)
    {
        //allCollisions.Clear();
        //allCollisions = new Collision[ParticleCount];
        allCollisions = new Collision[allCollisions.Length];
        UpdateQuadtree();
        UpdatePhysics(delta);
        UpdateNodes(delta);
        UpdateCollisions(delta);
    }

    public void UpdateQuadtree()
    {
        if (threadedInsert)
        {
            if (!quadtreeReady)
                return;

            // Swap quadtrees
            (quadtreesSwap, quadtrees) = (quadtrees, quadtreesSwap);
            quadtreeReady = false;
        }
        else
        {
            foreach (var quadtree in quadtrees.Values)
                quadtree.Clear();
            foreach (int id in activeEntities)
            {
                var shape = collisionShapes[id];
                if (shape.CollisionLayer == 0)
                    continue;
                var spatial = spatials[id];
                var positionid = new PositionId(id, spatial.Position);
                quadtrees[shape.CollisionLayer].Insert(positionid);
            }
        }
    }

    public void UpdatePhysics(double delta)
    {
        Parallel.For(0, activeEntities.Count, i =>
        //for(int i = 0; i < activeEntities.Count; i++)
        {
            int id1 = activeEntities[i];

            ref var spatial1 = ref spatials[id1];

            RespectBounds(ref spatial1, backgroundSize);

            spatial1.Position += spatial1.Velocity * (float) delta;

            // Update sprite animation
            if (entitySpriteAnimationTime.TryGetValue(id1, out var animTimer))
            {
                animTimer.CurrentTime += (float) delta;
                if (animTimer.CurrentTime > animTimer.Duration)
                    animTimer.CurrentTime = 0f;
                entitySpriteAnimationTime[id1] = animTimer;
            }

            // Update hurt animation
            if (entityHurtAnimationTime.TryGetValue(id1, out var hurtTime) && hurtTime != 0)
            {
                hurtTime -= (float) delta;
                if (hurtTime <= 0f)
                    hurtTime = 0f;
                entityHurtAnimationTime[id1] = hurtTime;
                float elapsed = HurtAnimationDuration - hurtTime;
                float progress = elapsed / HurtAnimationDuration;
                colors[id1] = new Color(colors[id1], progress); // Fade out hurt animation
            }

            // Update collision immunity
            ref var shape1 = ref collisionShapes[id1];
            if (shape1.CollisionImmunityTimer > 0f)
            {
                shape1.CollisionImmunityTimer -= (float) delta;
                if (shape1.CollisionImmunityTimer < 0f)
                    shape1.CollisionImmunityTimer = 0f;
                return;
            }

            if (shape1.CollisionMask == 0)
                return;

            // Check collisions
            var quadtree = quadtrees[shape1.CollisionMask];
            var results = quadtree.QueryNodes(spatial1.Position, shape1.Radius, []);
            foreach (var node in results)
            {
                foreach (var p2 in node.Data)
                {
                    if (p2.Id == id1)
                        continue;
                    int id2 = p2.Id;

                    ref var shape2 = ref collisionShapes[id2];
                    if (shape2.CollisionImmunityTimer > 0f)
                        continue;
                    var spatial2 = spatials[id2];
                    var deltaPos = spatial1.Position - spatial2.Position;
                    var distSquared = deltaPos.LengthSquared();
                    float maxDist = shape1.Radius + shape2.Radius;
                    if (distSquared < maxDist * maxDist)
                    {
                        Vector2N normal = Vector2N.Zero;
                        if (distSquared != 0)
                        {
                            float length = MathF.Sqrt(distSquared);
                            normal = deltaPos / length; // deltaPos.Normalized();
                        }
                        allCollisions[id1] = new Collision(id1, id2, normal);
                        shape1.CollisionImmunityTimer = 0.1f;
                        shape2.CollisionImmunityTimer = 0.1f;
                        //break;
                        return;
                    }
                }
            }
        }
        );
    }

    public virtual void RespectBounds(ref Spatial spatial, Vector2G Bounds)
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

        spatial.Velocity = new Vector2N(vx, vy);
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

            //Damage
            if (entityLife.TryGetValue(Id1, out var lifeValue1))
            {
                lifeValue1--;
                if (lifeValue1 < 1) lifeValue1 = 1;
                entityLife[Id1] = lifeValue1;
                entityHurtAnimationTime[Id1] = HurtAnimationDuration;
            }
            if (entityLife.TryGetValue(Id2, out var lifeValue2))
            {
                lifeValue2--;
                if (lifeValue2 < 1) lifeValue2 = 1;
                entityLife[Id2] = lifeValue2;
                entityHurtAnimationTime[Id2] = HurtAnimationDuration;
            }
        }
    }

    public void UpdateNodes(double delta)
    {
        //foreach (var spawner in spawners.Values)
        //{
        //    spawner.CurrentInstance = 0;
        //}

        for (int i = ParticleCount - 1; i >= 0; i--)
        {
            int id = activeEntities[i];
            var textureParams = textures[id];
            bool hasSpawner = spawners.TryGetValue(textureParams.Path, out var spawner);
            bool hasLife = entityLife.TryGetValue(id, out int life);

            // if (false) // if entity should be cleared
            // {
            //     entitySpriteAnimationTime.Remove(id);
            // }
            if (hasLife && life <= 0 && CAN_DIE)
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
                LifebarSpawner.RemoveInstance();
                continue;
            }
            else
            {
                if (hasSpawner)
                {
                    var spatial = spatials[id];
                    var color = colors[id];
                    entitySpriteAnimationTime.TryGetValue(id, out var animationTimer);
                    entityHurtAnimationTime.TryGetValue(id, out float hurtAnimTime);

                    spawner.UpdateInstance(spatial.Position, spatial.Velocity, color, new(animationTimer.CurrentTime, 0, 0, 0));  //spriteAnimTime, textureParams.AnimationDuration, hurtAnimTime, HurtAnimationDuration));

                    if (hasLife)
                    {
                        if (life == MAX_LIFE)
                        {
                            LifebarSpawner.UpdateInstance(Vector2N.Zero, Vector2N.Zero, Transparent, Transparent);
                        }
                        else
                        if (life < MAX_LIFE)
                        {

                            float percent = (float) life / MAX_LIFE;
                            float oldPercent = percent;
                            float entityRadius = collisionShapes[id].Radius;
                            var lifePos = spatial.Position - new Vector2N(0, entityRadius + LifebarOffset);
                            LifebarSpawner.UpdateInstance(lifePos, Vector2N.Zero, White, new(percent, oldPercent, 0, 0));
                        }
                    }
                }
            }
        }
        foreach (var spawner in spawners.Values)
        {
            spawner.SendToGodot();
        }
    }
    public static readonly Color White = new(1, 1, 1, 1);
    public static readonly Color Transparent = new(1, 1, 1, 0);
    public const float LifebarOffset = 5f;

    public void AddParticle(int team, int detectionMask, int collisionLayer, Color color)
    {
        Vector2N position = Vector2N.Zero;
        // set position randomly on a circle around the origin
        float angle = GD.Randf() * Mathf.Tau;
        position.X = Mathf.Cos(angle);
        position.Y = Mathf.Sin(angle);
        position *= 500;
        Vector2N velocity = -position.Normalized() * 200f;
        //position += backgroundSize / 2f;
        position = new Vector2N(position.X + backgroundSize.X / 2f, position.Y + backgroundSize.Y / 2f);


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
        ShaderSpriteAnimParam? animParam = null;
        float tint = 0.7f;
        if (team == 2) // 500 ally projectiles
        {
            // 16x16 per sprite, 640x400 total = 40x25 steps
            textureParam = new TextureParam("res://Assets/All_Fire_Bullet_Pixel_16x16_05.png");
            animParam = new ShaderSpriteAnimParam("res://Assets/spritesheetAnimatedMultimeshShader.tres", new(40, 25), new(6, 10), new(4, 1), 0.4f);
            entitySpriteAnimationTime[id] = new AnimationTimer(0f, animParam.Value.AnimationDuration);

            colors[id] = new(1, 1, 1, 1); //new Color(0, tint, 0);
            collisionShapes[id] = new CollisionShape(newSize / 2f, 0, 1, 0f); // (int) (CollisionLayers.Player | CollisionLayers.Projectile)
            if (allCollisions.Length <= id) Array.Resize(ref allCollisions, id + 500);
        }
        else
        if (team == 1) // 4000 enemies
        {
            textureParam = new TextureParam("res://Assets/right-arrow.png");
            collisionShapes[id] = new CollisionShape(newSize / 2f, 1, 0, 0f); // (int) (CollisionLayers.Player | CollisionLayers.Projectile)
            colors[id] = new Color(0, tint, tint, 1);
            //colors[id] = new(1, 1, 1, 1);
            // life
            entityLife[id] = MAX_LIFE - 1;

            // lifebar multimesh
            if (!spawners.TryGetValue(LIFEBAR_NAME, out _))
            {
                var lifebarTexture = GD.Load<Texture2D>("res://Assets/lifebarTexture.tres");
                var lifebarTexture2 = GD.Load<Texture2D>("res://Assets/lifebarTexture2.tres");

                LifebarSpawner = new MultimeshSpawner(lifebarTexture, new Vector2G(32, 8), MultimeshSpawnerFlags.All);

                var mat = new ShaderMaterial()
                {
                    Shader = GD.Load<Shader>("res://Assets/progressbarMultimeshShader.tres"),
                };
                mat.SetShaderParameter("LossTexture", lifebarTexture2);
                LifebarSpawner.MultiMeshInstance.Material = mat;
                SpritesPool.AddChild(LifebarSpawner.MultiMeshInstance);
            }
            LifebarSpawner.AddInstances(1);
        }
        // texture
        if (textures.Length <= id) Array.Resize(ref textures, id + 1000);
        textures[id] = textureParam;

        // multimesh spawner
        if (!spawners.TryGetValue(textureParam.Path, out var spawner))
        {
            var texture = GD.Load<Texture2D>(textureParam.Path);
            spawner = new MultimeshSpawner(texture, new Vector2G(newSize, newSize), MultimeshSpawnerFlags.All);
            spawners[textureParam.Path] = spawner;

            if (animParam != null)
            {
                var mat = new ShaderMaterial()
                {
                    Shader = GD.Load<Shader>(animParam.Value.shader),
                };
                mat.SetShaderParameter("Steps", animParam.Value.StepsCount);
                mat.SetShaderParameter("StepsAnimationStart", animParam.Value.StepAnimStart);
                mat.SetShaderParameter("StepsAnimationRange", animParam.Value.StepAnimSize);
                mat.SetShaderParameter("AnimationDuration", animParam.Value.AnimationDuration);
                spawner.MultiMeshInstance.Material = mat;
            }
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
