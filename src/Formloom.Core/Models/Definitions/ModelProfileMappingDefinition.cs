namespace Formloom.Core.Models.Definitions;

public sealed record ModelProfileMappingDefinition(
    ModelNumberExpression ScaleX,
    ModelNumberExpression ScaleY,
    ModelNumberExpression Length,
    ModelNumberExpression Width,
    ModelNumberExpression RootOffset,
    ModelNumberExpression Bend,
    ModelNumberExpression TransverseNormalization,
    bool ReverseLongitudinal);