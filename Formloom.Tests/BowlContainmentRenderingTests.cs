using System.Collections.Immutable;
using System.Diagnostics;
using Formloom.Core;
using Formloom.Rendering.Skia;
using SkiaSharp;
using Xunit;

namespace Formloom.Tests;

public sealed class BowlContainmentRenderingTests
{
    private const int Canvas = 1024;

    private static readonly string[] Fruits =
    [
        "a red apple",
        "a green apple",
        "a yellow apple",
        "a green pear",
        "a yellow pear",
        "an orange",
    ];

    // Default render set plus an alternate seed for each fruit and the low/high variation ends.
    private static readonly (string Prompt, string Name, int Seed, double Variation)[] Exports =
    [
        ("a red apple in a blue ceramic bowl on a wooden table", "apple-blue", 42, 0.35),
        ("a red apple in a cream ceramic bowl on a wooden table", "apple-cream", 42, 0.35),
        ("a green pear in a blue ceramic bowl on a wooden table", "pear-blue", 42, 0.35),
        ("a green pear in a cream ceramic bowl on a wooden table", "pear-cream", 42, 0.35),
        ("an orange in a blue ceramic bowl on a wooden table", "orange-blue", 42, 0.35),
        ("an orange in a cream ceramic bowl on a wooden table", "orange-cream", 42, 0.35),
        ("a yellow apple in a blue ceramic bowl on a wooden table", "apple-blue-altseed", 1337, 0.35),
        ("a yellow pear in a cream ceramic bowl on a wooden table", "pear-cream-altseed", 20261105, 0.35),
        ("a red apple in a blue ceramic bowl on a wooden table", "apple-blue-lowvariation", 42, 0),
        ("an orange in a cream ceramic bowl on a wooden table", "orange-cream-highvariation", 42, 1),
    ];

    public static TheoryData<string, string, int, double> ExportCases()
    {
        var data = new TheoryData<string, string, int, double>();
        foreach (var (prompt, name, seed, variation) in Exports)
        {
            data.Add(prompt, name, seed, variation);
        }

        return data;
    }

    public static TheoryData<string, string> Combinations()
    {
        var data = new TheoryData<string, string>();
        foreach (var fruit in Fruits)
        {
            foreach (var glaze in new[] { "blue", "cream" })
            {
                data.Add($"{fruit} in a {glaze} ceramic bowl on a wooden table", glaze);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Combinations))]
    public void EveryCombinationRendersDeterministicallyAtFullSize(string prompt, string glaze)
    {
        var scene = Scene(prompt);
        var image = StillLifeRenderer.Render(scene);
        Assert.Equal(1024, image.Width);
        Assert.Equal(1024, image.Height);
        Assert.EndsWith($"-in-{glaze}-bowl", image.Subject);
        Assert.Equal(scene.SubjectToken, image.Subject);
        Assert.Equal(image.PngBytes, StillLifeRenderer.Render(scene).PngBytes);

        using var bitmap = Decode(image.PngBytes);
        var opaque = 0;
        for (var y = 384; y < Canvas; y += 8)
        {
            for (var x = 0; x < Canvas; x += 8)
            {
                if (bitmap.GetPixel(x, y).Alpha == 255)
                {
                    opaque++;
                }
            }
        }

        Assert.True(opaque > 1000);
    }

    [Fact]
    public void NearWallOccludesTheSeatedPartOfTheContent()
    {
        var scene = Scene("a red apple in a blue ceramic bowl on a wooden table");
        var containment = scene.Containment!;
        using var rendered = Decode(StillLifeRenderer.Render(scene).PngBytes);
        using var bowlOnly = Decode(StillLifeRenderer.Render(BowlOnly(scene)).PngBytes);
        using var contentMask = Decode(StillLifeRenderer.RenderSilhouette(ContentOnly(scene)).PngBytes);

        var nearRimRow = (int)MathF.Floor(containment.FrontRimY * Canvas);
        var centerX = containment.OpeningCenter.X * Canvas;
        var radiusX = containment.OpeningRadius.X * Canvas;
        var visible = 0;
        var lowestVisible = 0;
        var highest = Canvas;
        var outsideOpeningWidth = 0;
        for (var y = 0; y < Canvas; y++)
        {
            for (var x = 0; x < Canvas; x++)
            {
                if (contentMask.GetPixel(x, y).Alpha == 0)
                {
                    continue;
                }

                highest = Math.Min(highest, y);
                if (Delta(rendered.GetPixel(x, y), bowlOnly.GetPixel(x, y)) > 24)
                {
                    visible++;
                    lowestVisible = Math.Max(lowestVisible, y);
                    if (MathF.Abs(x - centerX) > radiusX)
                    {
                        outsideOpeningWidth++;
                    }
                }
            }
        }

        // The visible content also stays inside the opening's horizontal span.
        Assert.Equal(0, outsideOpeningWidth);

        // Non-vacuous overlap: the content really is drawn, above the near wall...
        Assert.True(visible > 500, $"expected visible content pixels, saw {visible}");
        Assert.True(highest < nearRimRow - 40, "the content must rise above the near rim");

        // ...and the near wall really does cover the seated part.
        Assert.True(lowestVisible <= nearRimRow + 4, $"content leaked below the near rim at row {lowestVisible} (rim {nearRimRow})");
        Assert.True(lowestVisible < (containment.ContentBaseY * Canvas) - 20, "the base of the content must stay hidden");
    }

    [Fact]
    public void ContainmentChangesNothingOutsideTheContentAndTheCavity()
    {
        foreach (var prompt in new[]
        {
            "a red apple in a cream ceramic bowl on a wooden table",
            "a green pear in a blue ceramic bowl on a wooden table",
            "an orange in a cream ceramic bowl on a wooden table",
        })
        {
            var scene = Scene(prompt);
            var containment = scene.Containment!;
            using var rendered = Decode(StillLifeRenderer.Render(scene).PngBytes);
            using var bowlOnly = Decode(StillLifeRenderer.Render(BowlOnly(scene)).PngBytes);
            using var contentMask = Decode(StillLifeRenderer.RenderSilhouette(ContentOnly(scene)).PngBytes);
            var centerX = containment.OpeningCenter.X * Canvas;
            var centerY = containment.OpeningCenter.Y * Canvas;
            var radiusX = containment.OpeningRadius.X * Canvas;
            var radiusY = containment.OpeningRadius.Y * Canvas;
            var leaks = 0;
            for (var y = 0; y < Canvas; y++)
            {
                for (var x = 0; x < Canvas; x++)
                {
                    if (Delta(rendered.GetPixel(x, y), bowlOnly.GetPixel(x, y)) <= 6)
                    {
                        continue;
                    }

                    if (contentMask.GetPixel(x, y).Alpha > 0)
                    {
                        continue;
                    }

                    var dx = (x + 0.5f - centerX) / radiusX;
                    var dy = (y + 0.5f - centerY) / radiusY;
                    if ((dx * dx) + (dy * dy) > 1.15f)
                    {
                        leaks++;
                    }
                }
            }

            // Clipping the fruit instead of repainting the bowl keeps every pixel outside the
            // content and the cavity identical to a standalone bowl: no leaks, no halo, and no
            // shadow on the tabletop.
            Assert.Equal(0, leaks);
        }
    }

    [Fact]
    public void ContainedFruitCastsNoTabletopShadow()
    {
        var scene = Scene("an orange in a cream ceramic bowl on a wooden table");
        var contained = Decode(StillLifeRenderer.RenderShadowMask(scene).PngBytes);
        var bowlOnly = Decode(StillLifeRenderer.RenderShadowMask(BowlOnly(scene)).PngBytes);

        // The bowl still grounds itself on the table, and the contained fruit adds nothing.
        Assert.Equal(bowlOnly.PngBytes, contained.PngBytes);
        var grounded = 0;
        for (var y = 0; y < Canvas; y += 4)
        {
            for (var x = 0; x < Canvas; x += 4)
            {
                if (contained.GetPixel(x, y).Alpha > 0)
                {
                    grounded++;
                }
            }
        }

        Assert.True(grounded > 100, "the bowl must keep its own tabletop shadow");
    }

    [Fact]
    public void ContactShadowIsClippedToTheCavityAndStaysSubtle()
    {
        var scene = Scene("a red apple in a blue ceramic bowl on a wooden table");
        var containment = scene.Containment!;
        using var rendered = Decode(StillLifeRenderer.Render(scene).PngBytes);
        using var bowlOnly = Decode(StillLifeRenderer.Render(BowlOnly(scene)).PngBytes);
        using var contentMask = Decode(StillLifeRenderer.RenderSilhouette(ContentOnly(scene)).PngBytes);
        var centerX = containment.OpeningCenter.X * Canvas;
        var centerY = containment.OpeningCenter.Y * Canvas;
        var radiusX = containment.OpeningRadius.X * Canvas;
        var radiusY = containment.OpeningRadius.Y * Canvas;
        var shadowed = 0;
        var strongest = 0;
        for (var y = 0; y < Canvas; y++)
        {
            for (var x = 0; x < Canvas; x++)
            {
                if (contentMask.GetPixel(x, y).Alpha > 0)
                {
                    continue;
                }

                var dx = (x + 0.5f - centerX) / radiusX;
                var dy = (y + 0.5f - centerY) / radiusY;
                if ((dx * dx) + (dy * dy) > 0.98f)
                {
                    continue;
                }

                var before = bowlOnly.GetPixel(x, y);
                var after = rendered.GetPixel(x, y);
                var drop = (before.Red - after.Red) + (before.Green - after.Green) + (before.Blue - after.Blue);
                if (drop > 3)
                {
                    shadowed++;
                    strongest = Math.Max(strongest, drop);
                }
            }
        }

        // Visible, but subordinate: a soft darkening of the cavity around the seated base,
        // never a black blob.
        Assert.True(shadowed > 40, $"expected a visible contact shadow, saw {shadowed} pixels");
        Assert.InRange(strongest, 6, 120);
    }

    [Fact]
    public void SilhouetteShowsTheContentAboveTheRim()
    {
        var scene = Scene("a red apple in a blue ceramic bowl on a wooden table");
        var containment = scene.Containment!;
        using var silhouette = Decode(StillLifeRenderer.RenderSilhouette(scene).PngBytes);
        using var bowlOnly = Decode(StillLifeRenderer.RenderSilhouette(BowlOnly(scene)).PngBytes);

        // Documented convention: a bowl with contents silhouettes as one opaque shape. The
        // mouth is an opaque interior surface, so only the content above the rim is added.
        var added = 0;
        var aboveRim = (int)MathF.Floor(containment.RearRimY * Canvas);
        for (var y = 0; y < Canvas; y++)
        {
            for (var x = 0; x < Canvas; x++)
            {
                if (silhouette.GetPixel(x, y).Alpha > 0 && bowlOnly.GetPixel(x, y).Alpha == 0)
                {
                    added++;
                    Assert.True(y < aboveRim + 2, "only the part of the content above the rim may add to the silhouette");
                }
            }
        }

        Assert.True(added > 1000, $"expected the content in the silhouette, added {added} pixels");
    }

    [Fact]
    public void OmittedContentDetailKeepsTheLayering()
    {
        var scene = Scene("a green pear in a cream ceramic bowl on a wooden table");
        var containment = scene.Containment!;
        using var detailed = Decode(StillLifeRenderer.Render(scene).PngBytes);
        using var smooth = Decode(StillLifeRenderer.RenderWithoutInstanceDetail(scene, containment.Content.Depth).PngBytes);
        using var contentMask = Decode(StillLifeRenderer.RenderSilhouette(ContentOnly(scene)).PngBytes);

        var changed = 0;
        for (var y = 0; y < Canvas; y++)
        {
            for (var x = 0; x < Canvas; x++)
            {
                var before = detailed.GetPixel(x, y);
                var after = smooth.GetPixel(x, y);
                if (contentMask.GetPixel(x, y).Alpha == 0)
                {
                    Assert.Equal(before, after);
                }
                else if (before != after)
                {
                    changed++;
                }
            }
        }

        Assert.True(changed > 20);
    }

    [Theory]
    [MemberData(nameof(ExportCases))]
    public void ExportContainmentScenesTimingsAndSilhouettes(string prompt, string name, int seed, double variation)
    {
        var scene = Scene(prompt, (ulong)seed, variation);
        var timer = Stopwatch.StartNew();
        var rendered = StillLifeRenderer.Render(scene);
        timer.Stop();
        var stem = FormattableString.Invariant($"formloom-containment-{name}-seed-{seed}-variation-{variation:0.00}");
        var directory = AppContext.BaseDirectory;
        File.WriteAllBytes(Path.Combine(directory, stem + ".png"), rendered.PngBytes);
        File.WriteAllBytes(Path.Combine(directory, stem + "-silhouette.png"), StillLifeRenderer.RenderSilhouette(scene).PngBytes);
        var containment = scene.Containment!;
        var report = System.Text.Json.JsonSerializer.Serialize(
            new
            {
                Prompt = prompt,
                Subject = scene.SubjectToken,
                Containment = new
                {
                    Bowl = containment.Bowl.Kind.ToString(),
                    Glaze = containment.Bowl.Glaze.ToString(),
                    Content = containment.Content.Subject.ToString(),
                    containment.OccludedFraction,
                    containment.VisibleFraction,
                    containment.OpeningCoverage,
                    ContentSeed = containment.Content.Seed,
                },
                rendered.Width,
                rendered.Height,
                Framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                OperatingSystem = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                scene.Seed,
                scene.Variation,
                RenderAndEncodeMilliseconds = timer.Elapsed.TotalMilliseconds,
                ProcessorCount = Environment.ProcessorCount,
                Objects = scene.Objects,
            },
            new System.Text.Json.JsonSerializerOptions { IncludeFields = true });
        File.WriteAllText(Path.Combine(directory, stem + ".timing.json"), report);
        Assert.True(File.Exists(Path.Combine(directory, stem + ".png")));
    }

    [Fact]
    public void ExportContainmentReviewSheetAndManifest()
    {
        const int Cell = 320;
        const int Columns = 5;
        var rows = (int)Math.Ceiling(Exports.Length / (double)Columns);
        using var sheet = new SKBitmap(Cell * Columns, Cell * rows);
        using var canvas = new SKCanvas(sheet);
        canvas.Clear(new SKColor(20, 22, 24));
        using var typeface = SKTypeface.Default;
        using var font = typeface is null ? new SKFont() : new SKFont(typeface, 15);
        using var label = new SKPaint { Color = new SKColor(238, 236, 230), IsAntialias = true };
        using var strip = new SKPaint { Color = new SKColor(12, 14, 16, 190), IsAntialias = true };
        var manifest = new List<object>();
        for (var index = 0; index < Exports.Length; index++)
        {
            var (prompt, name, seed, variation) = Exports[index];
            var scene = Scene(prompt, (ulong)seed, variation);
            var timer = Stopwatch.StartNew();
            var rendered = StillLifeRenderer.Render(new(prompt, (ulong)seed, variation, Width: Cell, Height: Cell));
            timer.Stop();
            var left = (index % Columns) * Cell;
            var top = (index / Columns) * Cell;
            using (var cell = Decode(rendered.PngBytes))
            {
                canvas.DrawBitmap(
                    cell,
                    new SKRect(left, top, left + Cell, top + Cell),
                    new SKSamplingOptions(SKFilterMode.Linear));
            }

            canvas.DrawRect(new SKRect(left, top + Cell - 30, left + Cell, top + Cell), strip);
            canvas.DrawText(
                FormattableString.Invariant($"{name}  seed {seed}  v{variation:0.00}"),
                left + 8,
                top + Cell - 10,
                SKTextAlign.Left,
                font,
                label);
            manifest.Add(new
            {
                name,
                prompt,
                seed,
                variation,
                subject = scene.SubjectToken,
                cell = index,
                column = index % Columns,
                row = index / Columns,
                renderAndEncodeMilliseconds = timer.Elapsed.TotalMilliseconds,
            });
        }

        var directory = AppContext.BaseDirectory;
        using var image = SKImage.FromBitmap(sheet);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(Path.Combine(directory, "formloom-containment-sheet.png"), encoded.ToArray());
        File.WriteAllText(
            Path.Combine(directory, "formloom-containment-sheet.timing.json"),
            System.Text.Json.JsonSerializer.Serialize(
                new
                {
                    Sheet = "formloom-containment-sheet.png",
                    Cell,
                    Columns,
                    Rows = rows,
                    Generated = DateTimeOffset.UtcNow,
                    Cases = manifest,
                },
                new System.Text.Json.JsonSerializerOptions { IncludeFields = true }));
        Assert.True(File.Exists(Path.Combine(directory, "formloom-containment-sheet.png")));
        Assert.Equal(Exports.Length, manifest.Count);
    }

    [Fact]
    public void DiagnosticPassesDoNotChangeSubsequentContainmentRendering()
    {
        var scene = Scene("a red apple in a cream ceramic bowl on a wooden table");
        var original = StillLifeRenderer.Render(scene);
        using var shadows = Decode(StillLifeRenderer.RenderShadowMask(scene).PngBytes);
        using var changed = Decode(StillLifeRenderer.RenderWithoutInstanceDetail(scene, scene.Containment!.Bowl.Depth).PngBytes);
        Assert.NotNull(shadows);
        Assert.NotNull(changed);
        Assert.Equal(original.PngBytes, StillLifeRenderer.Render(scene).PngBytes);
    }

    private static StillLifeScene Scene(string prompt, ulong seed = 20261105, double variation = 0.35) =>
        SceneGenerator.Generate(new(prompt, Seed: seed, Variation: variation)).Scene!;

    // The same bowl with no content, used as the reference the containment render must match.
    private static StillLifeScene BowlOnly(StillLifeScene scene) => scene with
    {
        Entries = ImmutableArray.Create(SceneEntry.ForCeramic(scene.Containment!.Bowl)),
        Containment = null,
    };

    // Silhouette-only scene holding just the content, used as a pixel mask for the fruit.
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
        SKBitmap.Decode(png) ?? throw new InvalidOperationException("The renderer produced an undecodable PNG.");

    private static int Delta(SKColor first, SKColor second) => Math.Max(
        Math.Max(Math.Abs(first.Red - second.Red), Math.Abs(first.Green - second.Green)),
        Math.Max(Math.Abs(first.Blue - second.Blue), Math.Abs(first.Alpha - second.Alpha)));
}
