using System.Collections.Immutable;
using Formloom.Core.Models.Definitions;

namespace Formloom.Core.Models.Catalog;

public sealed record ModelDefinitionCatalogResult(
    ImmutableDictionary<string, ModelDefinition> Models,
    ImmutableArray<ModelDefinitionDiagnostic> Diagnostics)
{
    public bool HasErrors => !Diagnostics.IsDefaultOrEmpty;

    public bool IsSuccess => Diagnostics.IsDefaultOrEmpty;
}