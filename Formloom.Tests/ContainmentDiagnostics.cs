using System.Collections.Immutable;
using Formloom.Core;
using Formloom.Rendering.Skia;
using SkiaSharp;
using Xunit;

namespace Formloom.Tests;

// TEMPORARY diagnostics: emits measured containment metrics as GitHub Actions annotations
// so thresholds can be set from real numbers. Removed before merge.
public sealed class ContainmentDiagnostics
{
    private const int Canvas = 1024;

    [Fact]
    public void ReportMetrics()
    {
        var lines = new List<string>();
        foreach (var (name, prompt) in new[]
        {
            ("apple-blue", "a red apple in a blue ceramic bowl on a wooden table"),
            ("apple-cream", "a red apple in a cream ceramic bowl on a wooden table"),
            ("pear-cream", "a green pear in a cream ceramic bowl on a wooden table"),
            ("orange-blue", "an orange in a blue ceramic bowl on a wooden table"),
        })
        {
            try
            {
                lines.Add(Measure(name, prompt));
            }
            catch (Exception exception)
            {
                lines.Add($"{name} EX {exception.GetType().Name}: {Trim(exception.Message)}");
            }
        }

        try
        {
            lines.Add(CoreMetrics());
        }
        catch (Exception exception)
        {
            lines.Add($"core EX {exception.GetType().Name}: {Trim(exception.Message)}");
        }

        try
        {
            lines.Add(ExtraMetrics());
        }
        catch (Exception exception)
        {
            lines.Add($"extra EX {exception.GetType().Name}: {Trim(exception.Message)}");
        }

        using var stdout = Console.OpenStandardOutput();
        using var writer = new StreamWriter(stdout) { AutoFlush = true };
        foreach (var line in lines)
        {
            writer.WriteLine("::error::" + line.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " | ", StringComparison.Ordinal));
        }
    }

    private static string Trim(string value) => value.Length <= 220 ? value : value[..220];

    private static string CoreMetrics()
    {
        var minOcc = 1f;
        var maxOcc = 0f;
        var minCov = 1f;
        var maxCov = 0f;
        var minHeight = 1f;
        var maxHeight = 0f;
        var minTop = 1f;
        for (ulong seed = 0; seed < 150; seed++)
        {
            foreach (var variation in new[] { 0.0, 1.0 })
            {
                foreach (var fruit in new[] { "a red apple", "a green pear", "an orange" })
                {
                    var scene = SceneGenerator.Generate(new(
                        $"{fruit} in a cream ceramic bowl on a wooden table",
                        Seed: (seed * 7919) + 13,
                        Variation: variation)).Scene;
                    var containment = scene!.Containment!;
                    minOcc = Math.Min(minOcc, containment.OccludedFraction);
                    maxOcc = Math.Max(maxOcc, containment.OccludedFraction);
                    minCov = Math.Min(minCov, containment.OpeningCoverage);
                    maxCov = Math.Max(maxCov, containment.OpeningCoverage);
                    minHeight = Math.Min(minHeight, containment.Content.Body.Size.Y);
                    maxHeight = Math.Max(maxHeight, containment.Content.Body.Size.Y);
                    minTop = Math.Min(minTop, containment.Content.Body.Center.Y - (containment.Content.Body.Size.Y / 2));
                }
            }
        }

        var apple = SceneGenerator.Generate(new("a red apple in a blue ceramic bowl on a wooden table", Seed: 21)).Scene!.Containment!;
        var pear = SceneGenerator.Generate(new("a green pear in a blue ceramic bowl on a wooden table", Seed: 21)).Scene!.Containment!;
        var orange = SceneGenerator.Generate(new("an orange in a blue ceramic bowl on a wooden table", Seed: 21)).Scene!.Containment!;
        return FormattableString.Invariant(
            $"core: occ={minOcc:0.000}..{maxOcc:0.000} cov={minCov:0.000}..{maxCov:0.000} h={minHeight:0.000}..{maxHeight:0.000} minTop={minTop:0.000} aspect a/p/o={apple.Content.Body.Size.Y / apple.Content.Body.Size.X:0.000}/{pear.Content.Body.Size.Y / pear.Content.Body.Size.X:0.000}/{orange.Content.Body.Size.Y / orange.Content.Body.Size.X:0.000}");
    }

    private static string ExtraMetrics()
    {
        var scene = SceneGenerator.Generate(new("a green pear in a cream ceramic bowl on a wooden table")).Scene!;
        var containment = scene.Containment!;
        var bowlOnly = BowlOnly(scene);
        var contentOnly = ContentOnly(scene);
        var maskEqual = StillLifeRenderer.RenderShadowMask(scene).PngBytes.AsSpan()
            .SequenceEqual(StillLifeRenderer.RenderShadowMask(bowlOnly).PngBytes);
        using var detailed = Decode(StillLifeRenderer.Render(scene).PngBytes);
        using var smooth = Decode(StillLifeRenderer.RenderWithoutInstanceDetail(scene, containment.Content.Depth).PngBytes);
        using var mask = Decode(StillLifeRenderer.RenderSilhouette(contentOnly).PngBytes);
        var changed = 0;
        for (var y = 0; y < Canvas; y++)
        {
            for (var x = 0; x < Canvas; x++)
            {
                if (mask.GetPixel(x, y).Alpha > 0 && detailed.GetPixel(x, y) != smooth.GetPixel(x, y))
                {
                    changed++;
                }
            }
        }

        var dims = new List<string>();
        foreach (var (width, height) in new[] { (256, 256), (640, 480), (2048, 1024) })
        {
            try
            {
                var other = SceneGenerator.Generate(new("a green pear in a cream ceramic bowl on a wooden table", Width: width, Height: height)).Scene!;
                var image = StillLifeRenderer.Render(other);
                dims.Add($"{width}x{height}={image.Width}x{image.Height}");
            }
            catch (Exception exception)
            {
                dims.Add($"{width}x{height} EX {exception.GetType().Name}: {Trim(exception.Message)}");
            }
        }

        return $"extra: shadowMaskEqual={maskEqual} detailChanged={changed} dims={string.Join(',', dims)}";
    }

    private static string Measure(string name, string prompt)
    {
        var scene = SceneGenerator.Generate(new(prompt)).Scene!;
        var containment = scene.Containment!;
        using var rendered = Decode(StillLifeRenderer.Render(scene).PngBytes);
        using var bowlPixels = Decode(StillLifeRenderer.Render(BowlOnly(scene)).PngBytes);
        using var mask = Decode(StillLifeRenderer.RenderSilhouette(ContentOnly(scene)).PngBytes);
        using var silhouette = Decode(StillLifeRenderer.RenderSilhouette(scene).PngBytes);
        using var bowlSilhouette = Decode(StillLifeRenderer.RenderSilhouette(BowlOnly(scene)).PngBytes);
        var nearRimRow = (int)MathF.Floor(containment.FrontRimY * Canvas);
        var centerX = containment.OpeningCenter.X * Canvas;
        var centerY = containment.OpeningCenter.Y * Canvas;
        var radiusX = containment.OpeningRadius.X * Canvas;
        var radiusY = containment.OpeningRadius.Y * Canvas;
        var visible = 0;
        var lowest = 0;
        var highest = Canvas;
        var leaks = 0;
        var edge = 0;
        for (var y = 0; y < Canvas; y++)
        {
            for (var x = 0; x < Canvas; x++)
            {
                if (mask.GetPixel(x, y).Alpha == 0)
                {
                    continue;
                }

                highest = Math.Min(highest, y);
                if (Delta(rendered.GetPixel(x, y), bowlPixels.GetPixel(x, y)) > 24)
                {
                    visible++;
                    lowest = Math.Max(lowest, y);
                }
            }
        }

        var shadowed = 0;
        var strongest = 0;
        for (var y = 0; y < Canvas; y++)
        {
            for (var x = 0; x < Canvas; x++)
            {
                var delta = Delta(rendered.GetPixel(x, y), bowlPixels.GetPixel(x, y));
                if (delta <= 8 || mask.GetPixel(x, y).Alpha > 0)
                {
                    continue;
                }

                var dx = (x + 0.5f - centerX) / radiusX;
                var dy = (y + 0.5f - centerY) / radiusY;
                if ((dx * dx) + (dy * dy) > 1.2f)
                {
                    if (delta > 24)
                    {
                        leaks++;
                    }
                    else
                    {
                        edge++;
                    }

                    continue;
                }

                var before = bowlPixels.GetPixel(x, y);
                var after = rendered.GetPixel(x, y);
                var drop = (before.Red - after.Red) + (before.Green - after.Green) + (before.Blue - after.Blue);
                if (drop > 3 && (dx * dx) + (dy * dy) <= 0.98f)
                {
                    shadowed++;
                    strongest = Math.Max(strongest, drop);
                }
            }
        }

        var added = 0;
        var addedBelow = 0;
        for (var y = 0; y < Canvas; y++)
        {
            for (var x = 0; x < Canvas; x++)
            {
                if (silhouette.GetPixel(x, y).Alpha > 0 && bowlSilhouette.GetPixel(x, y).Alpha == 0)
                {
                    added++;
                    if (y >= containment.RearRimY * Canvas)
                    {
                        addedBelow++;
                    }
                }
            }
        }

        return FormattableString.Invariant(
            $"{name}: vis={visible} low={lowest} rim={nearRimRow} base={(int)(containment.ContentBaseY * Canvas)} leaks={leaks} edge={edge} shad={shadowed} strong={strongest} added={added} addedBelow={addedBelow} top={highest} occ={containment.OccludedFraction:0.000} cov={containment.OpeningCoverage:0.000}");
    }

    private static StillLifeScene BowlOnly(StillLifeScene scene) => scene with
    {
        Entries = ImmutableArray.Create(SceneEntry.ForCeramic(scene.Containment!.Bowl)),
        Containment = null,
    };

    private static StillLifeScene ContentOnly(StillLifeScene scene)
    {
        var content = scene.Containment!.Content;
        return scene with
        {
            Apple = content.Body,
            Subject = content.Subject,
            Seed = content.Seed,
            Instances = ImmutableArray.Create(content with { Depth = 0 }),
            Entries = null,
            Containment = null,
            TableHorizon = 0.5,
        };
    }

    private static SKBitmap Decode(byte[] png) =>
        SKBitmap.Decode(png) ?? throw new InvalidOperationException("decode failed");

    private static int Delta(SKColor first, SKColor second) => Math.Max(
        Math.Max(Math.Abs(first.Red - second.Red), Math.Abs(first.Green - second.Green)),
        Math.Max(Math.Abs(first.Blue - second.Blue), Math.Abs(first.Alpha - second.Alpha)));
}
