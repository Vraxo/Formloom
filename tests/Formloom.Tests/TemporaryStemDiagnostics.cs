using Formloom.Core;
using Formloom.Rendering.Skia;
using SkiaSharp;
using Xunit;

namespace Formloom.Tests;

// Temporary: prints the stem's brightness and coverage down the mouth as annotations.
public sealed class TemporaryStemDiagnostics
{
    private const int Canvas = 1024;

    [Fact]
    public void Report()
    {
        var scene = SceneGenerator.Generate(new("a red tulip in a blue ceramic vase on a wooden table", 42, 0.35)).Scene!;
        var flower = scene.VaseFlower!;
        using var planted = Decode(StillLifeRenderer.Render(scene).PngBytes);
        using var empty = Decode(StillLifeRenderer.Render(scene with { VaseFlower = null }).PngBytes);
        var x = (int)MathF.Round(flower.Plant.StemBase.X * Canvas);
        var rim = (int)MathF.Round((float)flower.MouthTop * Canvas);
        var bottom = (flower.OpeningCenter.Y + flower.OpeningRadius.Y) * Canvas;
        var lipInner = (int)MathF.Round(bottom) - 14;

        var above = Brightest(planted, x, rim - 6);
        var failsSeam = 0;
        var failRow = -1;
        for (var y = rim - 4; y <= rim + 4; y++)
        {
            if (Math.Abs(Brightest(planted, x, y) - above) < 45)
            {
                continue;
            }

            failsSeam++;
            if (failRow < 0)
            {
                failRow = y;
            }
        }

        var uncovered = 0;
        var uncoveredRow = -1;
        for (var y = rim + 2; y <= lipInner; y++)
        {
            var covered = false;
            for (var column = x - 2; column <= x + 2; column++)
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

        Console.WriteLine($"::error::diag stem x={x} rim={rim} bottom={bottom:0.0} lipInner={lipInner} above={above} seamFails={failsSeam} firstSeam={failRow} uncovered={uncovered} firstUncovered={uncoveredRow}");

        var samples = new List<string>();
        foreach (var y in new[] { rim - 8, rim - 4, rim, rim + 2, rim + 4, rim + 8, rim + 16, rim + 24, rim + 32, rim + 40, lipInner })
        {
            var pixel = planted.GetPixel(x, y);
            var emptyPixel = empty.GetPixel(x, y);
            samples.Add($"{y}:{Brightest(planted, x, y)}/[{(pixel.Red + pixel.Green + pixel.Blue) / 3}|{(emptyPixel.Red + emptyPixel.Green + emptyPixel.Blue) / 3}]d{Delta(pixel, emptyPixel)}");
        }

        Console.WriteLine("::error::diag rows " + string.Join(" ", samples));
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
