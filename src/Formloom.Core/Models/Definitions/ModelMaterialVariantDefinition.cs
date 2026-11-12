using System.Collections.Immutable;

namespace Formloom.Core.Models.Definitions;

public sealed record ModelMaterialVariantDefinition(
    ImmutableDictionary<string, string> Tokens)
{
    /// <summary>Gets optional linear gradients keyed by the material slot they fill.</summary>
    public ImmutableDictionary<string, ModelLinearGradientDefinition> Gradients { get; init; } =
        ImmutableDictionary<string, ModelLinearGradientDefinition>.Empty;

    /// <summary>Gets optional translucent gradient overlays keyed by the material slot they shade.</summary>
    public ImmutableDictionary<string, ImmutableArray<ModelLinearGradientDefinition>> GradientOverlays { get; init; } =
        ImmutableDictionary<string, ImmutableArray<ModelLinearGradientDefinition>>.Empty;
}