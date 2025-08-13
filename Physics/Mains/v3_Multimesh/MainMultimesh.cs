using Godot;
using Physics.Mains.v1;
using Physics.Mains.v2;
using System;

namespace Physics.Mains.v3;

public class MainMultimesh(Node mainNode, Vector2 backgroundSize) : MainThreadInsert(mainNode, backgroundSize)
{
    public MultiMeshInstance2D MultiMeshInstance;
    private MultiMesh Multimesh;

    public override void OnReady()
    {
        base.OnReady();
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
    }

    public override void AddParticles(int count, int team, int detectionMask, int collisionLayer, Color color)
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

        base.AddParticles(count, team, detectionMask, collisionLayer, color);
    }

    public override void RemoveParticles(int count)
    {
        Multimesh.VisibleInstanceCount = Math.Max(0, Multimesh.VisibleInstanceCount - count);
        base.RemoveParticles(count);
    }

    public override void UpdateParticleNode(int i, Particle p, double delta)
    {
        // Update position
        p.Position += p.Velocity * (float) delta;
        //// Update sprite
        Multimesh.SetInstanceTransform2D(i, new Transform2D(p.Velocity.Angle(), p.Position));
        Multimesh.SetInstanceColor(i, p.Color);
    }

    public override void AddParticleSprite(Particle p)
    {
        // dont.
    }

    public override void RemoveParticle(int i, Particle p)
    {
        Multimesh.VisibleInstanceCount = Math.Max(0, Multimesh.VisibleInstanceCount - 1);
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
