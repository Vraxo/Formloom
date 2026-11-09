using System.Collections.Immutable;
using System.Numerics;

namespace Formloom.Core;

/// <summary>A single layered, cupped rose bloom in normalized scene coordinates.</summary>
public sealed record RoseBloom(
    Vector2 Center,
    float RadiusX,
    float RadiusY,
    ImmutableArray<RosePetal> Petals)
{
    public float Width => RadiusX * 2;

    public float Height => RadiusY * 2;

    public float Top => Center.Y - RadiusY;

    public float Left => Center.X - RadiusX;

    public float Right => Center.X + RadiusX;

    public float Bottom => Center.Y + RadiusY;
}
