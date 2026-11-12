using System.Collections.Immutable;

namespace Formloom.Core.Models.Geometry;

public sealed record ModelGeometry(
    string ModelId,
    ModelGeometryCoordinateSpace CoordinateSpace,
    ImmutableArray<ModelGeometryPath> Paths)
{
    /// <summary>
    /// Gets the selected material variant's renderer-neutral color tokens, keyed by material slot.
    /// Values use the model format's #RRGGBB or #RRGGBBAA notation.
    /// </summary>
    public ImmutableDictionary<string, string> MaterialTokens { get; init; } =
        ImmutableDictionary<string, string>.Empty;

    /// <summary>Gets optional linear fill gradients keyed by material slot.</summary>
    public ImmutableDictionary<string, ModelGeometryLinearGradient> MaterialGradients { get; init; } =
        ImmutableDictionary<string, ModelGeometryLinearGradient>.Empty;

    /// <summary>Gets ordered translucent shading gradients keyed by their target material slot.</summary>
    public ImmutableDictionary<string, ImmutableArray<ModelGeometryLinearGradient>> MaterialGradientOverlays { get; init; } =
        ImmutableDictionary<string, ImmutableArray<ModelGeometryLinearGradient>>.Empty;
}
