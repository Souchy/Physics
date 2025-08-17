using Arch.Core;
using Arch.Core.Extensions;
using Godot;
using PhysicsLib.Godot;

namespace Physics.Mains.v5_Arch;

public class EntityQuadtree : Quadtree<Entity>
{
    public EntityQuadtree() : base() { }
    public EntityQuadtree(int depth, Rect2 bounds) : base(depth, bounds) { }

    public override void Insert(Entity item, Vector2 pos)
    {
        if (HasChildren)
        {
            int index = GetIndexForPoint(pos);
            Children[index].Insert(item, pos);
            return;
        }

        // Insert
        //Data ??= [];
        Data.Add(item);

        // Check if we need to split
        if (Data.Count > DATA_CAPACITY && Depth < MAX_DEPTH)
        {
            Split();
            foreach (var dataItem in Data)
            {
                if (dataItem.IsAlive())
                    this.Insert(dataItem, dataItem.Get<Position>().Value); // Fix using the position of reinserted moved entity
            }
            Data.Clear();
            Data.Capacity = DATA_CAPACITY;
        }
    }

    public override void Split()
    {
        // Split the current node into four subnodes
        int childDepth = Depth + 1;
        Children = [
            // NW
            new EntityQuadtree(childDepth, new Rect2(Bounds.Position, HalfSize)),
            // NE
            new EntityQuadtree(childDepth, new Rect2(Bounds.Position + new Vector2(HalfSize.X, 0), HalfSize)),
            // SW
            new EntityQuadtree(childDepth, new Rect2(Bounds.Position + new Vector2(0, HalfSize.Y), HalfSize)),
            // SE
            new EntityQuadtree(childDepth, new Rect2(Center, HalfSize))
        ];
    }


}
