using System.Diagnostics;
using System.Text.Json;
using Formloom.Core;
using Formloom.Rendering.Skia;
using SkiaSharp;
using Xunit;

namespace Formloom.Tests;

public sealed class VaseRoseRenderingTests
{
    private const int Canvas = 1024;
    private const int ReviewCell = 512;
    private const int RimBand = 14;

    private static readonly RoseSample[] Samples =
    [
        new("a red rose in a blue ceramic vase on a wooden table", "red-blue", "red", "blue", 42, 0.35, true),
        new("a red rose in a cream ceramic vase on a wooden table", "red-cream", "red", "cream", 42, 0.35, true),
        new("a pink rose in a blue ceramic vase on a wooden table", "pink-blue", "pink", "blue", 42, 0.35, true),
        new("a pink rose in a cream ceramic vase on a wooden table", "pink-cream", "pink", "cream", 42, 0.35, true),
        new("a red rose in a blue ceramic vase on a wooden table", "red-blue-altseed", "red", "blue", 1337, 0.35, false),
        new("a red rose in a cream ceramic vase on a wooden table", "red-cream-lowvariation", "red", "cream", 42, 0, false),
        new("a pink rose in a blue ceramic vase on a wooden table", "pink-blue-highvariation", "pink", "blue", 1337, 1, false),
        new("a pink rose in a cream ceramic vase on a wooden table", "pink-cream-altseed", "pink", "cream", 20261105, 0.35, false),
    ];

    public static TheoryData<string, string, string, string, int, double, bool> ExportCases()
    {
        var data = new TheoryData<string, string, string, string, int, double, bool>();
        foreach (var sample in Samples)
        {
            data.Add(sample.Prompt, sample.Name, sample.Color, sample.Glaze, sample.Seed, sample.Variation, sample.Canonical);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ExportCases))]
    public void EveryRoseRendersItsBloomLeavesAndContinuousVaseContact(
        string prompt,
        string name,
        string color,
        string glaze,
        int seed,
        double variation,
        bool canonical)
    {
        var scene = Scene(prompt, (ulong)seed, variation);
        var rose = scene.VaseRose!;
        Assert.Equal($"{color}-rose-in-{glaze}-vase", scene.SubjectToken);

        var timer = Stopwatch.StartNew();
        var rendered = StillLifeRenderer.Render(scene);
        timer.Stop();
        Assert.Equal(Canvas, rendered.Width);
        Assert.Equal(Canvas, rendered.Height);
        Assert.Equal(scene.SubjectToken, rendered.Subject);
        Assert.Equal(rendered.PngBytes, StillLifeRenderer.Render(scene).PngBytes);

        var emptyTimer = Stopwatch.StartNew();
        var emptyRendered = StillLifeRenderer.Render(EmptyVase(scene));
        emptyTimer.Stop();
        using var planted = Decode(rendered.PngBytes);
        using var empty = Decode(emptyRendered.PngBytes);

        var changed = CountChanged(planted, empty, new(0, 0, 1, 1));
        Assert.True(changed > 1200, $"{name} changed only {changed} pixels");
        AssertRoseHead(planted, empty, rose, Canvas, name);
        foreach (var leaf in rose.Plant.Leaves)
        {
            var green = CountChangedAndMatching(planted, empty, LeafWindow(leaf, Canvas), IsLeafGreen);
            Assert.True(green > 18, $"{name} lost a visible rose leaf near {leaf.Attachment}: {green} green pixels");
        }

        AssertStemContact(planted, empty, rose, Canvas, name);
        AssertNoPixelsLeakPastNearLip(planted, empty, rose, Canvas, name);

        var stemPixels = CountChangedAndMatching(
            planted,
            empty,
            new(rose.Plant.StemBase.X - 0.026f, rose.MouthTop - 0.065f, rose.Plant.StemBase.X + 0.026f, rose.MouthTop - 0.012f),
            IsLeafGreen);
        Assert.True(stemPixels > 30, $"{name} lost its visible green stem above the rim: {stemPixels} pixels");

        var stem = SampleStem(name, seed, variation);
        var directory = AppContext.BaseDirectory;
        File.WriteAllBytes(Path.Combine(directory, stem + ".png"), rendered.PngBytes);
        if (canonical)
        {
            var silhouette = StillLifeRenderer.RenderSilhouette(scene);
            File.WriteAllBytes(Path.Combine(directory, stem + "-silhouette.png"), silhouette.PngBytes);
        }

        var sidecar = JsonSerializer.Serialize(
            new
            {
                Prompt = prompt,
                Subject = scene.SubjectToken,
                Color = rose.Color.ToString(),
                VaseKind = rose.Vase.Kind.ToString(),
                VaseGlaze = rose.Vase.Glaze.ToString(),
                OpeningCenter = rose.OpeningCenter,
                OpeningRadius = rose.OpeningRadius,
                rose.MouthTop,
                Stem = new { rose.Plant.StemBase, rose.Plant.StemTop, rose.Plant.StemThickness, Segments = rose.Plant.Stem.Length },
                Leaves = rose.Plant.Leaves.Select(leaf => new { leaf.Attachment, leaf.Tip, leaf.Width }).ToArray(),
                Bloom = new
                {
                    rose.Plant.Bloom.Center,
                    rose.Plant.Bloom.RadiusX,
                    rose.Plant.Bloom.RadiusY,
                    Layers = new
                    {
                        Outer = rose.Plant.Bloom.Petals.Count(petal => petal.Layer == RosePetalLayer.Outer),
                        Middle = rose.Plant.Bloom.Petals.Count(petal => petal.Layer == RosePetalLayer.Middle),
                        Inner = rose.Plant.Bloom.Petals.Count(petal => petal.Layer == RosePetalLayer.Inner),
                    },
                },
                rose.Plant.Bounds,
                rendered.Width,
                rendered.Height,
                Framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                OperatingSystem = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                scene.Seed,
                scene.Variation,
                RenderAndEncodeMilliseconds = timer.Elapsed.TotalMilliseconds,
                EmptyVaseRenderAndEncodeMilliseconds = emptyTimer.Elapsed.TotalMilliseconds,
                ProcessorCount = Environment.ProcessorCount,
                RenderedBytes = rendered.PngBytes.Length,
                EmptyVaseBytes = emptyRendered.PngBytes.Length,
            },
            new JsonSerializerOptions { IncludeFields = true });
        File.WriteAllText(Path.Combine(directory, stem + ".timing.json"), sidecar);
        Assert.True(File.Exists(Path.Combine(directory, stem + ".png")));
        Assert.True(File.Exists(Path.Combine(directory, stem + ".timing.json")));
    }

    [Fact]
    public void RoseLayerBandsAndInnerWhorlAreVisibleAtFullResolutionAndReviewSize()
    {
        foreach (var sample in Samples.Where(sample => sample.Canonical))
        {
            var scene = Scene(sample.Prompt);
            var rose = scene.VaseRose!;
            var name = sample.Name;
            Assert.Equal(sample.Color, rose.Color.ToString().ToLowerInvariant());
            using var full = Decode(StillLifeRenderer.Render(scene).PngBytes);
            using var fullEmpty = Decode(StillLifeRenderer.Render(EmptyVase(scene)).PngBytes);
            AssertRoseLayerBands(full, fullEmpty, rose, Canvas, name + " full size");

            var previewScene = Scene(sample.Prompt, 42, 0.35, ReviewCell, ReviewCell);
            var previewRose = previewScene.VaseRose!;
            using var preview = Decode(StillLifeRenderer.Render(previewScene).PngBytes);
            using var previewEmpty = Decode(StillLifeRenderer.Render(EmptyVase(previewScene)).PngBytes);
            AssertRoseLayerBands(preview, previewEmpty, previewRose, ReviewCell, name + " 512 px");
        }
    }

    [Fact]
    public void RedAndPinkPetalsStayDistinctAndIndependentOfVaseGlaze()
    {
        foreach (var color in new[] { "red", "pink" })
        {
            var blue = Scene($"a {color} rose in a blue ceramic vase on a wooden table");
            var cream = Scene($"a {color} rose in a cream ceramic vase on a wooden table");
            using var bluePixels = Decode(StillLifeRenderer.Render(blue).PngBytes);
            using var creamPixels = Decode(StillLifeRenderer.Render(cream).PngBytes);
            var rose = blue.VaseRose!;
            var aboveMouth = new Window(0, 0, 1, rose.MouthTop - (4f / Canvas));
            Assert.Equal(0, CountDifferent(bluePixels, creamPixels, aboveMouth));
            using var empty = Decode(StillLifeRenderer.Render(EmptyVase(blue)).PngBytes);
            Assert.True(
                CountChangedAndMatching(bluePixels, empty, BloomWindow(rose.Plant.Bloom, Canvas, 4), pixel => IsRosePetal(pixel, rose.Color)) > 800,
                $"{color} petal palette did not paint a readable bloom");
        }

        var red = Scene("a red rose in a blue ceramic vase on a wooden table");
        var pink = Scene("a pink rose in a blue ceramic vase on a wooden table");
        using var redPixels = Decode(StillLifeRenderer.Render(red).PngBytes);
        using var pinkPixels = Decode(StillLifeRenderer.Render(pink).PngBytes);
        Assert.True(
            CountDifferent(redPixels, pinkPixels, BloomWindow(red.VaseRose!.Plant.Bloom, Canvas, 4)) > 2500,
            "red and pink rose materials should remain visibly distinct");
        using var redEmpty = Decode(StillLifeRenderer.Render(EmptyVase(red)).PngBytes);
        using var pinkEmpty = Decode(StillLifeRenderer.Render(EmptyVase(pink)).PngBytes);
        AssertRoseHead(redPixels, redEmpty, red.VaseRose!, Canvas, "red palette");
        AssertRoseHead(pinkPixels, pinkEmpty, pink.VaseRose!, Canvas, "pink palette");
    }

    [Fact]
    public void VaseRoseAddsNoTabletopShadowAndItsSilhouetteOnlyAddsThePlant()
    {
        var scene = Scene("a pink rose in a blue ceramic vase on a wooden table");
        var rose = scene.VaseRose!;
        Assert.Equal(
            StillLifeRenderer.RenderShadowMask(EmptyVase(scene)).PngBytes,
            StillLifeRenderer.RenderShadowMask(scene).PngBytes);

        using var empty = Decode(StillLifeRenderer.RenderSilhouette(EmptyVase(scene)).PngBytes);
        using var planted = Decode(StillLifeRenderer.RenderSilhouette(scene).PngBytes);
        var rim = (int)MathF.Ceiling(rose.MouthTop * Canvas) + RimBand;
        var added = 0;
        var lost = 0;
        var addedBelowRim = 0;
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
                        addedBelowRim++;
                    }
                }
            }
        }

        Assert.Equal(0, lost);
        Assert.Equal(0, addedBelowRim);
        Assert.True(added > 1000, $"the rose silhouette added only {added} pixels");
    }

    [Fact]
    public void DetailOmissionAndDiagnosticPassesStayScopedToTheRose()
    {
        var scene = Scene("a red rose in a cream ceramic vase on a wooden table", 1337, 0.6);
        var rose = scene.VaseRose!;
        var bounds = PlantWindow(rose.Plant, Canvas, 4);
        var fullImage = StillLifeRenderer.Render(scene);
        using var full = Decode(fullImage.PngBytes);
        using var plain = Decode(StillLifeRenderer.RenderWithoutInstanceDetail(scene, scene.Objects[0].Depth).PngBytes);

        var rim = (int)MathF.Ceiling(rose.MouthTop * Canvas) - RimBand;
        var changed = 0;
        var outside = 0;
        for (var y = 0; y < rim; y++)
        {
            for (var x = 0; x < Canvas; x++)
            {
                if (PixelDelta(full.GetPixel(x, y), plain.GetPixel(x, y)) == 0)
                {
                    continue;
                }

                changed++;
                if (!Inside(bounds, x, y, Canvas))
                {
                    outside++;
                }
            }
        }

        Assert.True(changed > 120, $"expected rose folds, edges and leaf veins to be omitted, saw {changed} pixels");
        Assert.Equal(0, outside);

        using var shadowMask = Decode(StillLifeRenderer.RenderShadowMask(scene).PngBytes);
        using var silhouette = Decode(StillLifeRenderer.RenderSilhouette(scene).PngBytes);
        Assert.NotNull(shadowMask);
        Assert.NotNull(silhouette);
        Assert.Equal(fullImage.PngBytes, StillLifeRenderer.Render(scene).PngBytes);
    }

    [Theory]
    [InlineData("a red rose in a blue ceramic vase on a wooden table", 64, 64)]
    [InlineData("a red rose in a cream ceramic vase on a wooden table", 320, 320)]
    [InlineData("a pink rose in a blue ceramic vase on a wooden table", 1024, 1024)]
    [InlineData("a pink rose in a cream ceramic vase on a wooden table", 640, 480)]
    [InlineData("a red rose in a blue ceramic vase on a wooden table", 4096, 1024)]
    [InlineData("a pink rose in a cream ceramic vase on a wooden table", 1024, 4096)]
    public void RoseRendersAtSupportedOutputDimensions(string prompt, int width, int height)
    {
        var scene = Scene(prompt, 1337, 1, width, height);
        var before = scene.VaseRose!.Plant;
        var image = StillLifeRenderer.Render(scene);
        Assert.Equal(width, image.Width);
        Assert.Equal(height, image.Height);
        Assert.Equal(scene.SubjectToken, image.Subject);
        Assert.NotEmpty(image.PngBytes);
        using var decoded = Decode(image.PngBytes);
        Assert.Equal(width, decoded.Width);
        Assert.Equal(height, decoded.Height);
        Assert.Same(before, scene.VaseRose.Plant);
    }

    [Fact]
    public void AntialiasingOnlyChangesRasterizationForRoseRenders()
    {
        var scene = Scene("a red rose in a blue ceramic vase on a wooden table", 42, 0.35);
        var plant = scene.VaseRose!.Plant;
        var antialiased = StillLifeRenderer.Render(scene, new RenderOptions(Antialiasing: true));
        var hard = StillLifeRenderer.Render(scene, new RenderOptions(Antialiasing: false));
        Assert.Equal(antialiased.Width, hard.Width);
        Assert.Equal(antialiased.Height, hard.Height);
        Assert.Equal(antialiased.Subject, hard.Subject);
        Assert.NotEqual(antialiased.PngBytes, hard.PngBytes);
        Assert.Same(plant, scene.VaseRose.Plant);
    }

    [Fact]
    public void ExportCanonicalReviewSheetsAndSilhouetteSheet()
    {
        WriteSheet("canonical", Samples.Where(sample => sample.Canonical).ToArray(),
            "row-major 2 by 2; red/blue, red/cream, pink/blue, pink/cream at seed 42 and variation 0.35");
        WriteSheet("robustness", Samples.Where(sample => !sample.Canonical).ToArray(),
            "row-major 2 by 2; alternate seed, zero variation and full variation cases cover both colors and both vase glazes");
        WriteCanonicalSilhouetteReviewSheet();

        var directory = AppContext.BaseDirectory;
        Assert.True(File.Exists(Path.Combine(directory, "formloom-vase-rose-canonical-sheet.png")));
        Assert.True(File.Exists(Path.Combine(directory, "formloom-vase-rose-canonical-sheet.timing.json")));
        Assert.True(File.Exists(Path.Combine(directory, "formloom-vase-rose-robustness-sheet.png")));
        Assert.True(File.Exists(Path.Combine(directory, "formloom-vase-rose-robustness-sheet.timing.json")));
        Assert.True(File.Exists(Path.Combine(directory, "formloom-vase-rose-canonical-silhouette-sheet.png")));
    }

    private static void WriteSheet(string label, RoseSample[] samples, string layout)
    {
        Assert.Equal(4, samples.Length);
        using var sheet = new SKBitmap(ReviewCell * 2, ReviewCell * 2);
        using var canvas = new SKCanvas(sheet);
        canvas.Clear(new SKColor(20, 22, 24));
        var cases = new List<object>();
        for (var index = 0; index < samples.Length; index++)
        {
            var sample = samples[index];
            var scene = Scene(sample.Prompt, (ulong)sample.Seed, sample.Variation, ReviewCell, ReviewCell);
            var timer = Stopwatch.StartNew();
            var rendered = StillLifeRenderer.Render(scene);
            timer.Stop();
            using var cell = Decode(rendered.PngBytes);
            using var empty = Decode(StillLifeRenderer.Render(EmptyVase(scene)).PngBytes);
            var rose = scene.VaseRose!;
            AssertRoseHead(cell, empty, rose, ReviewCell, $"{label} cell {index} {sample.Name}");
            AssertStemContact(cell, empty, rose, ReviewCell, $"{label} cell {index} {sample.Name}");
            AssertNoPixelsLeakPastNearLip(cell, empty, rose, ReviewCell, $"{label} cell {index} {sample.Name}");
            foreach (var leaf in rose.Plant.Leaves)
            {
                var green = CountChangedAndMatching(cell, empty, LeafWindow(leaf, ReviewCell), IsLeafGreen);
                Assert.True(green > 4, $"{label} cell {index} lost a rose leaf ({green} pixels)");
            }

            var left = (index % 2) * ReviewCell;
            var top = (index / 2) * ReviewCell;
            canvas.DrawBitmap(cell, new SKRect(left, top, left + ReviewCell, top + ReviewCell), new SKSamplingOptions(SKFilterMode.Linear));
            cases.Add(new
            {
                Cell = index,
                Column = index % 2,
                Row = index / 2,
                sample.Prompt,
                sample.Color,
                VaseGlaze = sample.Glaze,
                sample.Seed,
                sample.Variation,
                Subject = scene.SubjectToken,
                FileName = SampleStem(sample.Name, sample.Seed, sample.Variation) + ".png",
                Width = rendered.Width,
                Height = rendered.Height,
                RenderAndEncodeMilliseconds = timer.Elapsed.TotalMilliseconds,
            });
        }

        var name = $"formloom-vase-rose-{label}-sheet";
        var directory = AppContext.BaseDirectory;
        using var image = SKImage.FromBitmap(sheet);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(Path.Combine(directory, name + ".png"), encoded.ToArray());
        File.WriteAllText(
            Path.Combine(directory, name + ".timing.json"),
            JsonSerializer.Serialize(
                new
                {
                    Sheet = name + ".png",
                    Cell = ReviewCell,
                    Columns = 2,
                    Rows = 2,
                    Layout = layout,
                    Generated = DateTimeOffset.UtcNow,
                    Cases = cases,
                },
                new JsonSerializerOptions { IncludeFields = true }));
        Assert.Equal(4, cases.Count);
    }

    private static void WriteCanonicalSilhouetteReviewSheet()
    {
        var samples = Samples.Where(sample => sample.Canonical).ToArray();
        Assert.Equal(4, samples.Length);

        using var sheet = new SKBitmap(ReviewCell * 2, ReviewCell * 2);
        using var canvas = new SKCanvas(sheet);
        canvas.Clear(SKColors.White);

        for (var index = 0; index < samples.Length; index++)
        {
            var sample = samples[index];
            var scene = Scene(sample.Prompt, (ulong)sample.Seed, sample.Variation);
            using var silhouette = Decode(StillLifeRenderer.RenderSilhouette(scene).PngBytes);
            Assert.Equal(Canvas, silhouette.Width);
            Assert.Equal(Canvas, silhouette.Height);

            var left = (index % 2) * ReviewCell;
            var top = (index / 2) * ReviewCell;
            canvas.DrawBitmap(
                silhouette,
                new SKRect(left, top, left + ReviewCell, top + ReviewCell),
                new SKSamplingOptions(SKFilterMode.Linear));
        }

        canvas.Flush();
        var fileName = "formloom-vase-rose-canonical-silhouette-sheet.png";
        using var image = SKImage.FromBitmap(sheet);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory, fileName), encoded.ToArray());

        for (var index = 0; index < samples.Length; index++)
        {
            var left = (index % 2) * ReviewCell;
            var top = (index / 2) * ReviewCell;
            Assert.Equal(SKColors.White, sheet.GetPixel(left, top));

            var darkPixels = 0;
            for (var y = top; y < top + ReviewCell; y++)
            {
                for (var x = left; x < left + ReviewCell; x++)
                {
                    var pixel = sheet.GetPixel(x, y);
                    if (pixel.Red < 32 && pixel.Green < 32 && pixel.Blue < 32)
                    {
                        darkPixels++;
                    }
                }
            }

            Assert.True(
                darkPixels > 1000,
                $"{samples[index].Name} silhouette was not visible on white: {darkPixels} dark pixels");
        }
    }

    private static void AssertRoseHead(SKBitmap planted, SKBitmap empty, VaseRose rose, int canvas, string name)
    {
        var bloomWindow = BloomWindow(rose.Plant.Bloom, canvas, 4);
        var petalPixels = CountChangedAndMatching(
            planted,
            empty,
            bloomWindow,
            pixel => IsRosePetal(pixel, rose.Color));
        Assert.True(petalPixels > Math.Max(50, canvas * canvas / 1300), $"{name} lost its {rose.Color} bloom: {petalPixels} petal pixels");
        AssertRoseLayerBands(planted, empty, rose, canvas, name);
    }

    private static void AssertRoseLayerBands(SKBitmap planted, SKBitmap empty, VaseRose rose, int canvas, string name)
    {
        var bloom = rose.Plant.Bloom;
        var outer = CountRoseInRing(planted, empty, bloom, canvas, 0.82f, 1.18f, rose.Color);
        var middle = CountRoseInRing(planted, empty, bloom, canvas, 0.55f, 0.84f, rose.Color);
        var whorl = CountRoseInRing(planted, empty, bloom, canvas, 0.08f, 0.53f, rose.Color);
        var minimum = Math.Max(8, canvas * canvas / 18000);
        Assert.True(outer > minimum, $"{name} lost the broad outer petal layer: {outer} pixels");
        Assert.True(middle > minimum, $"{name} lost the overlapping middle layer: {middle} pixels");
        Assert.True(whorl > minimum, $"{name} lost the tight inner whorl: {whorl} pixels");
    }

    private static int CountRoseInRing(
        SKBitmap planted,
        SKBitmap empty,
        RoseBloom bloom,
        int canvas,
        float innerRadius,
        float outerRadius,
        RoseColor color)
    {
        var count = 0;
        for (var y = 0; y < planted.Height; y++)
        {
            for (var x = 0; x < planted.Width; x++)
            {
                if (PixelDelta(planted.GetPixel(x, y), empty.GetPixel(x, y)) == 0 ||
                    !IsRosePetal(planted.GetPixel(x, y), color))
                {
                    continue;
                }

                var normalizedX = (x + 0.5f) / canvas;
                var normalizedY = (y + 0.5f) / canvas;
                var dx = (normalizedX - bloom.Center.X) / bloom.RadiusX;
                var dy = (normalizedY - bloom.Center.Y) / bloom.RadiusY;
                var radius = MathF.Sqrt((dx * dx) + (dy * dy));
                if (radius >= innerRadius && radius <= outerRadius)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static void AssertStemContact(SKBitmap planted, SKBitmap empty, VaseRose rose, int canvas, string name)
    {
        var mouthRow = (int)MathF.Round(rose.MouthTop * canvas);
        var openingBottom = (int)MathF.Round((rose.OpeningCenter.Y + rose.OpeningRadius.Y) * canvas);
        var innerLip = LipInnerRow(empty, rose, canvas, openingBottom);
        var rimBand = Math.Max(5, (int)MathF.Ceiling(RimBand * (canvas / (float)Canvas)));
        var first = mouthRow - (rimBand / 2);
        var last = innerLip - 2;
        Assert.True(last >= first, $"{name} has no measurable opening stem interval ({first}..{last})");

        for (var y = first; y <= last; y++)
        {
            var center = StemColumn(rose, y, canvas);
            var stemHalfWidth = (int)MathF.Ceiling((rose.Plant.StemThickness * canvas) / 2);
            var halfWidth = Math.Max(2, stemHalfWidth + 2);
            var changed = false;
            for (var x = Math.Max(0, center - halfWidth); x <= Math.Min(planted.Width - 1, center + halfWidth); x++)
            {
                if (PixelDelta(planted.GetPixel(x, y), empty.GetPixel(x, y)) > 0)
                {
                    changed = true;
                    break;
                }
            }

            Assert.True(changed, $"{name} stem is discontinuous at row {y}, near x={center}");
        }
    }

    private static void AssertNoPixelsLeakPastNearLip(SKBitmap planted, SKBitmap empty, VaseRose rose, int canvas, string name)
    {
        var first = (int)MathF.Ceiling(rose.MouthTop * canvas) + Math.Max(5, (int)MathF.Ceiling(RimBand * (canvas / (float)Canvas)));
        for (var y = first; y < planted.Height; y++)
        {
            for (var x = 0; x < planted.Width; x++)
            {
                if (PixelDelta(planted.GetPixel(x, y), empty.GetPixel(x, y)) == 0 || InsideOpening(rose, x, y, canvas, 2))
                {
                    continue;
                }

                Assert.Fail($"{name} leaked rose pixels onto the vase wall or setting at {x},{y}");
            }
        }
    }

    private static int LipInnerRow(SKBitmap empty, VaseRose rose, int canvas, int bottom)
    {
        var column = StemColumn(rose, bottom, canvas);
        var centerRow = (int)MathF.Round(rose.OpeningCenter.Y * canvas);
        var row = bottom;
        var largestRise = 0;
        var previous = Brightness(empty.GetPixel(column, centerRow));
        for (var y = centerRow + 1; y <= bottom; y++)
        {
            var here = Brightness(empty.GetPixel(column, y));
            if (here - previous > largestRise)
            {
                largestRise = here - previous;
                row = y;
            }

            previous = here;
        }

        return row;
    }

    private static int StemColumn(VaseRose rose, int y, int canvas)
    {
        var target = (y + 0.5f) / canvas;
        var segment = target <= rose.Plant.StemBase.Y ? rose.Plant.Stem[0] : StemExtension(rose);
        var bestY = float.MaxValue;
        var bestX = rose.Plant.StemBase.X;
        for (var step = 0; step <= 96; step++)
        {
            var point = segment.Evaluate(step / 96f);
            var distance = MathF.Abs(point.Y - target);
            if (distance < bestY)
            {
                bestY = distance;
                bestX = point.X;
            }
        }

        return Math.Clamp((int)MathF.Round(bestX * canvas), 0, canvas - 1);
    }

    private static CubicSegment StemExtension(VaseRose rose)
    {
        var stemBase = rose.Plant.StemBase;
        var openingCenter = rose.OpeningCenter;
        var openingRadius = rose.OpeningRadius;
        var thickness = rose.Plant.StemThickness;
        var end = new System.Numerics.Vector2(
            stemBase.X,
            openingCenter.Y + openingRadius.Y + (thickness * 3));
        var extensionLength = end.Y - stemBase.Y;
        var baseTangent = stemBase - rose.Plant.Stem[0].Control1;
        var downward = baseTangent.LengthSquared() > 1e-10f
            ? System.Numerics.Vector2.Normalize(baseTangent)
            : System.Numerics.Vector2.UnitY;
        var control1 = stemBase + (downward * (extensionLength * 0.4f));
        var control2 = end - new System.Numerics.Vector2(0, extensionLength * 0.3f);
        return new(stemBase, control1, control2, end);
    }

    private static bool InsideOpening(VaseRose rose, int x, int y, int canvas, float marginPixels)
    {
        var normalizedX = (x + 0.5f) / canvas;
        var normalizedY = (y + 0.5f) / canvas;
        var radiusX = rose.OpeningRadius.X + (marginPixels / canvas);
        var radiusY = rose.OpeningRadius.Y + (marginPixels / canvas);
        var dx = (normalizedX - rose.OpeningCenter.X) / radiusX;
        var dy = (normalizedY - rose.OpeningCenter.Y) / radiusY;
        return (dx * dx) + (dy * dy) <= 1;
    }

    private static int Brightness(SKColor pixel) => pixel.Red + pixel.Green + pixel.Blue;

    private static void AssertRoseHead(SKBitmap planted, SKBitmap empty, VaseRose rose, string name) =>
        AssertRoseHead(planted, empty, rose, planted.Width, name);

    private static int CountChangedAndMatching(SKBitmap planted, SKBitmap empty, Window window, Func<SKColor, bool> predicate)
    {
        var (left, top, right, bottom) = PixelWindow(window, planted.Width, planted.Height);
        var count = 0;
        for (var y = top; y < bottom; y++)
        {
            for (var x = left; x < right; x++)
            {
                var pixel = planted.GetPixel(x, y);
                if (PixelDelta(pixel, empty.GetPixel(x, y)) > 0 && predicate(pixel))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static int CountChanged(SKBitmap first, SKBitmap second, Window window)
    {
        var (left, top, right, bottom) = PixelWindow(window, first.Width, first.Height);
        var count = 0;
        for (var y = top; y < bottom; y++)
        {
            for (var x = left; x < right; x++)
            {
                if (PixelDelta(first.GetPixel(x, y), second.GetPixel(x, y)) > 0)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static int CountDifferent(SKBitmap first, SKBitmap second, Window window)
    {
        var (left, top, right, bottom) = PixelWindow(window, first.Width, first.Height);
        var count = 0;
        for (var y = top; y < bottom; y++)
        {
            for (var x = left; x < right; x++)
            {
                if (PixelDelta(first.GetPixel(x, y), second.GetPixel(x, y)) > 0)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static Window BloomWindow(RoseBloom bloom, int canvas, int growPixels)
    {
        var grow = growPixels / (float)canvas;
        return new(
            bloom.Center.X - (bloom.RadiusX * 1.62f) - grow,
            bloom.Center.Y - (bloom.RadiusY * 1.62f) - grow,
            bloom.Center.X + (bloom.RadiusX * 1.62f) + grow,
            bloom.Center.Y + (bloom.RadiusY * 1.62f) + grow);
    }

    private static Window LeafWindow(RoseLeaf leaf, int canvas)
    {
        var middle = (leaf.Attachment + leaf.Tip) / 2;
        var reach = MathF.Max(leaf.Width * 0.66f, 5f / canvas);
        return new(middle.X - reach, middle.Y - reach, middle.X + reach, middle.Y + reach);
    }

    private static Window PlantWindow(RosePlant plant, int canvas, int growPixels)
    {
        var bounds = plant.Bounds;
        var grow = growPixels / (float)canvas;
        return new(bounds.Left - grow, bounds.Top - grow, bounds.Right + grow, bounds.Bottom + grow);
    }

    private static (int Left, int Top, int Right, int Bottom) PixelWindow(Window window, int width, int height) =>
        (
            Math.Clamp((int)MathF.Floor(window.Left * width), 0, width),
            Math.Clamp((int)MathF.Floor(window.Top * height), 0, height),
            Math.Clamp((int)MathF.Ceiling(window.Right * width), 0, width),
            Math.Clamp((int)MathF.Ceiling(window.Bottom * height), 0, height));

    private static bool Inside(Window window, int x, int y, int canvas) =>
        (x + 0.5f) / canvas >= window.Left && (x + 0.5f) / canvas <= window.Right &&
        (y + 0.5f) / canvas >= window.Top && (y + 0.5f) / canvas <= window.Bottom;

    private static bool IsLeafGreen(SKColor pixel) =>
        pixel.Green > 80 && pixel.Green > pixel.Red + 12 && pixel.Green > pixel.Blue + 12;

    private static bool IsRosePetal(SKColor pixel, RoseColor color) => color switch
    {
        RoseColor.Red => pixel.Red > 80 && pixel.Red > pixel.Green + 25 && pixel.Red > pixel.Blue + 18,
        RoseColor.Pink => pixel.Red > 115 && pixel.Red > pixel.Green + 35 && pixel.Blue > pixel.Green + 22,
        _ => false,
    };

    private static int PixelDelta(SKColor first, SKColor second) =>
        Math.Abs(first.Red - second.Red) + Math.Abs(first.Green - second.Green) +
        Math.Abs(first.Blue - second.Blue) + Math.Abs(first.Alpha - second.Alpha);

    private static StillLifeScene Scene(
        string prompt,
        ulong seed = 42,
        double variation = 0.35,
        int width = Canvas,
        int height = Canvas)
    {
        var result = SceneGenerator.Generate(new(prompt, seed, variation, Width: width, Height: height));
        Assert.True(result.IsSuccess, result.Error);
        return result.Scene!;
    }

    private static StillLifeScene EmptyVase(StillLifeScene scene) => scene with { VaseRose = null };

    private static string SampleStem(string name, int seed, double variation) =>
        FormattableString.Invariant($"formloom-vase-rose-{name}-seed-{seed}-variation-{variation:0.00}");

    private static SKBitmap Decode(byte[] bytes) =>
        SKBitmap.Decode(bytes) ?? throw new InvalidOperationException("Could not decode the rendered PNG.");

    private readonly record struct RoseSample(
        string Prompt,
        string Name,
        string Color,
        string Glaze,
        int Seed,
        double Variation,
        bool Canonical);

    private readonly record struct Window(float Left, float Top, float Right, float Bottom);
}
