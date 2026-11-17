using Microsoft.Xna.Framework;
using System;

namespace RpgProject.World;

public enum Direction
{
    North,
    South,
    West,
    East,
}

public static class DirectionExtensions
{
    public static Point ToPoint(this Direction direction) => direction switch
    {
        Direction.North => new Point(0, -1),
        Direction.South => new Point(0, 1),
        Direction.West => new Point(-1, 0),
        Direction.East => new Point(1, 0),
        _ => throw new ArgumentOutOfRangeException(nameof(direction)),
    };

    public static Vector2 ToVector(this Direction direction) => direction.ToPoint().ToVector2();

    public static Direction FromPoint(int x, int y) => (x, y) switch
    {
        (0, -1) => Direction.North,
        (0, 1) => Direction.South,
        (-1, 0) => Direction.West,
        (1, 0) => Direction.East,
        _ => throw new ArgumentException("Invalid direction"),
    };

    public static Direction FromVector(Vector2 vector) => FromPoint((int)vector.X, (int)vector.Y);
}
