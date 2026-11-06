using Formloom.Core;
using Formloom.Rendering.Skia;
using SkiaSharp;
using Xunit;

namespace Formloom.Tests;

// Temporary: prints the numbers behind the failing invariants as workflow annotations so the
// sandbox can read them back without artifact access. Deleted before the pull request lands.
public sealed class TemporaryFlowerDiagnostics
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
        ];

        foreach (var (prompt, name, seed, variation) in cases)
        {
            var scene = SceneGenerator.Generate(new(prompt, seed, variation)).Scene!;
            var flower = scene.VaseFlower!;
            using var planted = Decode(StillLifeRenderer.Render(scene).PngBytes);
            using var empty = Decode(StillLifeRenderer.Render(scene with { VaseFlower = null }).PngBytes);
            var rim = (int)MathF.Ceiling(flower.MouthTop * Canvas);
            var changed = 0;
            var below = 0;
            var belowGrown = 0;
            var inside = 0;
            var worstX = -1;
            var worstY = -1;
            var worstDelta = 0;
            var offX = -1;
            var offY = -1;
            for (var y = 0; y < Canvas; y++)
            {
                for (var x = 0; x < Canvas; x++)
                {
                    var delta = Delta(planted.GetPixel(x, y), empty.GetPixel(x, y));
                    if (delta == 0)
                    {
                        continue;
                    }

                    changed++;
                    if (delta > worstDelta)
                    {
                        worstDelta = delta;
                        worstX = x;
                        worstY = y;
                    }

                    if (y <= rim)
                    {
                        continue;
                    }

                    if (InsideOpening(flower, x, y, 2f))
                    {
                        inside++;
                    }
                    else
                    {
                        belowGrown++;
                        if (!InsideOpening(flower, x, y, 6f) && offX < 0)
                        {
                            offX = x;
                            offY = y;
                        }
                    }

                    if (!InsideOpening(flower, x, y, 0f))
                    {
                        below++;
                    }
                }
            }

            Console.WriteLine(
                $"::error::diag {name} rim={rim} changed={changed} below={below} belowGrown2={belowGrown} " +
                $"inside={inside} worst=({worstX},{worstY},d{worstDelta}) firstOutside6=({offX},{offY}) " +
                $"opening=({flower.OpeningCenter.X:0.0000},{flower.OpeningCenter.Y:0.0000})" +
                $"r({flower.OpeningRadius.X:0.0000},{flower.OpeningRadius.Y:0.0000})");
        }

        var red = SceneGenerator.Generate(new("a red tulip in a cream ceramic vase on a wooden table", 42, 0.35)).Scene!;
        var yellow = SceneGenerator.Generate(new("a yellow tulip in a cream ceramic vase on a wooden table", 42, 0.35)).Scene!;
        using var redPixels = Decode(StillLifeRenderer.Render(red).PngBytes);
        using var yellowPixels = Decode(StillLifeRenderer.Render(yellow).PngBytes);
        var bloom = red.VaseFlower!.Plant.Bloom;
        var outside = 0;
        var outsideX = -1;
        var outsideY = -1;
        for (var y = 0; y < Canvas; y++)
        {
            for (var x = 0; x < Canvas; x++)
            {
                if (Delta(redPixels.GetPixel(x, y), yellowPixels.GetPixel(x, y)) == 0)
                {
                    continue;
                }

                var inBox = x >= (bloom.Left * Canvas) - 20 && x <= (bloom.Right * Canvas) + 20 &&
                            y >= (bloom.Top * Canvas) - 20 && y <= (bloom.Base.Y * Canvas) + 20;
                if (!inBox)
                {
                    outside++;
                    if (outsideX < 0)
                    {
                        outsideX = x;
                        outsideY = y;
                    }
                }
            }
        }

        Console.WriteLine($"::error::diag redvsyellow outside={outside} first=({outsideX},{outsideY}) bloomTop={bloom.Top:0.0000} bloomBase={bloom.Base.Y:0.0000}");

        var detailed = SceneGenerator.Generate(new("a red tulip in a blue ceramic vase on a wooden table", 1337, 0.6)).Scene!;
        using var full = Decode(StillLifeRenderer.Render(detailed).PngBytes);
        using var plain = Decode(StillLifeRenderer.RenderWithoutInstanceDetail(detailed, 0).PngBytes);
        var rim2 = (int)MathF.Ceiling(detailed.VaseFlower!.MouthTop * Canvas) - 14;
        var diffAbove = 0;
        for (var y = 0; y < rim2; y++)
        {
            for (var x = 0; x < Canvas; x++)
            {
                if (Delta(full.GetPixel(x, y), plain.GetPixel(x, y)) != 0)
                {
                    diffAbove++;
                }
            }
        }

        Console.WriteLine($"::error::diag detail diffAboveRim={diffAbove}");
    }

    private static bool InsideOpening(VaseFlower flower, int x, int y, float grow)
    {
        var radiusX = (flower.OpeningRadius.X * Canvas) + grow;
        var radiusY = (flower.OpeningRadius.Y * Canvas) + grow;
        var dx = (x + 0.5f) - (flower.OpeningCenter.X * Canvas);
        var dy = (y + 0.5f) - (flower.OpeningCenter.Y * Canvas);
        return ((dx * dx) / (radiusX * radiusX)) + ((dy * dy) / (radiusY * radiusY)) <= 1f;
    }

    private static SKBitmap Decode(byte[] png) =>
        SKBitmap.Decode(png) ?? throw new InvalidOperationException("undecodable");

    private static int Delta(SKColor first, SKColor second) => Math.Max(
        Math.Max(Math.Abs(first.Red - second.Red), Math.Abs(first.Green - second.Green)),
        Math.Max(Math.Abs(first.Blue - second.Blue), Math.Abs(first.Alpha - second.Alpha)));
}
