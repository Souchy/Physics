using Godot;

namespace PhysicsLib.Util;

public enum Orientation4 : byte
{
    Up = 0,
    Down = 1,
    Left = 2,
    Right = 3,
}
public enum Orientation8 : byte
{
    Up = 0,
    Down = 1,
    Left = 2,
    Right = 3,
    UpLeft = 4,
    UpRight = 5,
    DownLeft = 6,
    DownRight = 7,
}
public enum Orientation : byte
{
    Center = 0,
    TopLeft = 1,
    Top = 2,
    TopRight = 4,
    Left = 8,
    Right = 16,
    DownLeft = 32,
    Down = 64,
    DownRight = 128,
}

public static class OrientationExtensions
{
    public static readonly Vector2I[] Orientation4Vectors =
    {
        new(0, -1),
        new(0, 1),
        new(-1, 0),
        new(1, 0),
    };
    public static readonly Vector2I[] Orientation8Vectors =
    {
        new(0, -1),
        new(0, 1),
        new(-1, 0),
        new(1, 0),
        new(-1, -1),
        new(1, -1),
        new(-1, 1),
        new(1, 1),
    };
    public static readonly Dictionary<Orientation, Vector2I> OrientationVectors = new()
    {
        { Orientation.Center   , new(0, 0) },
        { Orientation.Top      , new(0, -1) },
        { Orientation.Down     , new(0, 1) },
        { Orientation.Left     , new(-1, 0) },
        { Orientation.Right    , new(1, 0) },
        { Orientation.TopLeft  , new(-1, -1) },
        { Orientation.TopRight , new(1, -1) },
        { Orientation.DownLeft , new(-1, 1) },
        { Orientation.DownRight, new(1, 1) }
    };
    public static Vector2I GetVector(this Orientation4 orientation) => Orientation4Vectors[(byte) orientation];
    public static Vector2I GetVector(this Orientation8 orientation) => Orientation8Vectors[(byte) orientation];
    public static Vector2I GetVector(this Orientation orientation) => OrientationVectors[orientation];
    public static Orientation GetOpposite(this Orientation orientation)
    {
        return orientation switch
        {
            Orientation.Top => Orientation.Down,
            Orientation.Down => Orientation.Top,
            Orientation.Left => Orientation.Right,
            Orientation.Right => Orientation.Left,
            Orientation.TopLeft => Orientation.DownRight,
            Orientation.TopRight => Orientation.DownLeft,
            Orientation.DownLeft => Orientation.TopRight,
            Orientation.DownRight => Orientation.TopLeft,
            _ => Orientation.Center,
        };
    }
}
