using System.Numerics;
using Formloom.Core.Randomness;

namespace Formloom.Core;

public static class SceneGenerator
{
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

        var parsed = PromptParser.ParseFruit(request.Prompt);
        if (parsed is null)
        {
            return new(null, "Use a red/green/yellow apple, a green/yellow pear, or 'an orange on a wooden table'. Other combinations are not supported yet.");
        }

        var name = parsed.Kind.ToString().ToLowerInvariant();
        var shape = Pcg32.ForStream(request.Seed, $"{name}.shape");
        var stem = Pcg32.ForStream(request.Seed, $"{name}.stem");
        var leaf = Pcg32.ForStream(request.Seed, $"{name}.leaf");
        var surface = Pcg32.ForStream(request.Seed, $"{name}.surface");
        var table = Pcg32.ForStream(request.Seed, "table.grain");
        var background = Pcg32.ForStream(request.Seed, "background.detail");
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

        var scene = new StillLifeScene(
            request.Width,
            request.Height,
            request.Seed,
            amount,
            apple,
            0.64,
            0.2 + Offset(table, 0.1, amount),
            0.5 + Offset(background, 0.15, amount),
            Vector2.Normalize(new Vector2(-1, -1)))
        {
            Subject = parsed.Kind,
        };
        return new(scene, null);
    }

    private static double Offset(Pcg32 random, double range, double variation) =>
        ((random.NextDouble() * 2) - 1) * range * variation;
}
