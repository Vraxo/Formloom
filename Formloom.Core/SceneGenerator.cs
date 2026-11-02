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

        var color = PromptParser.Parse(request.Prompt);
        if (color is null)
        {
            return new(null, "Use 'a red apple on a wooden table' (red, green or yellow). Other subjects, materials and relationships are not supported yet.");
        }

        var shape = Pcg32.ForStream(request.Seed, "apple.shape");
        var stem = Pcg32.ForStream(request.Seed, "apple.stem");
        var leaf = Pcg32.ForStream(request.Seed, "apple.leaf");
        var surface = Pcg32.ForStream(request.Seed, "apple.surface");
        var table = Pcg32.ForStream(request.Seed, "table.grain");
        var background = Pcg32.ForStream(request.Seed, "background.detail");
        var amount = request.Variation;
        var apple = new AppleDescription(
            color.Value,
            new Vector2(0.5f + (float)Offset(shape, 0.035, amount), 0.55f),
            new Vector2(0.43f + (float)Offset(shape, 0.04, amount), 0.44f + (float)Offset(shape, 0.03, amount)),
            0.025 + Offset(shape, 0.04, amount),
            -0.16 + Offset(stem, 0.2, amount),
            -0.45 + Offset(leaf, 0.25, amount),
            0.12 + Offset(surface, 0.08, amount));
        return new(new StillLifeScene(
            request.Width,
            request.Height,
            request.Seed,
            amount,
            apple,
            0.64,
            0.2 + Offset(table, 0.1, amount),
            0.5 + Offset(background, 0.15, amount),
            Vector2.Normalize(new Vector2(-1, -1))), null);
    }

    private static double Offset(Pcg32 random, double range, double variation) =>
        ((random.NextDouble() * 2) - 1) * range * variation;
}
