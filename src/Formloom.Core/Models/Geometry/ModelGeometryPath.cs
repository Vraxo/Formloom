using System.Collections.Immutable;
using Formloom.Core.Core;

namespace Formloom.Core.Models.Geometry;

public sealed record ModelGeometryPath(
    string Id,
    string SourceNodeId,
    ImmutableArray<CubicSegment> Segments,
    bool Closed,
    string? MaterialSlot,
    bool Detail)
{
    public float? MaterialTone { get; init; }

    public ImmutableArray<ModelGeometryLandmark> Landmarks { get; init; } = [];
}
