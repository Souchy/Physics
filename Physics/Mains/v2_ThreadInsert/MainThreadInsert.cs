using Godot;
using Godot.Sharp.Extras;
using Physics.Mains.v1;
using Physics.Utils;
using System;
using System.Collections.Generic;

namespace Physics.Mains.v2;

public class MainThreadInsert(Node mainNode, Vector2 backgroundSize) : Main1(mainNode, backgroundSize), IGameLoop
{

    public Quadtree<int> quadtreeSwap;
    public Quadtree<int> quadtreeThread;

    public override void Start()
    {
        base.Start();
        Scheduler.RunTimed(16, (delta) =>
        {
            var bounds = new Rect2(Vector2.Zero, backgroundSize);
            quadtreeSwap = quadtreeThread;
            quadtreeThread = new(0, bounds);
            List<Particle> copy;
            lock (particles)
            {
                copy = new(particles);
            }
            // Update quadtree(s)
            foreach (var p in copy)
            {
                if (p?.CollisionLayer != 0)
                    quadtreeThread.Insert(p.id, p.Position);
            }
        });
    }

    public override void UpdateQuadtree(double delta)
    {
        if (quadtreeSwap != null)
        {
            // Swap quadtree references
            quadtree = quadtreeSwap;
            quadtreeSwap = null;
        }
    }

}
