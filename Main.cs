using Arch.Core;
using Boids.Util;
using GameLib.util;
using Godot;
using Godot.Sharp.Extras;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static Godot.Projection;


public class Particle
{
    public int id;
    public double CollisionImmunityTime = 0.5;
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public Color Color { get; set; }
    public Node2D? Sprite { get; set; }
    public float Size = 32f;
    public Particle(Vector2 position, Vector2 velocity, Color color)
    {
        Position = position;
        Velocity = velocity;
        Color = color;
    }
}

public partial class Main : Node2D
{
    public static int START_COUNT = 3000;
    public const double CollisionImmunityTimer = 0.1;

    public static Main Instance { get; private set; } = null!;

    public Dictionary<int, Particle> particleDict = new();
    public List<Particle> particles = new(START_COUNT);
    public Quadtree<int> quadtree;

    [NodePath] public Camera2D Camera2D { get; set; }
    [NodePath] public ColorRect Background { get; set; } = null!;
    [NodePath] public Node2D SpritesPool { get; set; } = null!;
    [NodePath] public Node2D QuadtreeLines { get; set; } = null!;

    public override void _Ready()
    {
        this.OnReady();
        Instance = this;

        //var size = Parameters.BoundRadius * 2f * Parameters.TreeToSpaceboundFactor;
        //Background.Size *= 5;
        Camera2D.Position = Background.Size / 2f;

        var size = Background.Size;
        var bounds = new Rect2(Vector2.Zero, size); //new Rect2(-size.X / 2f, -size.Y / 2f, size.X, size.Y);
        quadtree = new Quadtree<int>(0, bounds);

        AddParticles(START_COUNT);
    }

    public void AddParticles(int count)
    {
        var texture = GD.Load<Texture2D>("res://Assets/right-arrow.png");
        for (int i = 0; i < count; i++)
        {
            //Vector2 position = new Vector2(GD.Randf(), GD.Randf()) * Background.Size;
            Vector2 position = Vector2.Zero;
            // set position randomly on a circle around the origin
            float angle = GD.Randf() * Mathf.Tau;
            position.X = Mathf.Cos(angle);
            position.Y = Mathf.Sin(angle);
            position *= 500;

            //Vector2 velocity = new Vector2(GD.Randf(), GD.Randf()).Normalized() * 200f;
            Vector2 velocity = -position.Normalized() * 200f;

            position += Background.Size / 2f; // Offset to center the particles in the background

            Color color = new(GD.Randf(), GD.Randf(), GD.Randf());
            var p = new Particle(position, velocity, color)
            {
                id = i + particles.Count,
                Sprite = new Sprite2D()
                {
                    Texture = texture,
                    Position = position,
                    Modulate = color,
                }
            };
            p.Size = 5f;
            p.Sprite.Scale = Vector2.One * p.Size / 32f;
            particles.Add(p);
            SpritesPool.AddChild(p.Sprite);
            particleDict[p.id] = p;
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
            //quadtree.Remove(p.id, p.Position);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        quadtree.Clear();
        foreach (var p in particles)
        {
            quadtree.Insert(p.id, p.Position);
        }

        foreach (var p in particles)
        {
            // Physics

            DetectCollisions(p);
            p.CollisionImmunityTime = Math.Max(0, p.CollisionImmunityTime - delta);
            p.Color = new Color(p.Color, (float) (1 - p.CollisionImmunityTime));

            RespectBounds(p, Background.Size);

            // Update position
            p.Position += p.Velocity * (float) delta;
            // Update sprite
            p.Sprite.Position = p.Position;
            p.Sprite.Rotation = p.Velocity.Angle();
            p.Sprite.Modulate = p.Color;
        }
    }

    public override void _Process(double delta)
    {
        //QuadtreeLines.RemoveAndQueueFreeChildren();
        //DrawChunks(quadtree, 0, Quadtree<int>.MAX_DEPTH);
    }

    private void DrawChunks<T>(Quadtree<T> chunk, int depth, int totalDepth)
    {
        if (chunk.IsLeaf) return;
        foreach (var child in chunk.Children)
            DrawChunks(child, depth + 1, totalDepth);

        float hue = (float) depth / (float) totalDepth;
        float width = 5f * (1f - hue);
        Color color = Color.FromHsv(hue, 1, 1, (1f - hue));

        var v = new Line2D();
        v.AddPoint(new Vector2(chunk.Center.X, chunk.Center.Y - chunk.HalfSize.Y));
        v.AddPoint(new Vector2(chunk.Center.X, chunk.Center.Y + chunk.HalfSize.Y));
        v.DefaultColor = color;
        v.Width = width;

        var h = new Line2D();
        h.AddPoint(new Vector2(chunk.Center.X - chunk.HalfSize.X, chunk.Center.Y));
        h.AddPoint(new Vector2(chunk.Center.X + chunk.HalfSize.X, chunk.Center.Y));
        h.DefaultColor = color;
        h.Width = width;

        QuadtreeLines.AddChild(v);
        QuadtreeLines.AddChild(h);
        //GD.Print("Draw " + chunk.Center);
    }

    public void DetectCollisions(Particle p1)
    {
        if (p1.CollisionImmunityTime > 0) return; // Skip if immune to collisions
        var nodes = quadtree.QueryNodes(p1.Position, p1.Size * 2, []);
        foreach (var node in nodes)
        {
            foreach (var id in node.Data)
            {
                if (id == p1.id) continue;
                var p2 = particleDict[id];
                if (p2.CollisionImmunityTime > 0) continue; // Skip if immune to collisions
                CheckCollision(p1, p2);
            }
        }
    }

    public static void CheckCollision(Particle p1, Particle p2)
    {
        Vector2 deltaPos = p1.Position - p2.Position;
        float distSquared = deltaPos.LengthSquared();
        float particleRadiusSum = p1.Size;
        if (distSquared < particleRadiusSum * particleRadiusSum)
        {
            // Bounce the velocity off the collision normal
            p1.Velocity = p1.Velocity.Bounce(deltaPos.Normalized());
            // immune to collisions
            p1.CollisionImmunityTime = CollisionImmunityTimer;
        }
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

    public override void _Input(InputEvent @event)
    {
        // This method is called when an input event occurs.
        //if (@event is InputEventKey keyEvent && keyEvent.IsPressed())
        //{

        //}

        if (@event is InputEventMouseButton)
        {
            InputEventMouseButton emb = (InputEventMouseButton) @event;
            if (emb.IsPressed())
            {
                if (emb.ButtonIndex == MouseButton.Right)
                {
                }
                if (emb.ButtonIndex == MouseButton.WheelUp)
                {
                    Camera2D.Zoom *= 1.1f;
                }
                if (emb.ButtonIndex == MouseButton.WheelDown)
                {
                    Camera2D.Zoom /= 1.1f;
                }
            }
        }
    }

}
