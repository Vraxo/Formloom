using System.Numerics;
using Formloom.Core.Ceramics;
using Formloom.Core.Fruits;

namespace Formloom.Core.Core;

public readonly record struct SceneBounds(float Left, float Top, float Right, float Bottom)
{
    public static SceneBounds From(FruitBounds bounds) => new(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);

    public static SceneBounds From(CeramicBounds bounds) => new(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);

    public Vector2 Size => new(Right - Left, Bottom - Top);
}
