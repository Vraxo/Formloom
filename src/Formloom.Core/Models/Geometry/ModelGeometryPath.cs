using System.Collections.Immutable;
using Formloom.Core.Core;

namespace Formloom.Core.Models.Geometry;

public sealed record ModelGeometryPath(
    string Id,
    string SourceNodeId,
    ImmutableArray<CubicSegment> Segments,
    bool Closed,
    string? MaterialSlot,
    bool Detail);
