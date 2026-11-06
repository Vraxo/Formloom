using System.Collections.Immutable;
using System.Numerics;

namespace Formloom.Core;

// The cupped bloom: where the stem enters, the box the petals were authored in, and the
// petals in back-to-front order. The same bloom serves both petal colours, so colour can
// never change the silhouette.
public sealed record TulipBloom(
    Vector2 Base,
    float Width,
    float Height,
    ImmutableArray<TulipPetal> Petals)
{
    public float Top => Base.Y - Height;

    public float Left => Base.X - (Width / 2);

    public float Right => Base.X + (Width / 2);
}
