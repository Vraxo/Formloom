using System.Collections.Immutable;
using System.Numerics;

namespace Formloom.Core.Models.Geometry;

/// <summary>Describes a linear fill gradient in scene-normalized coordinates.</summary>
public sealed record ModelGeometryLinearGradient(
    Vector2 Start,
    Vector2 End,
    ImmutableArray<ModelGeometryGradientStop> Stops);
