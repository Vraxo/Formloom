namespace Formloom.Core.Models.Definitions;

public sealed record ModelRadialVariationDefinition(
    ModelNumberExpression Angle,
    ModelNumberExpression Length,
    ModelNumberExpression Width,
    ModelNumberExpression Bend,
    ModelNumberExpression Tone);