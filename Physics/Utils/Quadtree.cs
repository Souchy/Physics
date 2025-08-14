using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Physics.Utils;

public static class RectExtensions
{
    public static bool Intersects<T>(this Quadtree<T> quad, Vector2 point, float radius)
    {
        // Find the closest point to the circle within the rectangle
        Vector2 closestPoint = new(
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
    Rect2 Bounds { get; }
    int Depth { get; }
    bool IsLeaf { get; }
    bool HasChildren { get; }
    public SizeNode[] GetSizeNodes();
}

public class Quadtree<T>
{
    public const int DATA_CAPACITY = 25; // Maximum number of items per node before splitting
    public const int MAX_DEPTH = 5; // Maximum levels of the quadtree

    public int Depth { get; init; } = 0;
    public Quadtree<T>[] Children { get; protected set; } = [];
    public List<T> Data = new(DATA_CAPACITY); // List of things stored in this node. May use entity references or indexes.

    private Rect2 _bounds;
    public Rect2 Bounds
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
    public Vector2 Center { get; private set; }
    public Vector2 HalfSize { get; private set; }
    public Vector2 QuarterSize { get; private set; }
    public Vector2 PosMax { get; private set; }

    public bool IsLeaf => Children.Length == 0;
    public bool HasChildren => Children.Length > 0; // Children != null && 

    public Quadtree()
    {
        Bounds = new Rect2(Vector2.Zero, Vector2.One * 100);
    }
    public Quadtree(int depth, Rect2 bounds)
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
            CreateThis(childDepth, new Rect2(Bounds.Position, HalfSize)),
            // NE
            CreateThis(childDepth, new Rect2(Bounds.Position + new Vector2(HalfSize.X, 0), HalfSize)),
            // SW
            CreateThis(childDepth, new Rect2(Bounds.Position + new Vector2(0, HalfSize.Y), HalfSize)),
            // SE
            CreateThis(childDepth, new Rect2(Center, HalfSize))
        ];
    }

    public virtual Quadtree<T> CreateThis(int depth, Rect2 bounds)
    {
        return new Quadtree<T>(Depth, Bounds);
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

    public virtual void Insert(T item, Vector2 pos)
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
                Insert(dataItem, pos);
            }
            Data.Clear();
            Data.Capacity = DATA_CAPACITY;
        }
    }

    /// <summary>
    /// Inserts into every leaf that intersects with the item.
    /// </summary>
    public virtual void Insert(T item, Vector2 pos, float radius)
    {
        if (HasChildren)
        {
            //int index = GetIndexForPoint(pos);
            //Children[index].Insert(item, pos, radius);
            for(int i = 0; i < Children.Length; i++)
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

    protected virtual void Reinsert(T dataItem, Vector2 inserterPos)
    {
        this.Insert(dataItem, inserterPos);
    }

    /// <summary>
    /// Remove an item from the quadtree. If a child node was updated, check if we can merge children back into this node.
    /// </summary>
    /// <param name="item">Item to remove from data</param>
    /// <param name="pos">Item position</param>
    /// <returns>True if this node was updated</returns>
    public virtual bool Remove(T item, Vector2 pos)
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

    public virtual List<Quadtree<T>> QueryNodes(Rect2 area, List<Quadtree<T>> nodes)
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

    public virtual List<Quadtree<T>> QueryNodes(Vector2 point, float radius, List<Quadtree<T>> nodes)
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

    public virtual Quadtree<T> GetNode(Vector2 point)
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
    public virtual int GetIndexForPoint(Vector2 pos)
    {
        int index = 0;
        if (pos.X >= Center.X) index += 1; // Right
        if (pos.Y >= Center.Y) index += 2; // Down
        return index;
    }

}
