using System.Numerics;

namespace Formloom.Core;

public sealed record AppleDescription(
    AppleColor Color,
    Vector2 Center,
    Vector2 Size,
    double Asymmetry,
    double StemLean,
    double LeafAngle,
    double SurfaceVariation);
