using Arch.Core;
using Arch.System;
using Godot;
using Physics.Mains.v5_Arch;
using System.Runtime.CompilerServices;

namespace PhysicsLib.GodotArch;

public partial class MoveSystem(World world) : BaseSystem<World, float>(world)
{
    private static Random rng = new();
    public static int counter;

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Query]
    [All(typeof(Position), typeof(Velocity), typeof(Color))]
    public void Move([Data] in float delta, ref Position position, ref Velocity velocity, ref Color color)
    {
        counter++;
        position.Value += velocity.Value;
        color = new Color(rng.NextSingle(), rng.NextSingle(), rng.NextSingle());
    }
}
