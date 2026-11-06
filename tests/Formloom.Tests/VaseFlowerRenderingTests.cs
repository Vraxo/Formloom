using System.Diagnostics;
using Formloom.Core;
using Formloom.Rendering.Skia;
using SkiaSharp;
using Xunit;

namespace Formloom.Tests;

public sealed class VaseFlowerRenderingTests
{
    // Everything in the rim band the stem passes behind, in pixels at the 1024 reference size.
    private const int RimBand = 14;

    private const int Canvas = 1024;

    // Default set plus an alternate seed per colour and the low/high variation ends.
    private static readonly (string Prompt, string Name, int Seed, double Variation)[] Exports =
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

    public static TheoryData<string, string, string, string, int, double> ExportCases()
    {
        var data = new TheoryData<string, string, string, string, int, double>();
        foreach (var (prompt, name, seed, variation) in Exports)
        {
            var parts = name.Split('-');
            data.Add(prompt, name, parts[0], parts[1], seed, variation);
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
    [MemberData(nameof(ExportCases))]
    public void EveryCombinationRendersAndKeepsTheVaseIntact(string prompt, string name, string color, string glaze, int seed, double variation)
    {
        var scene = Scene(prompt, (ulong)seed, variation);
        var flower = scene.VaseFlower!;
        var plant = flower.Plant;
        var image = StillLifeRenderer.Render(scene);
        Assert.Equal(Canvas, image.Width);
        Assert.Equal(Canvas, image.Height);
        Assert.Equal($"{color}-tulip-in-{glaze}-vase", image.Subject);
        Assert.Equal(image.PngBytes, StillLifeRenderer.Render(scene).PngBytes);

        using var planted = Decode(image.PngBytes);
        using var empty = Decode(StillLifeRenderer.Render(EmptyVase(scene)).PngBytes);
        var rim = (int)MathF.Ceiling(flower.MouthTop * Canvas);
        var changed = 0;
        var belowTheRim = 0;
        for (var y = 0; y < Canvas; y++)
        {
            for (var x = 0; x < Canvas; x++)
            {
                if (Delta(planted.GetPixel(x, y), empty.GetPixel(x, y)) == 0)
                {
                    continue;
                }

                changed++;
                if (y > rim + RimBand)
                {
                    belowTheRim++;
                }
            }
        }

        // The flower is drawn, and the vase, the table and the background below the rim band
        // are byte-identical to the same vase without a flower.
        Assert.True(changed > 400, $"{name} drew almost nothing: {changed} changed pixels");
        Assert.Equal(0, belowTheRim);

        // The bloom fills its box, the leaves flank the stem and the stem reaches the opening.
        Assert.True(Count(planted, empty, BloomWindow(plant, 0)) > 1500, $"{name} lost its bloom");
        foreach (var leaf in plant.Leaves)
        {
            Assert.True(Count(planted, empty, LeafWindow(leaf)) > 20, $"{name} lost a leaf");
        }

        var stem = StemWindow(flower);
        Assert.True(Count(planted, empty, stem) > 40, $"{name} lost the stem above the rim");

        // The bloom reads as a red or yellow flower rather than an unlit patch: count the
        // pixels that actually carry the petal colour inside the bloom box.
        var warm = color == "red"
            ? Coloured(planted, BloomWindow(plant, 0), pixel => pixel.Red > pixel.Green + 45)
            : Coloured(planted, BloomWindow(plant, 0), pixel => pixel.Red > 150 && pixel.Green > 110 && pixel.Blue < 90);
        Assert.True(warm > 700, $"{name} does not read as {color}: {warm} petal pixels");
    }

    [Theory]
    [MemberData(nameof(Dimensions))]
    public void FlowersRenderAtOtherSupportedDimensions(int width, int height)
    {
        var scene = SceneGenerator.Generate(new("a yellow tulip in a cream ceramic vase on a wooden table", Width: width, Height: height)).Scene!;
        var image = StillLifeRenderer.Render(scene);
        Assert.Equal(width, image.Width);
        Assert.Equal(height, image.Height);
        Assert.Equal(image.PngBytes, StillLifeRenderer.Render(scene).PngBytes);

        using var planted = Decode(image.PngBytes);
        using var empty = Decode(StillLifeRenderer.Render(EmptyVase(scene)).PngBytes);
        var changed = 0;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (Delta(planted.GetPixel(x, y), empty.GetPixel(x, y)) > 0)
                {
                    changed++;
                }
            }
        }

        // The flower is drawn at every supported size: it is not a 1024-only pixel offset.
        Assert.True(changed > 10, $"expected flower pixels at {width}x{height}, saw {changed}");
    }

    [Fact]
    public void RedAndYellowDifferOnlyInsideTheBloom()
    {
        var red = Scene("a red tulip in a cream ceramic vase on a wooden table");
        var yellow = Scene("a yellow tulip in a cream ceramic vase on a wooden table");
        using var redPixels = Decode(StillLifeRenderer.Render(red).PngBytes);
        using var yellowPixels = Decode(StillLifeRenderer.Render(yellow).PngBytes);
        var box = BloomWindow(red.VaseFlower!.Plant, 20, Canvas);
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

                inside++;
                if (!Inside(box, x, y))
                {
                    outside++;
                }
            }
        }

        // Petals carry the colour; stem and leaves keep the same green material, so the two
        // renders are identical everywhere the petals are not.
        Assert.True(inside > 500, $"expected petals to differ, saw {inside} pixels");
        Assert.Equal(0, outside);
    }

    [Fact]
    public void VaseGlazeNeverRecoloursTheFlower()
    {
        var blue = Scene("a red tulip in a blue ceramic vase on a wooden table", 42, 0.5);
        var cream = Scene("a red tulip in a cream ceramic vase on a wooden table", 42, 0.5);
        using var bluePixels = Decode(StillLifeRenderer.Render(blue).PngBytes);
        using var creamPixels = Decode(StillLifeRenderer.Render(cream).PngBytes);
        var rim = (int)MathF.Floor(blue.VaseFlower!.MouthTop * Canvas) - 4;
        var differences = 0;
        for (var y = 0; y < rim; y++)
        {
            for (var x = 0; x < Canvas; x++)
            {
                if (Delta(bluePixels.GetPixel(x, y), creamPixels.GetPixel(x, y)) != 0)
                {
                    differences++;
                }
            }
        }

        // Everything above the vase is the background plus the flower: the glaze must not
        // reach either of them.
        Assert.Equal(0, differences);
    }

    [Fact]
    public void ShadowMaskIsUnchangedAndTheSilhouetteGainsTheBloom()
    {
        var scene = Scene("a yellow tulip in a blue ceramic vase on a wooden table");
        var flower = scene.VaseFlower!;

        // The flower casts no tabletop shadow and does not touch the vase's own shadow.
        Assert.Equal(
            StillLifeRenderer.RenderShadowMask(EmptyVase(scene)).PngBytes,
            StillLifeRenderer.RenderShadowMask(scene).PngBytes);

        using var empty = Decode(StillLifeRenderer.RenderSilhouette(EmptyVase(scene)).PngBytes);
        using var planted = Decode(StillLifeRenderer.RenderSilhouette(scene).PngBytes);
        var rim = (int)MathF.Ceiling(flower.MouthTop * Canvas) + RimBand;
        var added = 0;
        var lost = 0;
        var addedBelowTheRim = 0;
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
                        addedBelowTheRim++;
                    }
                }
            }
        }

        Assert.Equal(0, lost);
        Assert.Equal(0, addedBelowTheRim);
        Assert.True(added > 800, $"expected the bloom in the silhouette, saw {added} pixels");
    }

    [Fact]
    public void DetailOmissionAndDiagnosticPassesStayInsideTheFlower()
    {
        var scene = Scene("a red tulip in a blue ceramic vase on a wooden table", 1337, 0.6);
        var flower = scene.VaseFlower!;
        var bounds = BoundsWindow(flower.Plant, 3);
        using var full = Decode(StillLifeRenderer.Render(scene).PngBytes);
        using var plain = Decode(StillLifeRenderer.RenderWithoutInstanceDetail(scene, 0).PngBytes);

        // Above the rim band only the flower and the background exist, so this region isolates
        // the flower's own detail from the vase mottle the same pass also drops.
        var rim = (int)MathF.Ceiling(flower.MouthTop * Canvas) - RimBand;
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
                if (!Inside(bounds, x, y))
                {
                    outside++;
                }
            }
        }

        Assert.True(changed > 100, $"expected vein and petal detail to be omitted, saw {changed} pixels");
        Assert.Equal(0, outside);

        // Diagnostic passes must not contaminate the next normal render.
        var original = StillLifeRenderer.Render(scene);
        using var shadows = Decode(StillLifeRenderer.RenderShadowMask(scene).PngBytes);
        using var change = Decode(StillLifeRenderer.RenderWithoutInstanceDetail(scene, 0).PngBytes);
        using var silhouette = Decode(StillLifeRenderer.RenderSilhouette(scene).PngBytes);
        Assert.NotNull(shadows);
        Assert.NotNull(change);
        Assert.NotNull(silhouette);
        Assert.Equal(original.PngBytes, StillLifeRenderer.Render(scene).PngBytes);
    }

    [Theory]
    [MemberData(nameof(ExportCases))]
    public void ExportVaseFlowersTimingsAndSilhouettes(string prompt, string name, string color, string glaze, int seed, double variation)
    {
        var scene = Scene(prompt, (ulong)seed, variation);
        Assert.Equal($"{color}-tulip-in-{glaze}-vase", scene.SubjectToken);
        var empty = EmptyVase(scene);
        var timer = Stopwatch.StartNew();
        var rendered = StillLifeRenderer.Render(scene);
        timer.Stop();
        var emptyTimer = Stopwatch.StartNew();
        var emptyRendered = StillLifeRenderer.Render(empty);
        emptyTimer.Stop();

        var stem = FormattableString.Invariant($"formloom-vaseflower-{name}-seed-{seed}-variation-{variation:0.00}");
        var directory = AppContext.BaseDirectory;
        File.WriteAllBytes(Path.Combine(directory, stem + ".png"), rendered.PngBytes);
        var silhouette = StillLifeRenderer.RenderSilhouette(scene);
        File.WriteAllBytes(Path.Combine(directory, stem + "-silhouette.png"), silhouette.PngBytes);
        Assert.NotEqual(silhouette.PngBytes, StillLifeRenderer.RenderSilhouette(empty).PngBytes);

        var live = scene.VaseFlower!;
        var report = System.Text.Json.JsonSerializer.Serialize(
            new
            {
                Prompt = prompt,
                Subject = scene.SubjectToken,
                VaseFlower = new
                {
                    Flower = live.Flower,
                    Tulip = live.Color,
                    live.Vase.Kind,
                    live.Vase.Glaze,
                    Opening = new { live.OpeningCenter, live.OpeningRadius },
                    MouthTop = live.MouthTop,
                    live.Plant.StemBase,
                    live.Plant.StemTop,
                    live.Plant.StemThickness,
                    Bloom = new { live.Plant.Bloom.Base, Width = live.Plant.Bloom.Width, Height = live.Plant.Bloom.Height, live.Plant.Bloom.Top },
                    Leaves = live.Plant.Leaves.Length,
                    Petals = live.Plant.Bloom.Petals.Length,
                    Bounds = live.Plant.Bounds,
                },
                rendered.Width,
                rendered.Height,
                Framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                OperatingSystem = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                scene.Seed,
                scene.Variation,
                RenderAndEncodeMilliseconds = timer.Elapsed.TotalMilliseconds,
                EmptyVaseRenderAndEncodeMilliseconds = emptyTimer.Elapsed.TotalMilliseconds,
                AdditionalMillisecondsOverEmptyVase = timer.Elapsed.TotalMilliseconds - emptyTimer.Elapsed.TotalMilliseconds,
                ProcessorCount = Environment.ProcessorCount,
                Objects = scene.Objects,
                EmptyVaseBytes = emptyRendered.PngBytes.Length,
            },
            new System.Text.Json.JsonSerializerOptions { IncludeFields = true });
        File.WriteAllText(Path.Combine(directory, stem + ".timing.json"), report);
        Assert.True(File.Exists(Path.Combine(directory, stem + ".png")));
    }

    [Fact]
    public void ExportVaseFlowerReviewSheetAndManifest()
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

            var flower = scene.VaseFlower!;
            manifest.Add(new
            {
                name,
                prompt,
                seed,
                variation,
                subject = scene.SubjectToken,
                flower = flower.Flower.ToString(),
                tulip = flower.Color.ToString(),
                glaze = flower.Vase.Glaze.ToString(),
                fileName = FormattableString.Invariant($"formloom-vaseflower-{name}-seed-{seed}-variation-{variation:0.00}.png"),
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
        File.WriteAllBytes(Path.Combine(directory, "formloom-vaseflower-sheet.png"), encoded.ToArray());
        File.WriteAllText(
            Path.Combine(directory, "formloom-vaseflower-sheet.timing.json"),
            System.Text.Json.JsonSerializer.Serialize(
                new
                {
                    Sheet = "formloom-vaseflower-sheet.png",
                    Cell,
                    Columns,
                    Rows = rows,
                    Generated = DateTimeOffset.UtcNow,
                    Layout = "row-major, left to right, top to bottom",
                    Cases = manifest,
                },
                new System.Text.Json.JsonSerializerOptions { IncludeFields = true }));
        Assert.True(File.Exists(Path.Combine(directory, "formloom-vaseflower-sheet.png")));
        Assert.Equal(Exports.Length, manifest.Count);
    }

    private static StillLifeScene Scene(string prompt, ulong seed = 20261105, double variation = 0.35) =>
        SceneGenerator.Generate(new(prompt, seed, variation)).Scene!;

    // The same vase with no flower: the reference a planted render must match everywhere
    // except the flower and the rim band the stem passes behind.
    private static StillLifeScene EmptyVase(StillLifeScene scene) => scene with { VaseFlower = null };

    private readonly record struct Window(float Left, float Top, float Right, float Bottom);

    private static Window BloomWindow(TulipPlant plant, int grow, int canvas = Canvas)
    {
        var bloom = plant.Bloom;
        return new(
            ((bloom.Left * canvas) - grow) / canvas,
            ((bloom.Top * canvas) - grow) / canvas,
            ((bloom.Right * canvas) + grow) / canvas,
            ((bloom.Base.Y * canvas) + grow) / canvas);
    }

    private static Window LeafWindow(TulipLeaf leaf)
    {
        // A small box at the middle of the blade, well away from the stem and the bloom.
        var middle = (leaf.Attachment + leaf.Tip) / 2;
        const float Reach = 0.012f;
        return new(middle.X - Reach, middle.Y - Reach, middle.X + Reach, middle.Y + Reach);
    }

    private static Window StemWindow(VaseFlower flower)
    {
        var basePoint = flower.Plant.StemBase;
        return new(basePoint.X - 0.018f, flower.MouthTop - 0.034f, basePoint.X + 0.018f, flower.MouthTop - 0.014f);
    }

    private static Window BoundsWindow(TulipPlant plant, int grow)
    {
        var bounds = plant.Bounds;
        return new(
            ((bounds.Left * Canvas) - grow) / Canvas,
            ((bounds.Top * Canvas) - grow) / Canvas,
            ((bounds.Right * Canvas) + grow) / Canvas,
            ((bounds.Bottom * Canvas) + grow) / Canvas);
    }

    private static bool Inside(Window window, int x, int y)
    {
        var px = (x + 0.5f) / Canvas;
        var py = (y + 0.5f) / Canvas;
        return px >= window.Left && px <= window.Right && py >= window.Top && py <= window.Bottom;
    }

    private static int Count(SKBitmap first, SKBitmap second, Window window)
    {
        var count = 0;
        var left = Math.Max(0, (int)MathF.Floor(window.Left * Canvas));
        var right = Math.Min(Canvas - 1, (int)MathF.Ceiling(window.Right * Canvas));
        var top = Math.Max(0, (int)MathF.Floor(window.Top * Canvas));
        var bottom = Math.Min(Canvas - 1, (int)MathF.Ceiling(window.Bottom * Canvas));
        for (var y = top; y <= bottom; y++)
        {
            for (var x = left; x <= right; x++)
            {
                if (Delta(first.GetPixel(x, y), second.GetPixel(x, y)) > 8)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static int Coloured(SKBitmap bitmap, Window window, Func<SKColor, bool> predicate)
    {
        var left = Math.Max(0, (int)MathF.Floor(window.Left * Canvas));
        var right = Math.Min(Canvas - 1, (int)MathF.Ceiling(window.Right * Canvas));
        var top = Math.Max(0, (int)MathF.Floor(window.Top * Canvas));
        var bottom = Math.Min(Canvas - 1, (int)MathF.Ceiling(window.Bottom * Canvas));
        var count = 0;
        for (var y = top; y <= bottom; y++)
        {
            for (var x = left; x <= right; x++)
            {
                if (predicate(bitmap.GetPixel(x, y)))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static SKBitmap Decode(byte[] png) =>
        SKBitmap.Decode(png) ?? throw new InvalidOperationException("The renderer produced an undecodable PNG.");

    private static int Delta(SKColor first, SKColor second) => Math.Max(
        Math.Max(Math.Abs(first.Red - second.Red), Math.Abs(first.Green - second.Green)),
        Math.Max(Math.Abs(first.Blue - second.Blue), Math.Abs(first.Alpha - second.Alpha)));
}
