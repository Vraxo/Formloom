using Formloom.Core;
using Formloom.Rendering.Skia;
using SkiaSharp;
using Xunit;

namespace Formloom.Tests;

// Temporary: replays the file's other invariants and reports them as annotations.
public sealed class TemporaryStemDiagnostics
{
    private const int Canvas = 1024;

    [Fact]
    public void ReportTheOtherInvariants()
    {
        var lines = new List<string>();
        Try(lines, "L1", Silhouette);
        Try(lines, "L2", RedYellow);
        Try(lines, "L3", Glaze);
        Try(lines, "L4", Detail);
        Try(lines, "L5", Sizes);
        Try(lines, "M1", PetalsBelowTheRim);
        Assert.Fail($"temporary\n::error::diag {string.Join("\n::error::diag ", lines)}\n::error::diag end");
    }

    private static void Try(List<string> lines, string tag, Func<string> check)
    {
        try
        {
            lines.Add($"{tag} {check()}");
        }
        catch (Exception error)
        {
            lines.Add($"{tag} err {error.GetType().Name}");
        }
    }

    private static string Silhouette()
    {
        var scene = Scene("a yellow tulip in a blue ceramic vase on a wooden table");
        var flower = scene.VaseFlower!;
        var shadowSame = StillLifeRenderer.RenderShadowMask(scene).PngBytes.SequenceEqual(
            StillLifeRenderer.RenderShadowMask(scene with { VaseFlower = null }).PngBytes);
        using var empty = Decode(StillLifeRenderer.RenderSilhouette(scene with { VaseFlower = null }));
        using var planted = Decode(StillLifeRenderer.RenderSilhouette(scene));
        var rim = (int)MathF.Ceiling(flower.MouthTop * Canvas) + 14;
        var added = 0;
        var lost = 0;
        var below = 0;
        for (var y = 0; y < Canvas; y++)
        {
            for (var x = 0; x < Canvas; x++)
            {
                var before = empty.GetPixel(x, y).Alpha;
                var after = planted.GetPixel(x, y).Alpha;
                if (before > after + 4)
                {
                    lost++;
                }

                if (after > before + 4)
                {
                    added++;
                    if (y > rim)
                    {
                        below++;
                    }
                }
            }
        }

        return $"lost{lost} below{below} added{added} shadow{shadowSame}";
    }

    private static string RedYellow()
    {
        var red = Scene("a red tulip in a cream ceramic vase on a wooden table");
        var yellow = Scene("a yellow tulip in a cream ceramic vase on a wooden table");
        using var redPixels = Decode(StillLifeRenderer.Render(red));
        using var yellowPixels = Decode(StillLifeRenderer.Render(yellow));
        var bloom = red.VaseFlower!.Plant.Bloom;
        var inside = 0;
        var outside = 0;
        for (var y = 0; y < Canvas; y++)
        {
            for (var x = 0; x < Canvas; x++)
            {
                if (Delta(redPixels.GetPixel(x, y), yellowPixels.GetPixel(x, y)) == 0)
                {
                    continue;
                }

                var px = (x + 0.5f) / Canvas;
                var py = (y + 0.5f) / Canvas;
                var inBloom = px >= bloom.Left - (20f / Canvas) && px <= bloom.Right + (20f / Canvas)
                    && py >= bloom.Top - (20f / Canvas) && py <= bloom.Base.Y + (20f / Canvas);
                if (inBloom)
                {
                    inside++;
                }
                else
                {
                    outside++;
                }
            }
        }

        return $"inside{inside} outside{outside}";
    }

    private static string Glaze()
    {
        var blue = Scene("a red tulip in a blue ceramic vase on a wooden table", 42, 0.5);
        var cream = Scene("a red tulip in a cream ceramic vase on a wooden table", 42, 0.5);
        using var bluePixels = Decode(StillLifeRenderer.Render(blue));
        using var creamPixels = Decode(StillLifeRenderer.Render(cream));
        var rim = (int)MathF.Floor(blue.VaseFlower!.MouthTop * Canvas) - 4;
        var differences = 0;
        var firstRow = -1;
        for (var y = 0; y < rim; y++)
        {
            for (var x = 0; x < Canvas; x++)
            {
                if (Delta(bluePixels.GetPixel(x, y), creamPixels.GetPixel(x, y)) != 0)
                {
                    differences++;
                    if (firstRow < 0)
                    {
                        firstRow = y;
                    }
                }
            }
        }

        return $"diffs{differences} first{firstRow}";
    }

    private static string Detail()
    {
        var scene = Scene("a red tulip in a blue ceramic vase on a wooden table", 1337, 0.6);
        var flower = scene.VaseFlower!;
        using var full = Decode(StillLifeRenderer.Render(scene));
        using var plain = Decode(StillLifeRenderer.RenderWithoutInstanceDetail(scene, 0));
        var rim = (int)MathF.Ceiling(flower.MouthTop * Canvas) - 14;
        var bounds = flower.Plant.Bounds;
        var changed = 0;
        var outside = 0;
        for (var y = 0; y < rim; y++)
        {
            for (var x = 0; x < Canvas; x++)
            {
                if (Delta(full.GetPixel(x, y), plain.GetPixel(x, y)) == 0)
                {
                    continue;
                }

                changed++;
                var px = (x + 0.5f) / Canvas;
                var py = (y + 0.5f) / Canvas;
                var inside = px >= bounds.Left - (3f / Canvas) && px <= bounds.Right + (3f / Canvas)
                    && py >= bounds.Top - (3f / Canvas) && py <= bounds.Bottom + (3f / Canvas);
                if (!inside)
                {
                    outside++;
                }
            }
        }

        return $"changed{changed} outside{outside}";
    }

    private static string Sizes()
    {
        var report = string.Empty;
        foreach (var (width, height) in new[] { (64, 64), (256, 256), (640, 480), (2048, 1024), (4096, 1024) })
        {
            var scene = SceneGenerator.Generate(
                new("a yellow tulip in a cream ceramic vase on a wooden table", Width: width, Height: height)).Scene!;
            using var planted = Decode(StillLifeRenderer.Render(scene));
            using var empty = Decode(StillLifeRenderer.Render(scene with { VaseFlower = null }));
            var changed = 0;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (Delta(planted.GetPixel(x, y), empty.GetPixel(x, y)) != 0)
                    {
                        changed++;
                    }
                }
            }

            report += $" {width}x{height}:{changed}";
        }

        return report;
    }

    private static string PetalsBelowTheRim()
    {
        var report = string.Empty;
        foreach (var (prompt, name, seed, variation) in new[]
        {
            ("a yellow tulip in a cream ceramic vase on a wooden table", "yc", 42, 0.35),
            ("a yellow tulip in a blue ceramic vase on a wooden table", "yb", 42, 0.35),
            ("a yellow tulip in a blue ceramic vase on a wooden table", "ybh", 42, 1),
        })
        {
            var scene = Scene(prompt, (ulong)seed, variation);
            var flower = scene.VaseFlower!;
            using var pixels = Decode(StillLifeRenderer.Render(scene));
            var rim = (int)MathF.Ceiling(flower.MouthTop * Canvas);
            var horizon = (int)MathF.Floor((float)scene.TableHorizon * Canvas);
            var count = 0;
            var first = string.Empty;
            for (var y = rim; y < horizon; y++)
            {
                for (var x = 0; x < Canvas; x++)
                {
                    var pixel = pixels.GetPixel(x, y);
                    if (pixel.Red > 150 && pixel.Green > 110 && pixel.Blue < 90)
                    {
                        count++;
                        if (first.Length == 0)
                        {
                            first = $"{x},{y}:{pixel.Red}.{pixel.Green}.{pixel.Blue}/c{Column(flower.Plant, y)}";
                        }
                    }
                }
            }

            report += $" {name}{count} {first}";
        }

        return report;
    }

    private static StillLifeScene Scene(string prompt, ulong seed = 20261105, double variation = 0.35) =>
        SceneGenerator.Generate(new(prompt, seed, variation)).Scene!;

    private static SKBitmap Decode(RenderedImage image) => SKBitmap.Decode(image.PngBytes);

    private static int Delta(SKColor left, SKColor right) =>
        Math.Abs(left.Red - right.Red) + Math.Abs(left.Green - right.Green) + Math.Abs(left.Blue - right.Blue);
}
