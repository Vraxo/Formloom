namespace Formloom.Core.Models.Evaluation;

public sealed record ModelEvaluationDiagnostic(string Code, string Location, string Message)
{
    public string? ModelId { get; init; }
}
