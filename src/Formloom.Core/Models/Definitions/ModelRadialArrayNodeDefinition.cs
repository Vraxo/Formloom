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
    string Stream,
    string? MaterialSlot,
    bool Detail) : ModelNodeDefinition(Id);