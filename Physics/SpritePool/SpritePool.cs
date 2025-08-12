using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Physics.Interfaces;

public class SpritePool : ISpritePool
{
	public Node2D PoolNode;
	public int visibleCount = 0;
	public List<Sprite2D> Sprites = new();

	public void AddSprites(int count)
	{
		// Make visible existing sprites
		int toVisible = Sprites.Count - visibleCount;
		toVisible = Math.Min(toVisible, count);
		visibleCount += toVisible;
		count -= toVisible;

		// Add new sprites
		visibleCount += count;
		for (int i = 0; i < count; i++)
		{
			var sprite = new Sprite2D();
			Sprites.Add(sprite);
			PoolNode.AddChild(sprite);
		}
	}

	public void RemoveSprites(int count)
	{
		visibleCount = Math.Max(0, visibleCount - count);
		// Optionally remove sprites from pool
		// for (int i = 0; i < count; i++)
		// {
		// 	if (Sprites.Count > 0)
		// 	{
		// 		var sprite = Sprites.Pop();
		// 		PoolNode.RemoveChild(sprite);
		// 	}
		// }
	}

	public void RemoveSprite(int i)
	{
		visibleCount--;
		var sprite = Sprites.ElementAt(i);
		Sprites.RemoveAt(i);
		Sprites.Add(sprite);
		// if (i < visibleCount && Sprites.Count > 0)
		// {
		// 	var sprite = Sprites.ElementAt(i);
		// 	sprite.QueueFree();
		// 	Sprites = new Stack<Sprite2D>(Sprites.Where((s, index) => index != i).Reverse());
		// 	visibleCount--;
		// }
	}

	public void UpdateSprite(int i, Vector2 position, Vector2 velocity, Color color)
	{
		var sprite = Sprites.ElementAt(i);
		sprite.Position = position;
		sprite.Rotation = velocity.Angle();
		sprite.Modulate = color;
	}

	public Node2D TweenTarget(int i)
	{
		return Sprites.ElementAt(i);
	}

}
