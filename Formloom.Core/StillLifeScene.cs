using System.Collections.Immutable;
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
    public ImmutableArray<FruitInstance>? Instances { get; init; }

    public ImmutableArray<FruitInstance> Fruits => Instances ?? ImmutableArray.Create(new FruitInstance(Subject, Apple, Seed, 0));

    public string SubjectToken => Fruits.Length == 1
        ? Fruits[0].Subject.ToString().ToLowerInvariant()
        : "fruit-group";

    public double SurfaceDetail { get; init; } = 1;

    public FruitKind Subject { get; init; } = FruitKind.Apple;

    // Apple remains the legacy body-parameter contract for existing callers and tests.
    public AppleDescription Body => Apple;

    public FruitColor Color => Subject == FruitKind.Orange ? FruitColor.Orange : (FruitColor)Apple.Color;
}
