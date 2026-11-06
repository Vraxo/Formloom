using System.Collections.Immutable;
using System.Numerics;
using Formloom.Core.Randomness;

namespace Formloom.Core;

public static class SceneGenerator
{
    private static readonly AppleDescription NeutralBody = new(
        AppleColor.Red,
        new Vector2(0.5f, 0.55f),
        new Vector2(0.43f, 0.44f),
        0.025,
        -0.16,
        -0.45,
        0.12);

    public static GenerationResult Generate(GenerationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Width is < 64 or > 4096 || request.Height is < 64 or > 4096)
        {
            return new(null, "Canvas dimensions must each be between 64 and 4096 pixels.");
        }

        if (!double.IsFinite(request.Variation) || request.Variation is < 0 or > 1)
        {
            return new(null, "Variation must be a finite number between 0 and 1.");
        }

        if (PromptParser.ParseContainment(request.Prompt) is { } containment)
        {
            return GenerateContainment(containment, request);
        }

        if (PromptParser.ParseCupContents(request.Prompt) is { } cupContents)
        {
            return GenerateCupContents(cupContents, request);
        }

        var parsed = PromptParser.ParseObjects(request.Prompt);
        if (parsed is null)
        {
            return new(null, "That prompt is not supported. Use one to three objects on a wooden table: a red/green/yellow apple, a green/yellow pear, an orange, or a blue/cream ceramic bowl, cup or vase. Join objects with 'and'; three-object lists may use commas. One fruit may also be placed inside a bowl: '<fruit> in a blue ceramic bowl on a wooden table'. One cup may be filled with coffee or tea: 'a blue ceramic cup filled with coffee on a wooden table'.");
        }

        return GenerateObjects(parsed.Value, request);
    }

    private static GenerationResult GenerateObjects(ImmutableArray<ObjectPrompt> parsed, GenerationRequest request)
    {
        var entries = ImmutableArray.CreateBuilder<SceneEntry>(parsed.Length);
        var occurrences = new Dictionary<string, int>();
        foreach (var description in parsed)
        {
            occurrences.TryGetValue(description.Key, out var occurrence);
            occurrences[description.Key] = occurrence + 1;
            var instanceSeed = request.Seed;
            if (occurrence > 0)
            {
                var stream = Pcg32.ForStream(request.Seed, $"instance.{description.Key}.{occurrence}");
                instanceSeed = ((ulong)stream.NextUInt32() << 32) | stream.NextUInt32();
            }

            var seeded = request with { Seed = instanceSeed };
            if (description.Fruit is { } fruit)
            {
                entries.Add(SceneEntry.ForFruit(new(fruit.Kind, CreateBody(fruit, seeded), instanceSeed, 0)));
            }
            else
            {
                var ceramic = description.Ceramic!;
                entries.Add(SceneEntry.ForCeramic(new(ceramic.Kind, ceramic.Glaze, CreateCeramicBody(ceramic, seeded), instanceSeed, 0)));
            }
        }

        var arranged = Arrange(entries.MoveToImmutable(), request);
        var lead = arranged.FirstOrDefault(entry => entry.Fruit is not null).Fruit;
        var table = Pcg32.ForStream(request.Seed, "table.grain");
        var background = Pcg32.ForStream(request.Seed, "background.detail");
        var amount = request.Variation;
        var scene = new StillLifeScene(
            request.Width,
            request.Height,
            request.Seed,
            amount,
            lead?.Body ?? NeutralBody,
            0.64,
            0.2 + Offset(table, 0.1, amount),
            0.5 + Offset(background, 0.15, amount),
            Vector2.Normalize(new Vector2(-1, -1)))
        {
            Subject = lead?.Subject ?? FruitKind.Apple,
            Instances = arranged.Any(entry => entry.IsCeramic) || arranged.Length == 1
                ? null
                : ImmutableArray.CreateRange(arranged.Select(entry => entry.Fruit!)),
            Entries = arranged.Any(entry => entry.IsCeramic) ? arranged : null,
        };
        var error = SceneValidator.Validate(scene);
        return error is null ? new(scene, null) : new(null, error);
    }

    // Containment keeps a single, typed relation instead of a scene graph: the bowl is the
    // container, the fruit is its content, and the fruit's transform is derived from the
    // bowl's. The setting streams match the other scene types so the table and background
    // stay consistent for a given seed.
    private static GenerationResult GenerateContainment(ContainmentPrompt prompt, GenerationRequest request)
    {
        var bowlPrompt = prompt.Bowl with { Kind = CeramicKind.Bowl };
        var bowl = new CeramicInstance(
            CeramicKind.Bowl,
            bowlPrompt.Glaze,
            CreateCeramicBody(bowlPrompt, request),
            request.Seed,
            0);
        var contentSeed = BowlContainmentPlacement.ContentSeed(request.Seed);
        var content = new FruitInstance(
            prompt.Content.Kind,
            CreateBody(prompt.Content, request with { Seed = contentSeed }),
            contentSeed,
            1);
        var containment = BowlContainmentPlacement.Place(bowl, content, request.Seed, request.Variation);
        var table = Pcg32.ForStream(request.Seed, "table.grain");
        var background = Pcg32.ForStream(request.Seed, "background.detail");
        var amount = request.Variation;
        var scene = new StillLifeScene(
            request.Width,
            request.Height,
            request.Seed,
            amount,
            containment.Content.Body,
            0.64,
            0.2 + Offset(table, 0.1, amount),
            0.5 + Offset(background, 0.15, amount),
            Vector2.Normalize(new Vector2(-1, -1)))
        {
            Subject = containment.Content.Subject,
            Entries = ImmutableArray.Create(
                SceneEntry.ForCeramic(containment.Bowl),
                SceneEntry.ForFruit(containment.Content)),
            Containment = containment,
        };
        var error = SceneValidator.Validate(scene);
        return error is null ? new(scene, null) : new(null, error);
    }

    // A filled cup is built by the ordinary single-object path and then carries the drink as
    // typed contents of that exact cup. The cup therefore keeps its body, glaze, seed, depth
    // and handle, and the filled render can differ from the empty one only inside the mouth.
    private static GenerationResult GenerateCupContents(CupContentsPrompt prompt, GenerationRequest request)
    {
        var empty = GenerateObjects([ObjectPrompt.FromCeramic(prompt.Cup)], request);
        if (empty.Scene is not { } scene)
        {
            return empty;
        }

        var filled = scene with { CupContents = new CupContents(scene.Ceramics[0], prompt.Beverage) };
        var error = SceneValidator.Validate(filled);
        return error is null ? new(filled, null) : new(null, error);
    }

    private static ImmutableArray<SceneEntry> Arrange(ImmutableArray<SceneEntry> entries, GenerationRequest request)
    {
        if (entries.All(entry => entry.IsFruit))
        {
            var fruits = FruitComposition.Arrange(
                ImmutableArray.CreateRange(entries.Select(entry => entry.Fruit!)),
                request.Seed,
                request.Variation);
            return ImmutableArray.CreateRange(fruits.Select(fruit => SceneEntry.ForFruit(fruit)));
        }

        return MixedComposition.Arrange(entries, request.Seed, request.Variation);
    }

    private static AppleDescription CreateBody(FruitPrompt parsed, GenerationRequest request)
    {
        var name = parsed.Kind.ToString().ToLowerInvariant();
        var shape = Pcg32.ForStream(request.Seed, $"{name}.shape");
        var stem = Pcg32.ForStream(request.Seed, $"{name}.stem");
        var leaf = Pcg32.ForStream(request.Seed, $"{name}.leaf");
        var surface = Pcg32.ForStream(request.Seed, $"{name}.surface");
        var amount = request.Variation;
        var apple = new AppleDescription(
            parsed.Kind == FruitKind.Orange ? AppleColor.Yellow : (AppleColor)parsed.Color,
            new Vector2(0.5f + (float)Offset(shape, 0.035, amount), 0.55f),
            new Vector2(0.43f + (float)Offset(shape, 0.04, amount), 0.44f + (float)Offset(shape, 0.03, amount)),
            0.025 + Offset(shape, 0.04, amount),
            -0.16 + Offset(stem, 0.2, amount),
            -0.45 + Offset(leaf, 0.25, amount),
            0.12 + Offset(surface, 0.08, amount));
        if (parsed.Kind == FruitKind.Pear)
        {
            apple = apple with { Center = new Vector2(apple.Center.X, 0.515f), Size = new Vector2(apple.Size.X * 0.95f, apple.Size.Y * 1.18f) };
        }
        else if (parsed.Kind == FruitKind.Orange)
        {
            apple = apple with { Center = new Vector2(apple.Center.X, 0.56f), Size = new Vector2(apple.Size.X, apple.Size.X) };
        }

        return apple;
    }

    private static CeramicDescription CreateCeramicBody(CeramicPrompt parsed, GenerationRequest request)
    {
        var name = parsed.Kind.ToString().ToLowerInvariant();
        var shape = Pcg32.ForStream(request.Seed, $"{name}.form");
        var amount = request.Variation;
        var profile = CeramicForms.For(parsed.Kind);
        var baseSize = parsed.Kind switch
        {
            CeramicKind.Bowl => new Vector2(0.56f, 0.40f),
            CeramicKind.Cup => new Vector2(0.40f, 0.48f),
            _ => new Vector2(0.34f, 0.70f),
        };
        var size = new Vector2(
            baseSize.X * (float)(1 + Offset(shape, 0.035, amount)),
            baseSize.Y * (float)(1 + Offset(shape, 0.04, amount)));
        var visualCenter = (profile.OutlineLeft + profile.OutlineRight) / 2;
        var horizontal = 0.5f - ((visualCenter - 0.5f) * size.X);
        return new(
            new Vector2(horizontal + (float)Offset(shape, 0.012, amount), 0.775f - ((profile.BaseBottom - 0.5f) * size.Y)),
            size,
            0.1 + Offset(shape, 0.06, amount));
    }

    private static double Offset(Pcg32 random, double range, double variation) =>
        ((random.NextDouble() * 2) - 1) * range * variation;
}
