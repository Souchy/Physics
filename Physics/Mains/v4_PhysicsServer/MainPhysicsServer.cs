using Godot;
using Physics.Mains.v1;
using System;
using System.Collections.Generic;

namespace Physics.Mains.v4;

public class MainPhysicsServer(Node mainNode, Vector2 backgroundSize) : v3.MainMultimesh(mainNode, backgroundSize)
{

    public Dictionary<int, Area2D> areas = [];
    public Dictionary<Rid, Particle> ParticlesPerShapeId = [];

    public override void OnReady()
    {
        base.OnReady();
        // team 2 collision layer = 0
        areas[0] = new Area2D
        {
            Name = $"Area2D_{2}",
            CollisionMask = 1,
            CollisionLayer = 0,
            Monitorable = false,
            Monitoring = true,
        };
        areas[0].AreaShapeEntered += MainPhysicsServer_AreaShapeEntered;

        // team 1 collision layer = 1
        areas[1] = new Area2D
        {
            Name = $"Area2D_{1}",
            CollisionMask = 0,
            CollisionLayer = 1,
            Monitorable = true,
            Monitoring = false,
        };

        mainNode.AddChild(areas[0]);
        mainNode.AddChild(areas[1]);
    }

    private void MainPhysicsServer_AreaShapeEntered(Rid areaRid, Area2D area, long areaShapeIndex, long localShapeIndex)
    {
        var shape1 = PhysicsServer2D.AreaGetShape(area.GetRid(), (int) localShapeIndex);
        if (ParticlesPerShapeId.TryGetValue(shape1, out Particle p1))
        {
            return;
        }

        var shape2 = PhysicsServer2D.AreaGetShape(areaRid, (int) areaShapeIndex);
        if (!ParticlesPerShapeId.TryGetValue(shape2, out Particle p2))
        {
            return;
        }

        Vector2 deltaPos = p1.Position - p2.Position;
        p1.OnCollision(p1, p2, deltaPos, 0f, true);
        p2.OnCollision(p2, p1, deltaPos, 0f, false);
    }

    public override int AddParticle(int team, int detectionMask, int collisionLayer, Color color)
    {
        var area = areas[collisionLayer];

        int id = base.AddParticle(team, detectionMask, collisionLayer, color);
        var p = particleDict[id];
        var transform = new Transform2D(p.Velocity.Angle(), p.Position);

        var shape = PhysicsServer2D.CircleShapeCreate();
        PhysicsServer2D.ShapeSetData(shape, newSize / 2f);
        PhysicsServer2D.AreaAddShape(area.GetRid(), shape, transform);
        p.Set("shape", shape);
        ParticlesPerShapeId[shape] = p;

        return id;
    }

    public override void UpdateQuadtree(double delta) { }
    public override void UpdatePhysics(double delta)
    {
        lock (particles)
            // Update particle physics + nodes
            foreach (var p in particles)
            {
                if (p.Alive == false) continue;
                // Physics
                //DetectCollisions(p);
                p.CollisionImmunityTime = Math.Max(0, p.CollisionImmunityTime - delta);
                p.Color = new Color(p.Color, (float) (1 - p.CollisionImmunityTime));
                RespectBounds(p, backgroundSize);
            }
    }

    public override void UpdateParticleNode(int i, Particle p, double delta)
    {
        base.UpdateParticleNode(i, p, delta);

        var shape = p.Get<Rid>("shape");
        var area = areas[p.CollisionLayer];
        var transform = new Transform2D(p.Velocity.Angle(), p.Position);
        PhysicsServer2D.AreaAddShape(area.GetRid(), shape, transform);
    }

    public override void RemoveParticle(int i, Particle p)
    {
        base.RemoveParticle(i, p);
        var shape = p.Get<Rid>("shape");
        PhysicsServer2D.FreeRid(shape);
        ParticlesPerShapeId.Remove(shape);
    }

    public override void RemoveParticles(int count)
    {
        base.RemoveParticles(count);
    }

    public override void OnDeath(int i, Particle p)
    {
        // cant tween the multimesh.
        // removing the particle from the list will remove it from the multimesh render.

        // if we want animations,
        //      use customdata + a shader
        //      remove only after the animation is done.
    }

}
