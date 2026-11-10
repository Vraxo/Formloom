using System.Collections.Immutable;

namespace Formloom.Core.Models.Definitions;

public sealed record ModelCubicPathNodeDefinition(
    string Id,
    ImmutableArray<ModelCubicSegmentDefinition> Segments,
    string? MaterialSlot,
    bool Detail,
    ModelNumberExpression? StrokeWidth = null) : ModelNodeDefinition(Id);
