using Arch.Core;
using Arch.Core.Extensions;
using Godot;
using Physics.Utils;

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
                Insert(dataItem, dataItem.Get<Position>().Value); // Fix using the position of reinserted moved entity
            }
            Data.Clear();
            Data.Capacity = DATA_CAPACITY;
        }
    }
}
