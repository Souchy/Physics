using System;
using Godot;

namespace Physics.Interfaces;

public class SpritePoolMultimesh : ISpritePool
{
	public MultiMeshInstance2D mm;

	public void AddSprites(int count)
	{
		// Manage visible instances
		int toVisible = mm.Multimesh.InstanceCount - mm.Multimesh.VisibleInstanceCount;
		toVisible = Math.Min(toVisible, count);
		mm.Multimesh.VisibleInstanceCount += toVisible;
		count -= toVisible;

		mm.Multimesh.InstanceCount += count;
		mm.Multimesh.VisibleInstanceCount += count;
	}

	public void RemoveSprites(int count)
	{
		mm.Multimesh.VisibleInstanceCount = Math.Max(0, mm.Multimesh.VisibleInstanceCount - count);
	}

	public void UpdateSprite(int i, Vector2 position, Vector2 velocity, Color color)
	{
		// Update sprite
		mm.Multimesh.SetInstanceTransform2D(i, new Transform2D(velocity.Angle(), position));
		mm.Multimesh.SetInstanceColor(i, color);
	}

}
