using Godot;
using Physics.Mains.v1;
using System;

namespace Physics.Mains.v3;

public class MainMultimesh(Node mainNode, Vector2 backgroundSize) : v1.Main1(mainNode, backgroundSize)
{
    public MultiMeshInstance2D mm;

    public override void OnReady()
    {
        base.OnReady();
        mm = new MultiMeshInstance2D()
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
        SpritesPool.AddChild(mm);
    }

    public override void AddParticles(int count, int team, int detectionMask, int collisionLayer, Color color)
    {
        // Manage visible instances
        {
            int toVisible = mm.Multimesh.InstanceCount - mm.Multimesh.VisibleInstanceCount;
            toVisible = Math.Min(toVisible, count);
            mm.Multimesh.VisibleInstanceCount += toVisible;
            count -= toVisible;

            mm.Multimesh.InstanceCount += count;
            mm.Multimesh.VisibleInstanceCount += count;
        }

        base.AddParticles(count, team, detectionMask, collisionLayer, color);
    }

    public override void RemoveParticles(int count)
    {
        mm.Multimesh.VisibleInstanceCount = Math.Max(0, mm.Multimesh.VisibleInstanceCount - count);
        base.RemoveParticles(count);
    }

    public override void UpdateParticleNode(int i, Particle p, double delta)
    {
        // Update position
        p.Position += p.Velocity * (float) delta;
        //// Update sprite
        mm.Multimesh.SetInstanceTransform2D(i, new Transform2D(p.Velocity.Angle(), p.Position));
        mm.Multimesh.SetInstanceColor(i, p.Color);
    }

    public override void AddParticleSprite(Particle p)
    {
        // dont.
    }

    public override void RemoveParticle(int i, Particle p)
    {
        mm.Multimesh.VisibleInstanceCount = Math.Max(0, mm.Multimesh.VisibleInstanceCount - 1);
        base.RemoveParticle(i, p);
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
