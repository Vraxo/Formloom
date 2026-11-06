using Formloom.Core;
using Formloom.Rendering.Skia;
using SkiaSharp;
using Xunit;

namespace Formloom.Tests;

// Temporary: reports the numbers behind the pixel invariants as workflow annotations.
public sealed class TemporaryFlowerDiagnostics
{
    private const int Canvas = 1024;

    [Fact]
    public void Report()
    {
        (string Prompt, string Name)[] cases =
        [
            ("a red tulip in a blue ceramic vase on a wooden table", "red-blue"),
            ("a yellow tulip in a cream ceramic vase on a wooden table", "yellow-cream"),
        ];

        foreach (var (prompt, name) in cases)
        {
            var scene = SceneGenerator.Generate(new(prompt, 42, 0.35)).Scene!;
            var flower = scene.VaseFlower!;
            using var planted = Decode(StillLifeRenderer.Render(scene).PngBytes);
            using var empty = Decode(StillLifeRenderer.Render(scene with { VaseFlower = null }).PngBytes);
            var rim = (int)MathF.Ceiling(flower.MouthTop * Canvas);
            var below = 0;
            var inside = 0;
            var belowX = -1;
            var belowY = -1;
            var bounds = Bounds(flower.Plant, 3);
            var outsideBoundsAbove = 0;
            for (var y = 0; y < Canvas; y++)
            {
                for (var x = 0; x < Canvas; x++)
                {
                    if (Delta(planted.GetPixel(x, y), empty.GetPixel(x, y)) == 0)
                    {
                        continue;
                    }

                    if (y > rim)
                    {
                        if (Inside(flower, x, y, 2f))
                        {
                            inside++;
                        }
                        else
                        {
                            below++;
                            if (belowX < 0)
                            {
                                belowX = x;
                                belowY = y;
                            }
                        }
                    }
                    else if (y <= rim - 14 && !InBounds(bounds, x, y))
                    {
                        outsideBoundsAbove++;
                    }
                }
            }

            Console.WriteLine(
                $"::error::diag {name} inside={inside} below={below} first=({belowX},{belowY}) " +
                $"outsideBoundsAbove={outsideBoundsAbove} bounds={bounds.Left:0.0000}:{bounds.Right:0.0000}");
        }

        var red = SceneGenerator.Generate(new("a red tulip in a cream ceramic vase on a wooden table", 42, 0.35)).Scene!;
        var yellow = SceneGenerator.Generate(new("a yellow tulip in a cream ceramic vase on a wooden table", 42, 0.35)).Scene!;
        using var redPixels = Decode(StillLifeRenderer.Render(red).PngBytes);
        using var yellowPixels = Decode(StillLifeRenderer.Render(yellow).PngBytes);
        var bloom = red.VaseFlower!.Plant.Bloom;
        var outsideBloom = 0;
        var firstX = -1;
        var firstY = -1;
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
                    outsideBloom++;
                    if (firstX < 0)
                    {
                        firstX = x;
                        firstY = y;
                    }
                }
            }
        }

        Console.WriteLine($"::error::diag redyellow outside={outsideBloom} first=({firstX},{firstY})");

        var detailed = SceneGenerator.Generate(new("a red tulip in a blue ceramic vase on a wooden table", 1337, 0.6)).Scene!;
        using var full = Decode(StillLifeRenderer.Render(detailed).PngBytes);
        using var plain = Decode(StillLifeRenderer.RenderWithoutInstanceDetail(detailed, 0).PngBytes);
        var bounds2 = Bounds(detailed.VaseFlower!.Plant, 3);
        var rim2 = (int)MathF.Ceiling(detailed.VaseFlower!.MouthTop * Canvas) - 14;
        var outside2 = 0;
        var firstX2 = -1;
        var firstY2 = -1;
        for (var y = 0; y < rim2; y++)
        {
            for (var x = 0; x < Canvas; x++)
            {
                if (Delta(full.GetPixel(x, y), plain.GetPixel(x, y)) == 0 || InBounds(bounds2, x, y))
                {
                    continue;
                }

                outside2++;
                if (firstX2 < 0)
                {
                    firstX2 = x;
                    firstY2 = y;
                }
            }
        }

        Console.WriteLine($"::error::diag detail outside={outside2} first=({firstX2},{firstY2})");
    }

    private readonly record struct Box(float Left, float Top, float Right, float Bottom);

    private static Box Bounds(TulipPlant plant, int grow)
    {
        var b = plant.Bounds;
        return new(b.Left - (grow / 1024f), b.Top - (grow / 1024f), b.Right + (grow / 1024f), b.Bottom + (grow / 1024f));
    }

    private static bool InBounds(Box box, int x, int y)
    {
        var nx = (x + 0.5f) / Canvas;
        var ny = (y + 0.5f) / Canvas;
        return nx >= box.Left && nx <= box.Right && ny >= box.Top && ny <= box.Bottom;
    }

    private static bool Inside(VaseFlower flower, int x, int y, float grow)
    {
        var rx = (flower.OpeningRadius.X * Canvas) + grow;
        var ry = (flower.OpeningRadius.Y * Canvas) + grow;
        var dx = (x + 0.5f) - (flower.OpeningCenter.X * Canvas);
        var dy = (y + 0.5f) - (flower.OpeningCenter.Y * Canvas);
        return ((dx * dx) / (rx * rx)) + ((dy * dy) / (ry * ry)) <= 1f;
    }

    private static SKBitmap Decode(byte[] png) =>
        SKBitmap.Decode(png) ?? throw new InvalidOperationException("undecodable");

    private static int Delta(SKColor first, SKColor second) => Math.Max(
        Math.Max(Math.Abs(first.Red - second.Red), Math.Abs(first.Green - second.Green)),
        Math.Max(Math.Abs(first.Blue - second.Blue), Math.Abs(first.Alpha - second.Alpha)));
}
