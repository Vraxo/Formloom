namespace Formloom.Core.Models.Definitions;

public sealed record ModelProfileMapNodeDefinition(
    string Id,
    string Profile,
    ModelPointExpression Origin,
    ModelNumberExpression Angle,
    ModelProfileMappingDefinition Mapping,
    string? MaterialSlot,
    bool Detail,
    ModelProfileVariationDefinition? Variation = null,
    string? Stream = null,
    ModelNumberExpression? StrokeWidth = null) : ModelNodeDefinition(Id);
