using Formloom.Core;
using Formloom.Rendering.Skia;
using SkiaSharp;
using Xunit;

namespace Formloom.Tests;

// Temporary: reports, in one short annotation, which export case breaks which invariant and
// what the stem's own column looks like across the rim band.
public sealed class TemporaryStemDiagnostics
{
    private static readonly (string Prompt, string Name, int Seed, double Variation)[] Cases =
    [
        ("a red tulip in a blue ceramic vase on a wooden table", "red-blue", 42, 0.35),
        ("a red tulip in a cream ceramic vase on a wooden table", "red-cream", 42, 0.35),
        ("a yellow tulip in a blue ceramic vase on a wooden table", "yellow-blue", 42, 0.35),
        ("a yellow tulip in a cream ceramic vase on a wooden table", "yellow-cream", 42, 0.35),
        ("a red tulip in a blue ceramic vase on a wooden table", "red-blue-altseed", 1337, 0.35),
        ("a yellow tulip in a cream ceramic vase on a wooden table", "yellow-cream-altseed", 20261105, 0.35),
        ("a red tulip in a cream ceramic vase on a wooden table", "red-cream-lowvariation", 42, 0),
        ("a yellow tulip in a blue ceramic vase on a wooden table", "yellow-blue-highvariation", 42, 1),
    ];

    [Fact]
    public void ReportTheMouthBand()
    {
        var failures = string.Empty;
        foreach (var (prompt, name, seed, variation) in Cases)
        {
            var scene = SceneGenerator.Generate(new(prompt, (ulong)seed, variation)).Scene!;
            var flower = scene.VaseFlower!;
            var planted = StillLifeRenderer.Render(scene);
            var emptyScene = scene with { VaseFlower = null };
            var empty = StillLifeRenderer.Render(emptyScene);
            using var plantedBitmap = SKBitmap.Decode(planted.PngBytes);
            using var emptyBitmap = SKBitmap.Decode(empty.PngBytes);
            var rim = (int)MathF.Ceiling(flower.MouthTop * 1024);
            var lipInner = (int)MathF.Round((flower.OpeningCenter.Y + flower.OpeningRadius.Y) * 1024) - 14;
            var below = 0;
            var inside = 0;
            var gaps = 0;
            for (var y = 0; y < 1024; y++)
            {
                var uncovered = y >= rim - 8 && y <= lipInner;
                for (var x = 0; x < 1024; x++)
                {
                    if (Same(plantedBitmap.GetPixel(x, y), emptyBitmap.GetPixel(x, y)))
                    {
                        continue;
                    }

                    if (uncovered && Math.Abs(x - Column(flower.Plant, y)) <= 2)
                    {
                        uncovered = false;
                    }

                    if (y > rim)
                    {
                        if (Inside(flower, x, y))
                        {
                            inside++;
                        }
                        else
                        {
                            below++;
                        }
                    }
                }

                if (uncovered)
                {
                    gaps++;
                }
            }

            if (below != 0 || inside < 61 || gaps != 0)
            {
                failures += $" {name}:b{below}:i{inside}:g{gaps}";
            }
        }

        if (failures.Length != 0)
        {
            Assert.Fail($"::error::diag{failures}");
            return;
        }

        var scene2 = SceneGenerator.Generate(new("a red tulip in a blue ceramic vase on a wooden table", 42, 0.35)).Scene!;
        var flower2 = scene2.VaseFlower!;
        using var bitmap = SKBitmap.Decode(StillLifeRenderer.Render(scene2).PngBytes);
        var rim2 = (int)MathF.Round((float)flower2.MouthTop * 1024);
        var green = string.Empty;
        for (var y = rim2 - 6; y <= rim2 + 8; y += 2)
        {
            var column = Column(flower2.Plant, y);
            var best = -255;
            for (var x = column - 2; x <= column + 2; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                best = Math.Max(best, pixel.Green - Math.Max(pixel.Red, pixel.Blue));
            }

            green += $" {y}:{best}";
        }

        Assert.Fail($"::error::diag ok rim{rim2}{green}");
    }

    private static bool Same(SKColor left, SKColor right) =>
        left.Red == right.Red && left.Green == right.Green && left.Blue == right.Blue;

    private static bool Inside(VaseFlower flower, int x, int y)
    {
        var center = flower.OpeningCenter;
        var radius = flower.OpeningRadius;
        var dx = ((x + 0.5f) / 1024f) - center.X;
        var dy = ((y + 0.5f) / 1024f) - center.Y;
        var grow = 2f / 1024f;
        var rx = radius.X + grow;
        var ry = radius.Y + grow;
        return ((dx * dx) / (rx * rx)) + ((dy * dy) / (ry * ry)) <= 1f;
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
