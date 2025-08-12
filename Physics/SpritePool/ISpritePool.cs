using Godot;

namespace Physics.Interfaces;

public interface ISpritePool
{

	public void AddSprites(int count);
	public void RemoveSprites(int count);
	public void UpdateSprite(int i, Vector2 position, Vector2 velocity, Color color);

}
