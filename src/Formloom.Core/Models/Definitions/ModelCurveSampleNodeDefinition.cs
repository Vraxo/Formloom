namespace Formloom.Core.Models.Definitions;

public sealed record ModelCurveSampleNodeDefinition(
    string Id,
    string Curve,
    ModelNumberExpression Parameter) : ModelNodeDefinition(Id);
