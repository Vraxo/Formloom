namespace Formloom.Core.Models.Definitions;

public sealed record ModelCubicSegmentDefinition(
    ModelPointExpression Start,
    ModelPointExpression Control1,
    ModelPointExpression Control2,
    ModelPointExpression End);