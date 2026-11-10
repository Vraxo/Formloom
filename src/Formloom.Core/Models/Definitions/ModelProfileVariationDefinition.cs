namespace Formloom.Core.Models.Definitions;

public sealed record ModelProfileVariationDefinition(
    ModelNumberExpression Angle,
    ModelNumberExpression Length,
    ModelNumberExpression Width,
    ModelNumberExpression Bend);
