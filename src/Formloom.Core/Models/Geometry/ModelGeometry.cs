using System.Collections.Immutable;

namespace Formloom.Core.Models.Geometry;

public sealed record ModelGeometry(
    string ModelId,
    ModelGeometryCoordinateSpace CoordinateSpace,
    ImmutableArray<ModelGeometryPath> Paths);
