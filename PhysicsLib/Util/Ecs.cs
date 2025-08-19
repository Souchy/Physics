using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhysicsLib.Util;

public record struct EcsEntity(int Id, int WorldId);
public class Ecs
{
}

public class SparseArray<TComponent>
{
    private readonly List<TComponent> components;
    private readonly Dictionary<int, int> entityToIndex;
    private readonly List<int> entities; // Parallel to components

    public SparseArray(int initialCapacity = 10)
    {
        components = new(initialCapacity);
        entityToIndex = new(initialCapacity);
        entities = new(initialCapacity);
    }

    public void Add(int entity, TComponent component)
    {
        int idx = components.Count;
        entityToIndex[entity] = idx;
        components.Add(component);
        entities.Add(entity);
    }

    public bool TryGet(int entity, out TComponent component)
    {
        if (entityToIndex.TryGetValue(entity, out int idx))
        {
            component = components[idx];
            return true;
        }
        component = default;
        return false;
    }

    public void Remove(int entity)
    {
        if (!entityToIndex.TryGetValue(entity, out int idx))
            return;
        int lastIdx = components.Count - 1;
        if (idx != lastIdx)
        {
            // Move last to removed slot
            components[idx] = components[lastIdx];
            entities[idx] = entities[lastIdx];
            entityToIndex[entities[idx]] = idx;
        }
        // Remove last
        components.RemoveAt(lastIdx);
        entities.RemoveAt(lastIdx);
        entityToIndex.Remove(entity);
    }

    //public IEnumerable<TComponent> Iterate()
    //{
    //    for (int i = 0; i < components.Count; i++)
    //    {
    //        yield return (components[i]);
    //    }
    //}
    public IEnumerable<(int entity, TComponent component)> Iterate()
    {
        for (int i = 0; i < components.Count; i++)
        {
            yield return (entities[i], components[i]);
        }
    }
}