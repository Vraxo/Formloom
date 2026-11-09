using System.Collections.Immutable;
using System.Numerics;
using Formloom.Core.Core;

namespace Formloom.Core.Roses;

/// <summary>One curved rose petal, including its restrained fold contour.</summary>
public sealed record RosePetal(
    RosePetalLayer Layer,
    Vector2 Root,
    Vector2 Tip,
    float Angle,
    float Tone,
    ImmutableArray<CubicSegment> Outline,
    ImmutableArray<CubicSegment> Fold);
