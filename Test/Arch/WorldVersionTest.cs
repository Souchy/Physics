using Arch.Core;
using Arch.Core.Extensions;
using Souchy.Arch;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Test.Arch;


//public static class WorldVersion
//{
//    public static Dictionary<int, int> Versions = new();
//    public static int GetVersion(this World world)
//    {
//        if (!Versions.TryGetValue(world.Id, out var version))
//        {
//            version = 0;
//            Versions[world.Id] = version;
//        }
//        return version;
//    }
//    public static void RegisterVersion(this World world)
//    {
//        if (!Versions.TryGetValue(world.Id, out int value))
//        {
//            Versions[world.Id] = 0;
//        }
//        else
//        {
//            Versions[world.Id] = ++value;
//        }
//    }
//    public static EntityRef GetRef(this Entity entity)
//    {
//        var world = World.Worlds[entity.WorldId];
//        return new EntityRef(entity, world.GetVersion());
//    }
//}

//public record struct EntityRef(Entity Entity, int WorldVersion) : IEquatable<EntityRef>
//{
//    public readonly bool IsAlive()
//    {
//        var world = World.Worlds[Entity.WorldId];
//        if (world == null) return false;
//        return world.IsAlive(Entity) && world.GetVersion() == WorldVersion;
//    }
//    public readonly bool TryGet<T>(out T component) where T : struct
//    {
//        var world = World.Worlds[Entity.WorldId];
//        if (world == null)
//        {
//            component = default;
//            return false;
//        }
//        if (WorldVersion != world.GetVersion())
//        {
//            component = default;
//            return false;
//        }
//        if (!world.IsAlive(Entity))
//        {
//            component = default;
//            return false;
//        }
//        return world.TryGet(Entity, out component);
//    }
//    public readonly T Get<T>() where T : struct
//    {
//        var world = World.Worlds[Entity.WorldId];
//        if (world == null) throw new NullReferenceException("World is null");
//        if (world.GetVersion() != WorldVersion) throw new InvalidOperationException("Entity is from a different world version");
//        return world.Get<T>(Entity);
//    }
//}


public class WorldVersionTest
{

    private static readonly Position v1 = new(Vector2.One);
    private static readonly Position v2 = new(Vector2.One * 2);

    [Fact]
    public void VersionedWorlds()
    {
        // World 1
        var world1 = World.Create();
        world1.RegisterVersion(); // Version 0
        var entt1 = world1.Create(v1);
        var ref1 = entt1.GetRef();

        world1.Destroy(entt1);
        world1.Dispose();

        var world2 = World.Create();
        world2.RegisterVersion(); // Version 1
        var entt2 = world2.Create(v2);
        var ref2 = entt2.GetRef();

        // Not the same
        Assert.False(ref1.IsAlive());
        Assert.True(ref2.IsAlive());

        // Not the same
        Assert.NotEqual(ref1, ref2);
        Assert.Equal(entt1, entt2);

        // Current world version
        Assert.Equal(1, world1.GetVersion());
        Assert.Equal(1, world2.GetVersion());

        // Entity's world version
        Assert.Equal(0, ref1.WorldVersion);
        Assert.Equal(1, ref2.WorldVersion);

        // Regular Get<T> gives the new entity's value (bad)
        Assert.Equal(entt1.Get<Position>().Value, entt2.Get<Position>().Value);

        // Ref Get<T> gives gives an error (good)
        Assert.Throws<InvalidOperationException>(() => ref1.Get<Position>().Value);
    }

}
