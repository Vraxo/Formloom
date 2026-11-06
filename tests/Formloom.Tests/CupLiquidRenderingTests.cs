using System.Diagnostics;
using Formloom.Core;
using Formloom.Rendering.Skia;
using SkiaSharp;
using Xunit;

namespace Formloom.Tests;

public sealed class CupLiquidRenderingTests
{
    private const int Canvas = 1024;

    // Default set plus an alternate seed per beverage and the low/high variation ends.
    private static readonly (string Prompt, string Name, int Seed, double Variation)[] Exports =
    [
        ("a blue ceramic cup filled with coffee on a wooden table", "coffee-blue", 42, 0.35),
        ("a cream ceramic cup filled with coffee on a wooden table", "coffee-cream", 42, 0.35),
        ("a blue ceramic cup filled with tea on a wooden table", "tea-blue", 42, 0.35),
        ("a cream ceramic cup filled with tea on a wooden table", "tea-cream", 42, 0.35),
        ("a cream ceramic cup filled with coffee on a wooden table", "coffee-cream-altseed", 1337, 0.35),
        ("a blue ceramic cup filled with tea on a wooden table", "tea-blue-altseed", 20261105, 0.35),
        ("a blue ceramic cup filled with coffee on a wooden table", "coffee-blue-lowvariation", 42, 0),
        ("a cream ceramic cup filled with tea on a wooden table", "tea-cream-highvariation", 42, 1),
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

    public static TheoryData<string, string, string> Combinations()
    {
        var data = new TheoryData<string, string, string>();
        foreach (var glaze in new[] { "blue", "cream" })
        {
            foreach (var beverage in new[] { "coffee", "tea" })
            {
                data.Add($"a {glaze} ceramic cup filled with {beverage} on a wooden table", glaze, beverage);
            }
        }

        return data;
    }

    public static TheoryData<int, int> Dimensions()
    {
        var data = new TheoryData<int, int>();
        foreach (var (width, height) in new[] { (256, 256), (640, 480), (2048, 1024) })
        {
            data.Add(width, height);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Combinations))]
    public void EveryCombinationRendersDeterministicallyAtFullSize(string prompt, string glaze, string beverage)
    {
        var scene = Scene(prompt);
        var image = StillLifeRenderer.Render(scene);
        Assert.Equal(Canvas, image.Width);
        Assert.Equal(Canvas, image.Height);
        Assert.Equal($"{beverage}-in-{glaze}-cup", image.Subject);
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

        Assert.True(opaque > 800, $"expected the cup to be drawn, saw {opaque} opaque samples");
    }

    [Theory]
    [MemberData(nameof(Dimensions))]
    public void FilledCupsRenderAtOtherSupportedDimensions(int width, int height)
    {
        var scene = SceneGenerator.Generate(new("a cream ceramic cup filled with tea on a wooden table", Width: width, Height: height)).Scene!;
        var image = StillLifeRenderer.Render(scene);
        Assert.Equal(width, image.Width);
        Assert.Equal(height, image.Height);
        Assert.Equal(image.PngBytes, StillLifeRenderer.Render(scene).PngBytes);

        using var filled = Decode(image.PngBytes);
        using var empty = Decode(StillLifeRenderer.Render(EmptyCup(scene)).PngBytes);
        var changed = 0;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (Delta(filled.GetPixel(x, y), empty.GetPixel(x, y)) > 8)
                {
                    changed++;
                }
            }
        }

        // The drink is drawn at every supported size: it is not a 1024-only pixel offset.
        Assert.True(changed > 10, $"expected liquid pixels at {width}x{height}, saw {changed}");
    }

    [Theory]
    [MemberData(nameof(Combinations))]
    public void LiquidIsDrawnOnlyInsideTheCupMouth(string prompt, string glaze, string beverage)
    {
        var scene = Scene(prompt);
        Assert.NotNull(scene.CupContents);
        var geometry = Geometry(scene);
        using var filled = Decode(StillLifeRenderer.Render(scene).PngBytes);
        using var empty = Decode(StillLifeRenderer.Render(EmptyCup(scene)).PngBytes);

        var changed = 0;
        var outsideMouth = 0;
        var outsideClip = 0;
        var belowHorizon = 0;
        var horizon = (int)MathF.Floor((float)scene.TableHorizon * Canvas);
        for (var y = 0; y < Canvas; y++)
        {
            for (var x = 0; x < Canvas; x++)
            {
                if (Delta(filled.GetPixel(x, y), empty.GetPixel(x, y)) == 0)
                {
                    continue;
                }

                changed++;
                if (y > horizon)
                {
                    belowHorizon++;
                }

                if (Normalized(x, y, geometry.Center, geometry.Radius) > 1.04f)
                {
                    outsideMouth++;
                    continue;
                }

                if (Normalized(x, y, geometry.ClipCenter, geometry.ClipRadius) > 1.06f)
                {
                    outsideClip++;
                }
            }
        }

        // Non-vacuous: the drink really is painted, and it is confined to the mouth.
        Assert.True(changed > 700, $"{prompt}: expected liquid pixels, saw {changed}");
        Assert.Equal(0, outsideMouth);
        Assert.Equal(0, belowHorizon);

        // The ceramic rim and lip keep their own pixels: the drink never covers them.
        Assert.Equal(0, outsideClip);

        // The surface is visible over a real area, not a couple of stray pixels.
        Assert.True(changed > (geometry.ClipRadius.X * geometry.ClipRadius.Y * MathF.PI * 0.15f), "the drink must fill a substantial part of the opening");
    }

    [Fact]
    public void CoffeeAndTeaAreVisiblyDistinct()
    {
        var coffee = Scene("a blue ceramic cup filled with coffee on a wooden table");
        var tea = Scene("a cream ceramic cup filled with tea on a wooden table");
        var geometry = Geometry(coffee);
        using var coffeePixels = Decode(StillLifeRenderer.Render(coffee).PngBytes);
        using var teaPixels = Decode(StillLifeRenderer.Render(tea).PngBytes);

        var total = 0f;
        var samples = 0;
        for (var y = 0; y < Canvas; y++)
        {
            for (var x = 0; x < Canvas; x++)
            {
                if (Normalized(x, y, geometry.ClipCenter, geometry.ClipRadius) > 0.7f)
                {
                    continue;
                }

                total += Delta(coffeePixels.GetPixel(x, y), teaPixels.GetPixel(x, y));
                samples++;
            }
        }

        Assert.True(samples > 1000);
        var mean = total / samples;
        Assert.True(mean > 25, $"coffee and tea must read as different drinks, mean delta was {mean:0.0}");

        // Coffee is the darker drink: its surface averages lower luminance than tea's.
        Assert.True(MeanLuminance(coffeePixels, geometry) < MeanLuminance(teaPixels, geometry) - 0.05f, "coffee must stay darker than tea");
    }

    [Fact]
    public void GlazeDoesNotRecolorTheBeverage()
    {
        var blue = Scene("a blue ceramic cup filled with coffee on a wooden table");
        var cream = Scene("a cream ceramic cup filled with coffee on a wooden table");
        var geometry = Geometry(blue);
        using var bluePixels = Decode(StillLifeRenderer.Render(blue).PngBytes);
        using var creamPixels = Decode(StillLifeRenderer.Render(cream).PngBytes);
        using var blueEmpty = Decode(StillLifeRenderer.Render(EmptyCup(blue)).PngBytes);
        using var creamEmpty = Decode(StillLifeRenderer.Render(EmptyCup(cream)).PngBytes);

        var liquidDifferences = 0;
        var ceramicDifferences = 0;
        for (var y = 0; y < Canvas; y++)
        {
            for (var x = 0; x < Canvas; x++)
            {
                var distance = Normalized(x, y, geometry.ClipCenter, geometry.ClipRadius);
                if (distance < 0.75f && Delta(bluePixels.GetPixel(x, y), creamPixels.GetPixel(x, y)) > 0)
                {
                    liquidDifferences++;
                }

                if (distance > 1.2f && Delta(blueEmpty.GetPixel(x, y), creamEmpty.GetPixel(x, y)) > 12)
                {
                    ceramicDifferences++;
                }
            }
        }

        // Same beverage, same geometry, same pixels: the glaze cannot tint the drink...
        Assert.Equal(0, liquidDifferences);

        // ...while the two glazes stay plainly different ceramics.
        Assert.True(ceramicDifferences > 200, $"expected distinct glazes, saw {ceramicDifferences} differing pixels");
    }

    [Fact]
    public void FilledCupKeepsTheEmptyCupSilhouetteAndHandleHole()
    {
        foreach (var prompt in new[]
        {
            "a blue ceramic cup filled with coffee on a wooden table",
            "a cream ceramic cup filled with tea on a wooden table",
        })
        {
            var scene = Scene(prompt);
            var silhouette = StillLifeRenderer.RenderSilhouette(scene);
            var empty = StillLifeRenderer.RenderSilhouette(EmptyCup(scene));
            Assert.Equal(empty.PngBytes, silhouette.PngBytes);

            var geometry = Geometry(scene);
            using var bitmap = Decode(silhouette.PngBytes);
            var hole = 0;
            foreach (var (x, y) in HandleSamples(geometry, 0.5f))
            {
                if (bitmap.GetPixel(x, y).Alpha < 32)
                {
                    hole++;
                }
            }

            Assert.True(hole > 20, "the handle hole must stay transparent");
        }
    }

    [Fact]
    public void FilledCupCastsTheSameTabletopShadow()
    {
        var scene = Scene("a cream ceramic cup filled with coffee on a wooden table");
        var filled = StillLifeRenderer.RenderShadowMask(scene);
        var empty = StillLifeRenderer.RenderShadowMask(EmptyCup(scene));

        // Only the cup touches the table, so the drink can add no shadow of its own.
        Assert.Equal(empty.PngBytes, filled.PngBytes);
        using var mask = Decode(filled.PngBytes);
        var grounded = 0;
        for (var y = 0; y < Canvas; y += 4)
        {
            for (var x = 0; x < Canvas; x += 4)
            {
                if (mask.GetPixel(x, y).Alpha > 0)
                {
                    grounded++;
                }
            }
        }

        Assert.True(grounded > 100, "the cup must keep its own tabletop shadow");
    }

    [Fact]
    public void DetailOmissionKeepsTheDrinkButDropsItsAccents()
    {
        var scene = Scene("a cream ceramic cup filled with coffee on a wooden table");
        var cup = scene.Ceramics[0];
        var geometry = Geometry(scene);
        var full = StillLifeRenderer.Render(scene);
        var omitted = StillLifeRenderer.RenderWithoutInstanceDetail(scene, cup.Depth);
        var omittedEmpty = StillLifeRenderer.RenderWithoutInstanceDetail(EmptyCup(scene), cup.Depth);
        using var fullPixels = Decode(full.PngBytes);
        using var omittedPixels = Decode(omitted.PngBytes);
        using var emptyPixels = Decode(omittedEmpty.PngBytes);

        var stillDrawn = 0;
        var accents = 0;
        var escapedTheMouth = 0;
        for (var y = 0; y < Canvas; y++)
        {
            for (var x = 0; x < Canvas; x++)
            {
                var distance = Normalized(x, y, geometry.ClipCenter, geometry.ClipRadius);
                if (distance < 0.9f && Delta(omittedPixels.GetPixel(x, y), emptyPixels.GetPixel(x, y)) > 8)
                {
                    stillDrawn++;
                }

                if (distance < 0.9f && Delta(fullPixels.GetPixel(x, y), omittedPixels.GetPixel(x, y)) > 4)
                {
                    accents++;
                }

                if (distance > 1.06f && Delta(omittedPixels.GetPixel(x, y), emptyPixels.GetPixel(x, y)) > 0)
                {
                    escapedTheMouth++;
                }
            }
        }

        // The drink keeps its basic identity when accents are omitted...
        Assert.True(stillDrawn > 500, $"the drink must still be drawn, saw {stillDrawn}");
        Assert.True(accents > 100, "the reflections are the accents that detail omission removes");

        // ...and omitting detail moves no pixel outside the mouth.
        Assert.Equal(0, escapedTheMouth);
    }

    [Fact]
    public void DiagnosticPassesDoNotChangeSubsequentLiquidRendering()
    {
        var scene = Scene("a blue ceramic cup filled with tea on a wooden table");
        var original = StillLifeRenderer.Render(scene);
        using var shadows = Decode(StillLifeRenderer.RenderShadowMask(scene).PngBytes);
        using var changed = Decode(StillLifeRenderer.RenderWithoutInstanceDetail(scene, scene.Ceramics[0].Depth).PngBytes);
        using var silhouette = Decode(StillLifeRenderer.RenderSilhouette(scene).PngBytes);
        Assert.NotNull(shadows);
        Assert.NotNull(changed);
        Assert.NotNull(silhouette);
        Assert.Equal(original.PngBytes, StillLifeRenderer.Render(scene).PngBytes);
    }

    [Fact]
    public void BeverageIdentitySurvivesSeeds()
    {
        foreach (var beverage in new[] { "coffee", "tea" })
        {
            foreach (var seed in new ulong[] { 42, 1337, 20261105 })
            {
                var prompt = $"a cream ceramic cup filled with {beverage} on a wooden table";
                var scene = Scene(prompt, seed);
                var image = StillLifeRenderer.Render(scene);
                Assert.Equal($"{beverage}-in-cream-cup", image.Subject);

                var geometry = Geometry(scene);
                using var filled = Decode(image.PngBytes);
                using var empty = Decode(StillLifeRenderer.Render(EmptyCup(scene)).PngBytes);
                var changed = 0;
                for (var y = 0; y < Canvas; y++)
                {
                    for (var x = 0; x < Canvas; x++)
                    {
                        if (Normalized(x, y, geometry.ClipCenter, geometry.ClipRadius) < 0.9f &&
                            Delta(filled.GetPixel(x, y), empty.GetPixel(x, y)) > 8)
                        {
                            changed++;
                        }
                    }
                }

                Assert.True(changed > 500, $"{prompt} at seed {seed} lost its drink");
            }
        }
    }

    [Theory]
    [MemberData(nameof(ExportCases))]
    public void ExportFilledCupsTimingsAndSilhouettes(string prompt, string name, int seed, double variation)
    {
        var scene = Scene(prompt, (ulong)seed, variation);
        var empty = EmptyCup(scene);
        var timer = Stopwatch.StartNew();
        var rendered = StillLifeRenderer.Render(scene);
        timer.Stop();
        var emptyTimer = Stopwatch.StartNew();
        var emptyRendered = StillLifeRenderer.Render(empty);
        emptyTimer.Stop();

        var stem = FormattableString.Invariant($"formloom-cupliquid-{name}-seed-{seed}-variation-{variation:0.00}");
        var directory = AppContext.BaseDirectory;
        File.WriteAllBytes(Path.Combine(directory, stem + ".png"), rendered.PngBytes);
        var silhouette = StillLifeRenderer.RenderSilhouette(scene);
        File.WriteAllBytes(Path.Combine(directory, stem + "-silhouette.png"), silhouette.PngBytes);

        // The filled cup silhouettes exactly as the same empty cup.
        Assert.Equal(File.ReadAllBytes(Path.Combine(directory, stem + "-silhouette.png")), StillLifeRenderer.RenderSilhouette(empty).PngBytes);

        var contents = scene.CupContents!;
        var report = System.Text.Json.JsonSerializer.Serialize(
            new
            {
                Prompt = prompt,
                Subject = scene.SubjectToken,
                CupContents = new
                {
                    contents.Cup.Kind,
                    contents.Cup.Glaze,
                    contents.Beverage,
                    Surface = new
                    {
                        contents.Surface.Center,
                        contents.Surface.Radius,
                        contents.Surface.ClipCenter,
                        contents.Surface.ClipRadius,
                    },
                },
                rendered.Width,
                rendered.Height,
                Framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                OperatingSystem = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                scene.Seed,
                scene.Variation,
                RenderAndEncodeMilliseconds = timer.Elapsed.TotalMilliseconds,
                EmptyCupRenderAndEncodeMilliseconds = emptyTimer.Elapsed.TotalMilliseconds,
                AdditionalMillisecondsOverEmptyCup = timer.Elapsed.TotalMilliseconds - emptyTimer.Elapsed.TotalMilliseconds,
                ProcessorCount = Environment.ProcessorCount,
                Objects = scene.Objects,
                EmptyCupBytes = emptyRendered.PngBytes.Length,
            },
            new System.Text.Json.JsonSerializerOptions { IncludeFields = true });
        File.WriteAllText(Path.Combine(directory, stem + ".timing.json"), report);
        Assert.True(File.Exists(Path.Combine(directory, stem + ".png")));
    }

    [Fact]
    public void ExportCupLiquidReviewSheetAndManifest()
    {
        const int Cell = 320;
        const int Columns = 4;
        var rows = (int)Math.Ceiling(Exports.Length / (double)Columns);
        using var sheet = new SKBitmap(Cell * Columns, Cell * rows);
        using var canvas = new SKCanvas(sheet);
        canvas.Clear(new SKColor(20, 22, 24));

        // No text: the CI image has no font stack, so the sheet is a plain row-major grid and
        // the sidecar manifest is the authoritative key for every cell.
        var manifest = new List<object>();
        for (var index = 0; index < Exports.Length; index++)
        {
            var (prompt, name, seed, variation) = Exports[index];
            var scene = SceneGenerator.Generate(new(prompt, (ulong)seed, variation, Width: Cell, Height: Cell)).Scene!;
            var timer = Stopwatch.StartNew();
            var rendered = StillLifeRenderer.Render(scene);
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

            manifest.Add(new
            {
                name,
                prompt,
                seed,
                variation,
                subject = scene.SubjectToken,
                beverage = scene.CupContents!.Beverage.ToString(),
                glaze = scene.CupContents.Cup.Glaze.ToString(),
                fileName = FormattableString.Invariant($"formloom-cupliquid-{name}-seed-{seed}-variation-{variation:0.00}.png"),
                cell = index,
                column = index % Columns,
                row = index / Columns,
                width = rendered.Width,
                height = rendered.Height,
                renderAndEncodeMilliseconds = timer.Elapsed.TotalMilliseconds,
            });
        }

        var directory = AppContext.BaseDirectory;
        using var image = SKImage.FromBitmap(sheet);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(Path.Combine(directory, "formloom-cupliquid-sheet.png"), encoded.ToArray());
        File.WriteAllText(
            Path.Combine(directory, "formloom-cupliquid-sheet.timing.json"),
            System.Text.Json.JsonSerializer.Serialize(
                new
                {
                    Sheet = "formloom-cupliquid-sheet.png",
                    Cell,
                    Columns,
                    Rows = rows,
                    Generated = DateTimeOffset.UtcNow,
                    Layout = "row-major, left to right, top to bottom",
                    Cases = manifest,
                },
                new System.Text.Json.JsonSerializerOptions { IncludeFields = true }));
        Assert.True(File.Exists(Path.Combine(directory, "formloom-cupliquid-sheet.png")));
        Assert.Equal(Exports.Length, manifest.Count);
    }

    private static StillLifeScene Scene(string prompt, ulong seed = 20261105, double variation = 0.35) =>
        SceneGenerator.Generate(new(prompt, Seed: seed, Variation: variation)).Scene!;

    // The same cup with no drink: the reference a filled render must match everywhere except
    // inside the mouth.
    private static StillLifeScene EmptyCup(StillLifeScene scene) => scene with { CupContents = null };

    // Canvas-space mouth, clip and (for the handle hole) inner-handle ellipses.
    private readonly record struct MouthGeometry(
        System.Numerics.Vector2 Center,
        System.Numerics.Vector2 Radius,
        System.Numerics.Vector2 ClipCenter,
        System.Numerics.Vector2 ClipRadius,
        System.Numerics.Vector2 HandleCenter,
        System.Numerics.Vector2 HandleRadius);

    private static MouthGeometry Geometry(StillLifeScene scene)
    {
        var cup = scene.CupContents!.Cup;
        var profile = CeramicForms.For(cup.Kind);
        var surface = CupLiquidSurface.For(profile);
        var width = cup.Body.Size.X * Canvas;
        var height = cup.Body.Size.Y * Canvas;
        var left = (cup.Body.Center.X * Canvas) - (width / 2);
        var top = (cup.Body.Center.Y - (cup.Body.Size.Y / 2)) * Canvas;
        var handle = profile.Handle!;
        return new(
            new(left + (profile.MouthCenter.X * width), top + (profile.MouthCenter.Y * height)),
            new(profile.MouthRadius.X * width, profile.MouthRadius.Y * height),
            new(left + (surface.ClipCenter.X * width), top + (surface.ClipCenter.Y * height)),
            new(surface.ClipRadius.X * width, surface.ClipRadius.Y * height),
            new(left + (handle.Center.X * width), top + (handle.Center.Y * height)),
            new(handle.InnerRadius.X * width, handle.InnerRadius.Y * height));
    }

    private static float Normalized(int x, int y, System.Numerics.Vector2 center, System.Numerics.Vector2 radius)
    {
        var dx = (x + 0.5f - center.X) / radius.X;
        var dy = (y + 0.5f - center.Y) / radius.Y;
        return (dx * dx) + (dy * dy);
    }

    private static IEnumerable<(int X, int Y)> HandleSamples(MouthGeometry geometry, float scale)
    {
        for (var y = -6; y <= 6; y++)
        {
            for (var x = -6; x <= 6; x++)
            {
                if (((x * x) + (y * y)) / 36f > 1f)
                {
                    continue;
                }

                yield return (
                    (int)MathF.Round(geometry.HandleCenter.X + (geometry.HandleRadius.X * scale * x / 6f)),
                    (int)MathF.Round(geometry.HandleCenter.Y + (geometry.HandleRadius.Y * scale * y / 6f)));
            }
        }
    }

    private static float MeanLuminance(SKBitmap bitmap, MouthGeometry geometry)
    {
        var total = 0f;
        var count = 0;
        for (var y = 0; y < Canvas; y += 2)
        {
            for (var x = 0; x < Canvas; x += 2)
            {
                if (Normalized(x, y, geometry.ClipCenter, geometry.ClipRadius) > 0.6f)
                {
                    continue;
                }

                var pixel = bitmap.GetPixel(x, y);
                total += (0.299f * pixel.Red) + (0.587f * pixel.Green) + (0.114f * pixel.Blue);
                count++;
            }
        }

        return count == 0 ? 0 : total / count / 255f;
    }

    private static SKBitmap Decode(byte[] png) =>
        SKBitmap.Decode(png) ?? throw new InvalidOperationException("The renderer produced an undecodable PNG.");

    private static int Delta(SKColor first, SKColor second) => Math.Max(
        Math.Max(Math.Abs(first.Red - second.Red), Math.Abs(first.Green - second.Green)),
        Math.Max(Math.Abs(first.Blue - second.Blue), Math.Abs(first.Alpha - second.Alpha)));
}
