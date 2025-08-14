using Godot;

namespace Physics.Utils;

public interface IHasPosition
{
    Vector2 Position { get; }
}

public class QuadtreeWithPosStruct<T> : Quadtree<T> where T : struct, IHasPosition
{
    public QuadtreeWithPosStruct() : base() { }
    public QuadtreeWithPosStruct(int depth, Rect2 bounds) : base(depth, bounds) { }

    protected override void Reinsert(T dataItem, Vector2 inserterPos)
    {
        this.Insert(dataItem, dataItem.Position);
    }

    public override Quadtree<T> CreateThis(int depth, Rect2 bounds)
    {
        return new QuadtreeWithPosStruct<T>(depth, bounds);
    }

}
