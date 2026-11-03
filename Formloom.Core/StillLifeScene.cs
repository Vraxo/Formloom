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
    Vector2 LightDirection)
{
    public double SurfaceDetail { get; init; } = 1;

    public FruitKind Subject { get; init; } = FruitKind.Apple;

    // Apple remains the legacy body-parameter contract for existing callers and tests.
    public AppleDescription Body => Apple;

    public FruitColor Color => Subject == FruitKind.Orange ? FruitColor.Orange : (FruitColor)Apple.Color;
}
