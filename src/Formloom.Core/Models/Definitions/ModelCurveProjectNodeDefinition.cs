namespace Formloom.Core.Models.Definitions;

public sealed record ModelCurveProjectNodeDefinition(
    string Id,
    string Curve,
    ModelPointExpression Target) : ModelNodeDefinition(Id);