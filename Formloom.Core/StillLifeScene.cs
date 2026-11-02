using System.Numerics;

namespace Formloom.Core;

public sealed record StillLifeScene(
    int Width,
    int Height,
    ulong Seed,
    double Variation,
    AppleDescription Apple,
    double TableHorizon,
    double GrainStrength,
    double BackgroundWarmth,
    Vector2 LightDirection);
