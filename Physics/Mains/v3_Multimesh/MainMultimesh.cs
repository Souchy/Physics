using Godot;
using Physics.Mains.v1;
using Physics.Mains.v2;
using Physics.Mains.v3_Multimesh;
using System;

namespace Physics.Mains.v3;

public class MainMultimesh(Node mainNode, Vector2 backgroundSize) : MainThreadInsert(mainNode, backgroundSize)
{
    public MultimeshSpawner spawner;

    public override void OnReady()
    {
        base.OnReady();
        spawner = new(texture, new Vector2(newSize, newSize), MultimeshSpawnerFlags.Color);
        SpritesPool.AddChild(spawner.MultiMeshInstance);
    }

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

    public override void UpdateParticleNode(int i, Particle p, double delta)
    {
        // Update position
        p.Position += p.Velocity * (float) delta;
        //// Update sprite
        spawner.UpdateInstance(i, p.Position, p.Velocity, p.Color);
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

}
