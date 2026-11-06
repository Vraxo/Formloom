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
    // Fruit-only scenes keep using Instances. Scenes that contain a ceramic vessel use
    // the mixed Entries projection, which also carries prompt order across categories.
    public ImmutableArray<FruitInstance>? Instances { get; init; }

    public ImmutableArray<SceneEntry>? Entries { get; init; }

    // The only containment relationship the grammar supports. When present the bowl is
    // drawn first and the content is clipped against the near wall in front of it.
    public BowlContainment? Containment { get; init; }

    // The one cup-content relationship the grammar supports. Mutually exclusive with
    // Containment: a scene is either a filled cup or a fruit in a bowl, never both.
    public CupContents? CupContents { get; init; }

    // The one vase-content relationship the grammar supports: a single tulip growing out of
    // the vase instance already in Entries. Mutually exclusive with the two relationships
    // above, and never an entry of its own.
    public VaseFlower? VaseFlower { get; init; }

    public ImmutableArray<SceneEntry> Objects
    {
        get
        {
            if (Entries is { } entries)
            {
                return entries.IsDefault ? ImmutableArray<SceneEntry>.Empty : entries;
            }

            if (Instances is { } fruits)
            {
                return fruits.IsDefault
                    ? ImmutableArray<SceneEntry>.Empty
                    : ImmutableArray.CreateRange(fruits.Select(fruit => SceneEntry.ForFruit(fruit)));
            }

            return ImmutableArray.Create(SceneEntry.ForFruit(new FruitInstance(Subject, Apple, Seed, 0)));
        }
    }

    public ImmutableArray<FruitInstance> Fruits => Entries is null
        ? Instances ?? ImmutableArray.Create(new FruitInstance(Subject, Apple, Seed, 0))
        : ImmutableArray.CreateRange(Objects.Where(entry => entry.Fruit is not null).Select(entry => entry.Fruit!));

    public ImmutableArray<CeramicInstance> Ceramics => Entries is null
        ? ImmutableArray<CeramicInstance>.Empty
        : ImmutableArray.CreateRange(Objects.Where(entry => entry.Ceramic is not null).Select(entry => entry.Ceramic!));

    public string SubjectToken
    {
        get
        {
            if (VaseFlower is { } flower)
            {
                return flower.SubjectToken;
            }

            if (CupContents is { } contents)
            {
                return contents.SubjectToken;
            }

            if (Containment is { } containment)
            {
                return containment.SubjectToken;
            }

            var objects = Objects;
            if (objects.Length == 1)
            {
                var entry = objects[0];
                return entry.Fruit is { } fruit
                    ? fruit.Subject.ToString().ToLowerInvariant()
                    : "ceramic-" + entry.Ceramic!.Kind.ToString().ToLowerInvariant();
            }

            var hasFruit = false;
            var hasCeramic = false;
            foreach (var entry in objects)
            {
                hasFruit |= entry.IsFruit;
                hasCeramic |= entry.IsCeramic;
            }

            return hasFruit && hasCeramic ? "still-life-group" : hasCeramic ? "ceramic-group" : "fruit-group";
        }
    }

    public double SurfaceDetail { get; init; } = 1;

    public FruitKind Subject { get; init; } = FruitKind.Apple;

    // Apple remains the legacy body-parameter contract for existing callers and tests.
    public AppleDescription Body => Apple;

    public FruitColor Color => Subject == FruitKind.Orange ? FruitColor.Orange : (FruitColor)Apple.Color;
}
