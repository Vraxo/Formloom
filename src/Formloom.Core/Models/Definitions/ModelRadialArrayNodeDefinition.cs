namespace Formloom.Core.Models.Definitions;

public sealed record ModelRadialArrayNodeDefinition(
    string Id,
    string Profile,
    ModelPointExpression Center,
    ModelNumberExpression RadiusX,
    ModelNumberExpression RadiusY,
    int Count,
    ModelNumberExpression StartAngle,
    ModelNumberExpression Phase,
    ModelProfileMappingDefinition Mapping,
    ModelRadialVariationDefinition Variation,
    ModelNumberExpression? Tone,
    string Stream,
    string? MaterialSlot,
    bool Detail,
    ModelNumberExpression? StrokeWidth = null) : ModelNodeDefinition(Id)