using Godot;
using System;

namespace Physics.Interfaces;

public class SpritePoolMultimesh : ISpritePool
{
	public MultiMeshInstance2D mm;

    public int VisibleCount {
        get; // => mm.Multimesh.VisibleInstanceCount;
        set; // => mm.Multimesh.VisibleInstanceCount = value;
    }


    public void AddSprites(int count)
	{
		// Manage visible instances
		//int toVisible = mm.Multimesh.InstanceCount - mm.Multimesh.VisibleInstanceCount;
        int toVisible = mm.Multimesh.InstanceCount - VisibleCount;
        toVisible = Math.Min(toVisible, count);
        //mm.Multimesh.VisibleInstanceCount += toVisible;
        VisibleCount += toVisible;
        count -= toVisible;

        mm.Multimesh.InstanceCount += count;
		mm.Multimesh.VisibleInstanceCount += count;
        VisibleCount += count;

    }

	public void RemoveSprites(int count)
	{
        //mm.Multimesh.VisibleInstanceCount = Math.Max(0, mm.Multimesh.VisibleInstanceCount - count);
        VisibleCount = Math.Max(0, VisibleCount - count);

        // make it black
        mm.Multimesh.SetInstanceColor(1, new Color(0, 0, 0, 1));

    }

	public void UpdateSprite(int i, Vector2 position, Vector2 velocity, Color color)
	{
		// Update sprite
		mm.Multimesh.SetInstanceTransform2D(i, new Transform2D(velocity.Angle(), position));
		mm.Multimesh.SetInstanceColor(i, color);
	}

}
