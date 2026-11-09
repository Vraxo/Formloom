using System.Numerics;

namespace Formloom.Core.Geometry;

public readonly record struct CubicCurveProjection(
    int SegmentIndex,
    float Parameter,
    Vector2 Point,
    Vector2 Tangent,
    float DistanceSquared);
