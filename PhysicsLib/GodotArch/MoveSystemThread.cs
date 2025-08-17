using Arch.Core;
using Arch.System;
using Godot;
using Physics.Mains.v5_Arch;

namespace PhysicsLib.GodotArch;

public partial class MoveSystemThread(World world) : BaseSystem<World, float>(world)
{
    private static Random rng = new();
    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Query(Parallel = true)]
    [All(typeof(Position), typeof(Velocity), typeof(Color))]
    public static void Movethread([Data] in float delta, ref Position position, ref Velocity velocity, ref Color color)
    {
        position.Value += velocity.Value;
        color = new Color(rng.NextSingle(), rng.NextSingle(), rng.NextSingle());
    }
}