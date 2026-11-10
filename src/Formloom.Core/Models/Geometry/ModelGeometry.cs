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
}
