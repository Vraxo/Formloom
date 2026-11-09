using System.Collections.Immutable;

namespace Formloom.Core.Models.Definitions;

public sealed record ModelPromptDefinition(
    string Article,
    string Noun,
    ImmutableArray<string> Aliases,
    string MaterialVariantParameter,
    ImmutableDictionary<string, ImmutableArray<string>> Descriptors);