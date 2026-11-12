using System.Collections.Immutable;
using System.Numerics;

namespace Formloom.Core.Models.Geometry;

/// <summary>Describes a linear fill gradient with scene-normalized fallback endpoints and optional path landmarks.</summary>
public sealed record ModelGeometryLinearGradient(
    Vector2 Start,
    Vector2 End,
    ImmutableArray<ModelGeometryGradientStop> Stops)
{
    /// <summary>Gets the optional per-path start landmark.</summary>
    public string? StartLandmark { get; init; }

    /// <summary>Gets the optional per-path end landmark.</summary>
    public string? EndLandmark { get; init; }
}
