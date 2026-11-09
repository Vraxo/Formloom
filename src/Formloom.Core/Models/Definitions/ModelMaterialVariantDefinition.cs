using System.Collections.Immutable;

namespace Formloom.Core.Models.Definitions;

public sealed record ModelMaterialVariantDefinition(
    ImmutableDictionary<string, string> Tokens);