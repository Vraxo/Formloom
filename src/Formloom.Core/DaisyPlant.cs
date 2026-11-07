using System.Collections.Immutable;
using System.Numerics;

namespace Formloom.Core;

// The daisy as typed geometry rather than a scene object: one stem, exactly two leaves and one
// radial bloom. Bounds is measured from the drawn geometry, so no caller can hand in a
// rectangle that disagrees with the shape that is drawn.
public sealed record DaisyPlant(
    Vector2 StemBase,
    Vector2 StemTop,
    float StemThickness,
    ImmutableArray<CubicSegment> Stem,
    ImmutableArray<DaisyLeaf> Leaves,
    DaisyBloom Bloom)
{
    public SceneBounds Bounds => DaisyGeometry.Measure(this);
}
