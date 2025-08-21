using Godot;
using PhysicsLib.Godot;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Rect2G = Godot.Rect2;
using Vector2G = Godot.Vector2;
//using Vector2N = Godot.Vector2;
using Vector2N = System.Numerics.Vector2;

namespace Physics.Mains.v7_Game;

public static class RectExtensions
{
    public static bool Intersects<T>(this NumericsQuadtree<T> quad, Vector2N point, float radius) where T : struct, IHasPosition
    {
        // Find the closest point to the circle within the rectangle
        Vector2N closestPoint = new(
            Mathf.Clamp(point.X, quad.Bounds.Position.X, quad.PosMax.X),
            Mathf.Clamp(point.Y, quad.Bounds.Position.Y, quad.PosMax.Y)
        );
        // Calculate the distance between the closest point and the circle's center
        float distanceSquared = (closestPoint - point).LengthSquared();
        // Check if the distance is less than or equal to the radius squared
        return distanceSquared <= radius * radius;
    }
}

public interface SizeNode
{
    Rect2G Bounds { get; }
    int Depth { get; }
    bool IsLeaf { get; }
    bool HasChildren { get; }
    public SizeNode[] GetSizeNodes();
}

public interface IHasPosition
{
    Vector2N Position { get; }
}
public class NumericsQuadtree<T> where T : struct, IHasPosition
{
    public const int DATA_CAPACITY = 25; // Maximum number of items per node before splitting
    public const int MAX_DEPTH = 5; // Maximum levels of the NumericsQuadtree

    public int Depth { get; init; } = 0;
    public NumericsQuadtree<T>[] Children { get; protected set; } = [];
    public List<T> Data = new(DATA_CAPACITY); // List of things stored in this node. May use entity references or indexes.

    private Rect2G _bounds;
    public Rect2G Bounds
    {
        get => _bounds;
        private set
        {
            _bounds = value;
            HalfSize = Bounds.Size / 2f;
            QuarterSize = Bounds.Size / 4f;
            Center = Bounds.Position + HalfSize;
            PosMax = Bounds.Position + Bounds.Size;
        }
    }
    public Vector2G Center { get; private set; }
    public Vector2G HalfSize { get; private set; }
    public Vector2G QuarterSize { get; private set; }
    public Vector2G PosMax { get; private set; }

    public bool IsLeaf => Children.Length == 0;
    public bool HasChildren => Children.Length > 0; // Children != null && 

    public NumericsQuadtree()
    {
        Bounds = new Rect2G(Vector2G.Zero, Vector2G.One * 100);
    }
    public NumericsQuadtree(int depth, Rect2G bounds)
    {
        Depth = depth;
        Bounds = bounds;
    }

    public virtual void Split()
    {
        // Split the current node into four subnodes
        int childDepth = Depth + 1;
        Children = [
            // NW
            CreateThis(childDepth, new Rect2G(Bounds.Position, HalfSize)),
            // NE
            CreateThis(childDepth, new Rect2G(Bounds.Position + new Vector2G(HalfSize.X, 0), HalfSize)),
            // SW
            CreateThis(childDepth, new Rect2G(Bounds.Position + new Vector2G(0, HalfSize.Y), HalfSize)),
            // SE
            CreateThis(childDepth, new Rect2G(Center, HalfSize))
        ];
    }

    public virtual NumericsQuadtree<T> CreateThis(int depth, Rect2G bounds)
    {
        return new NumericsQuadtree<T>(depth, Bounds);
    }

    public virtual void Clear()
    {
        Data.Clear();
        Data.Capacity = DATA_CAPACITY;
        // may as well do it ourselves, but the GC would do it on its own..
        foreach (var node in Children)
            node.Clear();
        Children = [];
    }

    public virtual void Insert(T item)
    {
        if (HasChildren)
        {
            int index = GetIndexForPoint(item.Position);
            Children[index].Insert(item);
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
                Insert(dataItem);
            }
            Data.Clear();
            Data.Capacity = DATA_CAPACITY;
        }
    }

    /// <summary>
    /// Inserts into every leaf that intersects with the item.
    /// </summary>
    public virtual void Insert(T item, Vector2N pos, float radius)
    {
        if (HasChildren)
        {
            //int index = GetIndexForPoint(pos);
            //Children[index].Insert(item, pos, radius);
            for (int i = 0; i < Children.Length; i++)
            {
                if (Children[i].Intersects(pos, radius))
                {
                    Children[i].Insert(item, pos, radius);
                }
            }
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
                Reinsert(dataItem, pos);
            }
            Data.Clear();
            Data.Capacity = DATA_CAPACITY;
        }
    }

    protected virtual void Reinsert(T dataItem, Vector2N inserterPos)
    {
        Insert(dataItem);
    }

    /// <summary>
    /// Remove an item from the NumericsQuadtree. If a child node was updated, check if we can merge children back into this node.
    /// </summary>
    /// <param name="item">Item to remove from data</param>
    /// <param name="pos">Item position</param>
    /// <returns>True if this node was updated</returns>
    public virtual bool Remove(T item, Vector2N pos)
    {
        if (HasChildren)
        {
            int index = GetIndexForPoint(pos);
            bool nodeUpdated = Children[index].Remove(item, pos);

            // if a direct child was updated, check if we can merge children
            if (!nodeUpdated) return false;
            int totalItems = Children.Sum(c => c.Data.Count);
            if (totalItems > DATA_CAPACITY) return false;

            // Merge child nodes back into this node
            Data = [];
            foreach (var node in Children)
            {
                Data.AddRange(node.Data);
                node.Clear();
            }
            Children = [];
            return true;
        }
        Data.Remove(item);
        return true;
    }

    public virtual List<NumericsQuadtree<T>> QueryNodes(Rect2G area, List<NumericsQuadtree<T>> nodes)
    {
        if (!Bounds.Intersects(area))
            return nodes;
        if (HasChildren)
        {
            foreach (var child in Children)
            {
                child.QueryNodes(area, nodes);
            }
        }
        else
        if (Data.Count > 0)
        {
            nodes.Add(this);
        }
        return nodes;
    }

    public virtual List<NumericsQuadtree<T>> QueryNodes(Vector2N point, float radius, List<NumericsQuadtree<T>> nodes)
    {
        if (!this.Intersects(point, radius))
            return nodes;
        if (HasChildren)
        {
            foreach (var child in Children)
            {
                child.QueryNodes(point, radius, nodes);
            }
        }
        else
        if (Data.Count > 0)
        {
            nodes.Add(this);
        }
        return nodes;
    }

    public virtual NumericsQuadtree<T> GetNode(Vector2N point)
    {
        if (HasChildren)
        {
            int index = GetIndexForPoint(point);
            return Children[index].GetNode(point);
        }
        else
            return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual int GetIndexForPoint(Vector2N pos)
    {
        int index = 0;
        if (pos.X >= Center.X) index += 1; // Right
        if (pos.Y >= Center.Y) index += 2; // Down
        return index;
    }

}
