using System.Collections.Immutable;
using System.Numerics;

namespace Formloom.Core.Models.Geometry;

public sealed record ModelGeometryPath(
    string Id,
    string SourceNodeId,
    ImmutableArray<CubicSegment> Segments,
    bool Closed,
    string? MaterialSlot,
    bool Detail)
{
    /// <summary>
    /// Gets the optional normalized material tone: 0 is darkest, 0.5 is neutral, and 1 is brightest.
    /// </summary>
    public float? MaterialTone { get; init; }

    /// <summary>Gets the optional stroke width in scene-normalized coordinate units.</summary>
    public float? StrokeWidth { get; init; }

    public ImmutableArray<ModelGeometryLandmark> Landmarks { get; init; } = [];
}
