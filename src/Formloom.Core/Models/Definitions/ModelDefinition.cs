using System.Collections.Immutable;

namespace Formloom.Core.Models.Definitions;

public sealed record ModelDefinition(
    string Format,
    int SchemaVersion,
    string Id,
    string Name,
    string Category,
    ModelPromptDefinition Prompt,
    ImmutableArray<string> Mounts,
    ImmutableDictionary<string, ModelParameterDefinition> Parameters,
    ImmutableDictionary<string, ModelProfileDefinition> Profiles,
    ImmutableDictionary<string, ModelMaterialVariantDefinition> Materials,
    ImmutableArray<ModelNodeDefinition> Nodes,
    ImmutableArray<string> Outputs);