namespace Formloom.Core.Models.Catalog;

public sealed record ModelDefinitionDiagnostic(
    string Code,
    string FilePath,
    string Location,
    string Message);