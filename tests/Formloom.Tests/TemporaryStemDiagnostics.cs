using Formloom.Core;
using Formloom.Rendering.Skia;
using SkiaSharp;
using Xunit;

namespace Formloom.Tests;

// Temporary: prints where the stem sits relative to the rim band through the CI annotations.
public sealed class TemporaryStemDiagnostics
{
    [Fact]
    public void DumpTheRimBandAcrossTheStem()
    {
        var scene = SceneGenerator.Generate(new("a red tulip in a blue ceramic vase on a wooden table", 42, 0.35)).Scene!;
        var flower = scene.VaseFlower!;
        var image = StillLifeRenderer.Render(scene);
        using var bitmap = SKBitmap.Decode(image.PngBytes);
        var rim = (int)MathF.Round((float)flower.MouthTop * 1024);
        var green = string.Empty;
        for (var y = rim - 8; y <= rim + 8; y += 2)
        {
            var column = Column(flower.Plant, y);
            var best = -255;
            for (var x = column - 2; x <= column + 2; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                best = Math.Max(best, pixel.Green - Math.Max(pixel.Red, pixel.Blue));
            }

            green += $" {y}:{best}";
        }

        Console.WriteLine($"::error::diag green rim{rim}{green}");

        var steps = string.Empty;
        var worst = 0;
        var worstRow = 0;
        var above = -1;
        for (var y = rim - 8; y <= rim + 40; y++)
        {
            var column = Column(flower.Plant, y);
            var best = 0;
            for (var x = column - 2; x <= column + 2; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                best = Math.Max(best, (pixel.Red + pixel.Green + pixel.Blue) / 3);
            }

            if (above >= 0 && Math.Abs(best - above) > worst)
            {
                worst = Math.Abs(best - above);
                worstRow = y;
            }

            above = best;
        }

        steps += $" step{worst}@{worstRow}";
        Console.WriteLine($"::error::diag step{steps} lip{(int)MathF.Round((flower.OpeningCenter.Y + flower.OpeningRadius.Y) * 1024)}");
    }

    private static int Column(TulipPlant plant, int y)
    {
        var target = (y + 0.5f) / 1024f;
        var best = float.MaxValue;
        var bestX = plant.StemBase.X;
        foreach (var segment in plant.Stem)
        {
            for (var step = 0; step <= 64; step++)
            {
                var t = step / 64f;
                var u = 1 - t;
                var a = u * u * u;
                var b = 3 * u * u * t;
                var c = 3 * u * t * t;
                var d = t * t * t;
                var x = (a * segment.Start.X) + (b * segment.Control1.X) + (c * segment.Control2.X) + (d * segment.End.X);
                var pointY = (a * segment.Start.Y) + (b * segment.Control1.Y) + (c * segment.Control2.Y) + (d * segment.End.Y);
                var distance = MathF.Abs(pointY - target);
                if (distance < best)
                {
                    best = distance;
                    bestX = x;
                }
            }
        }

        return (int)MathF.Round(bestX * 1024);
    }
}
