using System.Collections.Immutable;
using Formloom.Core.Models.Geometry;

namespace Formloom.Core.Models.Evaluation;

public sealed record ModelEvaluationResult(
    ModelGeometry? Geometry,
    ImmutableDictionary<string, double> NumberParameters,
    ImmutableDictionary<string, string> ChoiceParameters,
    ImmutableArray<ModelEvaluationDiagnostic> Diagnostics)
{
    public bool HasErrors => !Diagnostics.IsDefaultOrEmpty;

    public bool IsSuccess => Geometry is not null && Diagnostics.IsDefaultOrEmpty;
}
