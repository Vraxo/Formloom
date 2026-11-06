using Formloom.Core;
using Formloom.Rendering.Skia;
using SkiaSharp;
using Xunit;

namespace Formloom.Tests;

// Temporary: one annotation line covering the stem-entry checks for every export case.
public sealed class TemporaryStemDiagnostics
{
    private const int Canvas = 1024;

    [Fact]
    public void Report()
    {
        (string Prompt, string Name, ulong Seed, double Variation)[] cases =
        [
            ("a red tulip in a blue ceramic vase on a wooden table", "red-blue", 42, 0.35),
            ("a red tulip in a cream ceramic vase on a wooden table", "red-cream", 42, 0.35),
            ("a yellow tulip in a blue ceramic vase on a wooden table", "yellow-blue", 42, 0.35),
            ("a yellow tulip in a cream ceramic vase on a wooden table", "yellow-cream", 42, 0.35),
            ("a red tulip in a blue ceramic vase on a wooden table", "red-blue-alt", 1337, 0.35),
            ("a yellow tulip in a cream ceramic vase on a wooden table", "yellow-cream-alt", 20261105, 0.35),
            ("a red tulip in a cream ceramic vase on a wooden table", "red-cream-low", 42, 0),
            ("a yellow tulip in a blue ceramic vase on a wooden table", "yellow-blue-high", 42, 1),
        ];

        foreach (var (prompt, name, seed, variation) in cases)
        {
            var scene = SceneGenerator.Generate(new(prompt, seed, variation)).Scene!;
            var flower = scene.VaseFlower!;
            using var planted = Decode(StillLifeRenderer.Render(scene).PngBytes);
            using var empty = Decode(StillLifeRenderer.Render(scene with { VaseFlower = null }).PngBytes);
            var rim = (int)MathF.Round((float)flower.MouthTop * Canvas);
            var bottom = (flower.OpeningCenter.Y + flower.OpeningRadius.Y) * Canvas;
            var lipInner = (int)MathF.Round(bottom) - 14;
            var above = Brightest(planted, Column(flower.Plant, rim - 6), rim - 6);
            var seam = 0;
            var seamRow = -1;
            var worst = 0;
            for (var y = rim - 4; y <= rim + 4; y++)
            {
                var delta = Math.Abs(Brightest(planted, Column(flower.Plant, y), y) - above);
                worst = Math.Max(worst, delta);
                if (delta >= 45)
                {
                    seam++;
                    if (seamRow < 0)
                    {
                        seamRow = y;
                    }
                }
            }

            var uncovered = 0;
            var uncoveredRow = -1;
            for (var y = rim + 2; y <= lipInner; y++)
            {
                var centre = Column(flower.Plant, y);
                var covered = false;
                for (var column = centre - 3; column <= centre + 3; column++)
                {
                    covered |= Delta(planted.GetPixel(column, y), empty.GetPixel(column, y)) > 0;
                }

                if (!covered)
                {
                    uncovered++;
                    if (uncoveredRow < 0)
                    {
                        uncoveredRow = y;
                    }
                }
            }

            Console.WriteLine($"::error::diag {name} seam{seam}@{seamRow}(w{worst}) gap{uncovered}@{uncoveredRow} rim{rim} lip{lipInner} above{above}");
        }
    }

    private static int Column(TulipPlant plant, int y)
    {
        var from = plant.StemTop;
        var to = plant.StemBase;
        var normalized = (y + 0.5f) / Canvas;
        var along = (normalized - from.Y) / (to.Y - from.Y);
        return (int)MathF.Round((from.X + (along * (to.X - from.X))) * Canvas);
    }

    private static int Brightest(SKBitmap image, int x, int y)
    {
        var best = 0;
        for (var column = x - 1; column <= x + 1; column++)
        {
            var pixel = image.GetPixel(column, y);
            best = Math.Max(best, (pixel.Red + pixel.Green + pixel.Blue) / 3);
        }

        return best;
    }

    private static SKBitmap Decode(byte[] png) =>
        SKBitmap.Decode(png) ?? throw new InvalidOperationException("undecodable");

    private static int Delta(SKColor first, SKColor second) => Math.Max(
        Math.Max(Math.Abs(first.Red - second.Red), Math.Abs(first.Green - second.Green)),
        Math.Max(Math.Abs(first.Blue - second.Blue), Math.Abs(first.Alpha - second.Alpha)));
}
