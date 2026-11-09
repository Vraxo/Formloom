using System.Collections.Immutable;
using System.Numerics;
using Formloom.Core.Core;

namespace Formloom.Core.Roses;

/// <summary>A rose's single continuous stem, two leaves and one fixed layered bloom.</summary>
public sealed record RosePlant(
    Vector2 StemBase,
    Vector2 StemTop,
    float StemThickness,
    ImmutableArray<CubicSegment> Stem,
    ImmutableArray<RoseLeaf> Leaves,
    RoseBloom Bloom)
{
    public SceneBounds Bounds => RoseGeometry.Measure(this);
}
