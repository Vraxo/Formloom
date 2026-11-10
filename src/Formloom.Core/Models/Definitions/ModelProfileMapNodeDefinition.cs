namespace Formloom.Core.Models.Definitions;

public sealed record ModelProfileMapNodeDefinition(
    string Id,
    string Profile,
    ModelPointExpression Origin,
    ModelNumberExpression Angle,
    ModelProfileMappingDefinition Mapping,
    string? MaterialSlot,
    bool Detail) : ModelNodeDefinition(Id);