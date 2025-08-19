using Godot;

namespace PhysicsLib.Godot;

public interface IHasPosition
{
    Vector2 Position { get; }
}

public class QuadtreeWithPosStruct<T> : Quadtree<T> where T : struct, IHasPosition
{
    public QuadtreeWithPosStruct() : base() { }
    public QuadtreeWithPosStruct(int depth, Rect2 bounds) : base(depth, bounds) { }

    public virtual void Insert(T item) => Insert(item, item.Position);

    protected override void Reinsert(T dataItem, Vector2 inserterPos)
    {
        Insert(dataItem, dataItem.Position);
    }

    public override QuadtreeWithPosStruct<T> CreateThis(int depth, Rect2 bounds)
    {
        return new QuadtreeWithPosStruct<T>(depth, bounds);
    }

}
