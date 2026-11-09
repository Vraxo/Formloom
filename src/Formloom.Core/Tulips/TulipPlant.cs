using System.Collections.Immutable;
using System.Numerics;
using Formloom.Core.Core;

namespace Formloom.Core.Tulips;

// The plant as typed geometry rather than a scene object: one stem, exactly two leaves and
// one bloom, all in scene-normalized space. Bounds is measured from the geometry itself, so
// no caller can hand in a rectangle that disagrees with the shape that is drawn.
public sealed record TulipPlant(
    Vector2 StemBase,
    Vector2 StemTop,
    float StemThickness,
    ImmutableArray<CubicSegment> Stem,
    ImmutableArray<TulipLeaf> Leaves,
    TulipBloom Bloom)
{
    public SceneBounds Bounds => TulipGeometry.Measure(this);
}
