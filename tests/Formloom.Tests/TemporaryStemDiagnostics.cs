using Formloom.Core;
using Formloom.Rendering.Skia;
using SkiaSharp;
using Xunit;

namespace Formloom.Tests;

// Temporary: reports where the stem's in-mouth run ends, as an annotation.
public sealed class TemporaryStemDiagnostics
{
    private const int Canvas = 1024;

    [Fact]
    public void ReportWhereTheStemEnds()
    {
        var report = string.Empty;
        foreach (var (prompt, name) in new[]
        {
            ("a red tulip in a blue ceramic vase on a wooden table", "rb"),
            ("a yellow tulip in a blue ceramic vase on a wooden table", "yb"),
        })
        {
            var scene = SceneGenerator.Generate(new(prompt, 42, 0.35)).Scene!;
            var flower = scene.VaseFlower!;
            using var pixels = SKBitmap.Decode(StillLifeRenderer.Render(scene).PngBytes);
            var bottom = (int)MathF.Round((float)(flower.OpeningCenter.Y + flower.OpeningRadius.Y) * Canvas);
            var last = 0;
            var profile = string.Empty;
            for (var y = bottom; y >= bottom - 24; y--)
            {
                var column = Column(flower.Plant, y);
                var best = -255;
                for (var x = column - 2; x <= column + 2; x++)
                {
                    var pixel = pixels.GetPixel(x, y);
                    best = Math.Max(best, pixel.Green - Math.Max(pixel.Red, pixel.Blue));
                }

                if (best >= 8 && last == 0)
                {
                    last = y;
                }

                if (y >= bottom - 12)
                {
                    profile = $" {y}:{best}{profile}";
                }
            }

            report += $" {name}bottom{bottom}last{last}{profile}";
        }

        Assert.Fail($"temporary\n::error::diag E{report}\n::error::diag end");
    }

    private static int Column(TulipPlant plant, int y)
    {
        var target = (y + 0.5f) / Canvas;
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

        return (int)MathF.Round(bestX * Canvas);
    }
}
