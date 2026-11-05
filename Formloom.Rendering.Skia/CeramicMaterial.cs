using Formloom.Core;
using Formloom.Core.Randomness;
using SkiaSharp;

namespace Formloom.Rendering.Skia;

internal sealed record CeramicMaterial(
    SKColor BaseColor,
    SKColor EdgeColor,
    SKColor LipColor,
    SKColor CavityColor,
    byte HighlightAlpha,
    float HighlightRadius,
    byte MottleAlpha)
{
    public static CeramicMaterial For(CeramicInstance ceramic, double variation, double detail)
    {
        var random = Pcg32.ForStream(ceramic.Seed, ceramic.Kind.ToString().ToLowerInvariant() + ".glaze");
        var shift = (int)Math.Round(((random.NextDouble() * 2) - 1) * 10 * variation);
        var mottle = (byte)Math.Clamp(Math.Round(12 * detail), 0, 255);
        return ceramic.Glaze == CeramicGlaze.Blue
            ? new(
                Shift(new SKColor(84, 122, 164), shift),
                new SKColor(30, 52, 82),
                new SKColor(190, 212, 232),
                new SKColor(24, 40, 62),
                104,
                0.30f,
                mottle)
            : new(
                Shift(new SKColor(226, 213, 186), shift),
                new SKColor(138, 120, 92),
                new SKColor(252, 246, 230),
                new SKColor(126, 108, 80),
                118,
                0.32f,
                mottle);
    }

    private static SKColor Shift(SKColor color, int amount) => new(
        (byte)Math.Clamp(color.Red + amount, 0, 255),
        (byte)Math.Clamp(color.Green + amount, 0, 255),
        (byte)Math.Clamp(color.Blue + amount, 0, 255));
}
