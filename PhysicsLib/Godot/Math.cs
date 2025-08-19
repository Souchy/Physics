using Godot;

namespace PhysicsLib.Util;

public static class Rads
{
    public const float Pi15 = Mathf.Pi / 12f;
    public const float Pi30 = Mathf.Pi / 6;
    public const float Pi45 = Mathf.Pi / 4f;
    public const float Pi90 = Mathf.Pi / 2f;
    public const float Pi180 = Mathf.Pi;
    public const float Pi360 = Mathf.Pi * 2f;
}

public static class Vectors
{
    public static int ManhattahnDistanceTo(this Vector2I pos, Vector2I pos2)
    {
        return Math.Abs(pos.X - pos2.X) + Math.Abs(pos.Y - pos2.Y);
    }
}
