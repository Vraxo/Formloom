using System.Numerics;

namespace Formloom.Core;

public readonly record struct SceneBounds(float Left, float Top, float Right, float Bottom)
{
    public static SceneBounds From(FruitBounds bounds) => new(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);

    public static SceneBounds From(CeramicBounds bounds) => new(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);

    public Vector2 Size => new(Right - Left, Bottom - Top);
}
