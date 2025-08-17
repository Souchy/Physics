using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using System.Drawing;

//using Godot;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Test.Arch;

public class ArchSystemsTest
{

    [Fact]
    public void Arch_Query_ShouldWork()
    {
        // Arrange
        var world = World.Create();
        var entt = world.Create(
            new Position(Vector2.Zero),
            new Velocity(Vector2.One)
        );
        // Act
        world.Query(in new QueryDescription().WithAll<Position, Velocity>(), (Entity entity, ref Position pos, ref Velocity vel) =>
        {
            pos.Value += vel.Value;
        });
        // Assert
        Assert.Equal(Vector2.One, entt.Get<Position>().Value);
    }

    [Fact]
    public void Arch_LowLevel_ShouldWork()
    {
        // Arrange
        var world = World.Create();
        var entt = world.Create(
            new Position(Vector2.Zero),
            new Velocity(Vector2.One)
        );
        // Act
        var query = world.Query(in new QueryDescription().WithAll<Position, Velocity>());
        foreach (ref var chunk in query.GetChunkIterator())
        {
            var references = chunk.GetFirst<Position, Velocity>();
            foreach (var i in chunk)
            {
                ref var position = ref Unsafe.Add(ref references.t0, i);
                ref var collisionLayer = ref Unsafe.Add(ref references.t1, i);
                position.Value += collisionLayer.Value;
            }
        }
        // Assert
        Assert.Equal(Vector2.One, entt.Get<Position>().Value);
    }


    [Fact]
    public void Arch_System_ShouldWork()
    {
        // Arrange
        var world = World.Create();
        var moveSystem = new MoveSystem(world);
        var group = new Group<float>(
            "Moving",
            moveSystem
        );
        group.Initialize();

        var entt = world.Create(
            new Position(Vector2.Zero),
            new Velocity(Vector2.One)
        );
        // Act
        float delta = 1f / 60f;
        {
            group.BeforeUpdate(in delta);
            group.Update(in delta);
            group.AfterUpdate(in delta);
        }
        // Assert
        Assert.Equal(Vector2.One, entt.Get<Position>().Value);
        Assert.Equal(1, moveSystem.counter);
    }

}

public partial class MoveSystem(World world) : BaseSystem<World, float>(world)
{
    public int counter;
    [Query]
    [All(typeof(Position), typeof(Velocity))]
    public void Move(ref Position position, ref Velocity velocity)
    {
        counter++;
        position.Value += velocity.Value;
    }
}