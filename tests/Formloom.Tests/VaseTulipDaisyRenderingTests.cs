using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using Formloom.Core;
using Formloom.Rendering.Skia;
using SkiaSharp;
using Xunit;

namespace Formloom.Tests;

public sealed class VaseTulipDaisyRenderingTests
{
    private const int Canvas = 1024;
    private const int Cell = 320;
    private const string Canonical = "formloom-vase-tulip-daisy-red-blue-seed-42-variation-0.35";

    private static readonly Sample[] DefaultSamples =
    [
        new("a red tulip and a white daisy in a blue ceramic vase on a wooden table", "red-blue", TulipColor.Red, CeramicGlaze.Blue, 42, 0.35),
        new("a red tulip and a white daisy in a cream ceramic vase on a wooden table", "red-cream", TulipColor.Red, CeramicGlaze.Cream, 42, 0.35),
        new("a yellow tulip and a white daisy in a blue ceramic vase on a wooden table", "yellow-blue", TulipColor.Yellow, CeramicGlaze.Blue, 42, 0.35),
        new("a yellow tulip and a white daisy in a cream ceramic vase on a wooden table", "yellow-cream", TulipColor.Yellow, CeramicGlaze.Cream, 42, 0.35),
    ];

    // The first row is the exact four-combination matrix. Remaining cells add a second seed
    // for both tulip colours plus low/high variation examples for red and yellow.
    private static readonly Sample[] ReviewSamples =
    [
        DefaultSamples[0],
        DefaultSamples[1],
        DefaultSamples[2],
        DefaultSamples[3],
        new("a red tulip and a white daisy in a blue ceramic vase on a wooden table", "red-blue-altseed", TulipColor.Red, CeramicGlaze.Blue, 1337, 0.35),
        new("a yellow tulip and a white daisy in a cream ceramic vase on a wooden table", "yellow-cream-altseed", TulipColor.Yellow, CeramicGlaze.Cream, 20261105, 0.35),
        new("a red tulip and a white daisy in a blue ceramic vase on a wooden table", "red-blue-lowvariation", TulipColor.Red, CeramicGlaze.Blue, 42, 0),
        new("a red tulip and a white daisy in a cream ceramic vase on a wooden table", "red-cream-highvariation", TulipColor.Red, CeramicGlaze.Cream, 42, 1),
        new("a yellow tulip and a white daisy in a blue ceramic vase on a wooden table", "yellow-blue-lowvariation", TulipColor.Yellow, CeramicGlaze.Blue, 20261105, 0),
        new("a yellow tulip and a white daisy in a cream ceramic vase on a wooden table", "yellow-cream-highvariation", TulipColor.Yellow, CeramicGlaze.Cream, 1337, 1),
    ];

    private static readonly string[] RetainedReviewStems = ReviewSamples.Select(SampleStem).ToArray();

    public static TheoryData<string, string, string, string, int, double> CombinationCases()
    {
        var data = new TheoryData<string, string, string, string, int, double>();
        foreach (var sample in DefaultSamples)
        {
            data.Add(
                sample.Prompt,
                sample.Name,
                sample.TulipColor.ToString().ToLowerInvariant(),
                sample.Glaze.ToString().ToLowerInvariant(),
                sample.Seed,
                sample.Variation);
        }

        return data;
    }

    public static TheoryData<string, string, string, string, int, double> ReviewCases()
    {
        var data = new TheoryData<string, string, string, string, int, double>();
        foreach (var sample in ReviewSamples)
        {
            data.Add(
                sample.Prompt,
                sample.Name,
                sample.TulipColor.ToString().ToLowerInvariant(),
                sample.Glaze.ToString().ToLowerInvariant(),
                sample.Seed,
                sample.Variation);
        }

        return data;
    }

    public static TheoryData<string, int, int> Dimensions()
    {
        var data = new TheoryData<string, int, int>();
        foreach (var sample in DefaultSamples)
        {
            foreach (var (width, height) in new[] { (64, 64), (320, 320), (640, 480), (2048, 1024), (4096, 1024) })
            {
                data.Add(sample.Prompt, width, height);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(CombinationCases))]
    public void EveryCombinationRendersBothSpeciesAndLeavesTheVaseAndShadowUnchanged(
        string prompt,
        string name,
        string color,
        string glaze,
        int seed,
        double variation)
    {
        var scene = Scene(prompt, (ulong)seed, variation);
        var pair = scene.VaseTulipDaisy!;
        var image = StillLifeRenderer.Render(scene);
        Assert.Equal(Canvas, image.Width);
        Assert.Equal(Canvas, image.Height);
        Assert.Equal($"{color}-tulip-and-white-daisy-in-{glaze}-vase", image.Subject);
        Assert.Equal(image.PngBytes, StillLifeRenderer.Render(scene).PngBytes);

        using var planted = Decode(image.PngBytes);
        using var empty = Decode(StillLifeRenderer.Render(EmptyVase(scene)).PngBytes);
        AssertStemFitsOpening(pair.Tulip.StemBase, pair.Tulip.StemThickness, pair.OpeningCenter, pair.OpeningRadius);
        AssertStemFitsOpening(pair.Daisy.StemBase, pair.Daisy.StemThickness, pair.OpeningCenter, pair.OpeningRadius);
        var tulipBloom = TulipBloomWindow(pair.Tulip, 4);
        var daisyBloom = DaisyBloomWindow(pair.Daisy, 4);
        var tulipPetals = Coloured(planted, empty, tulipBloom, pixel => IsTulipPetal(pixel, pair.TulipColor));
        var daisyPetals = Coloured(planted, empty, daisyBloom, IsPetalWhite);
        var goldenDisk = Coloured(planted, empty, DiskWindow(pair.Daisy.Bloom.Disk, 4), IsDiskGold);
        Assert.True(tulipPetals > 700, $"{name} lost its {color} tulip petals: {tulipPetals}");
        Assert.True(daisyPetals > 700, $"{name} lost its white daisy petals: {daisyPetals}");
        Assert.True(goldenDisk > 60, $"{name} lost its golden disk: {goldenDisk}");

        foreach (var leaf in pair.Tulip.Leaves)
        {
            var green = Coloured(planted, empty, LeafWindow(leaf, 4), IsLeafGreen);
            Assert.True(green > 18, $"{name} lost a tulip leaf at {leaf.Attachment}: {green} pixels");
        }

        foreach (var leaf in pair.Daisy.Leaves)
        {
            var green = Coloured(planted, empty, LeafWindow(leaf, 4), IsLeafGreen);
            Assert.True(green > 18, $"{name} lost a daisy leaf at {leaf.Attachment}: {green} pixels");
        }

        AssertStemVisibleAboveRim(planted, empty, pair.Tulip, pair.MouthTop, name + " tulip");
        AssertStemVisibleAboveRim(planted, empty, pair.Daisy, pair.MouthTop, name + " daisy");
        AssertStemContinuousThroughMouth(planted, empty, pair, pair.Tulip, name + " tulip");
        AssertStemContinuousThroughMouth(planted, empty, pair, pair.Daisy, name + " daisy");
        AssertStemContinuationInsideOpening(planted, empty, pair, pair.Tulip, name + " tulip");
        AssertStemContinuationInsideOpening(planted, empty, pair, pair.Daisy, name + " daisy");
        AssertChangedPixelsStayOnThePlantsOrInsideTheMouth(planted, empty, pair, name);

        Assert.Equal(
            StillLifeRenderer.RenderShadowMask(EmptyVase(scene)).PngBytes,
            StillLifeRenderer.RenderShadowMask(scene).PngBytes);
        using var silhouette = Decode(StillLifeRenderer.RenderSilhouette(scene).PngBytes);
        using var emptySilhouette = Decode(StillLifeRenderer.RenderSilhouette(EmptyVase(scene)).PngBytes);
        Assert.True(CountChanged(silhouette, emptySilhouette, PlantWindow(pair, 4)) > 800, $"{name} silhouette omitted a flower");

        _ = StillLifeRenderer.RenderWithoutInstanceDetail(scene, scene.Objects[0].Depth);
        Assert.Equal(image.PngBytes, StillLifeRenderer.Render(scene).PngBytes);
    }

    [Fact]
    public void DetailOmissionAndDiagnosticPassesStayInsideTheArrangement()
    {
        var scene = Scene(DefaultSamples[0].Prompt, 1337, 0.6);
        var pair = scene.VaseTulipDaisy!;
        var bounds = PlantWindow(pair, 3);
        using var full = Decode(StillLifeRenderer.Render(scene).PngBytes);
        using var plain = Decode(StillLifeRenderer.RenderWithoutInstanceDetail(scene, scene.Objects[0].Depth).PngBytes);

        var rim = (int)MathF.Ceiling(pair.MouthTop * Canvas) - 3;
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
                if (!Inside(bounds, x, y, Canvas, Canvas))
                {
                    outside++;
                }
            }
        }

        Assert.True(changed > 100, $"expected tulip and daisy details to be omitted, saw {changed} pixels");
        Assert.Equal(0, outside);

        var original = StillLifeRenderer.Render(scene);
        using var shadows = Decode(StillLifeRenderer.RenderShadowMask(scene).PngBytes);
        using var change = Decode(StillLifeRenderer.RenderWithoutInstanceDetail(scene, scene.Objects[0].Depth).PngBytes);
        using var silhouette = Decode(StillLifeRenderer.RenderSilhouette(scene).PngBytes);
        Assert.NotNull(shadows);
        Assert.NotNull(change);
        Assert.NotNull(silhouette);
        Assert.Equal(original.PngBytes, StillLifeRenderer.Render(scene).PngBytes);
    }

    [Theory]
    [MemberData(nameof(Dimensions))]
    public void AllCombinationsRenderAtSupportedSquareAndRectangularSizes(string prompt, int width, int height)
    {
        var scene = Scene(prompt, 1337, 1, width, height);
        var image = StillLifeRenderer.Render(scene);
        Assert.Equal(width, image.Width);
        Assert.Equal(height, image.Height);
        Assert.NotEmpty(image.PngBytes);
        using var bitmap = Decode(image.PngBytes);
        Assert.Equal(width, bitmap.Width);
        Assert.Equal(height, bitmap.Height);
        if (Math.Min(width, height) >= Cell)
        {
            var pair = scene.VaseTulipDaisy!;
            using var empty = Decode(StillLifeRenderer.Render(EmptyVase(scene)).PngBytes);
            AssertStemContinuousThroughMouth(bitmap, empty, pair, pair.Tulip, $"{width}x{height} tulip", width, height);
            AssertStemContinuousThroughMouth(bitmap, empty, pair, pair.Daisy, $"{width}x{height} daisy", width, height);
        }
    }

    [Theory]
    [MemberData(nameof(ReviewCases))]
    public void ExportVaseTulipDaisySamplesAndTimingSidecars(
        string prompt,
        string name,
        string color,
        string glaze,
        int seed,
        double variation)
    {
        var scene = Scene(prompt, (ulong)seed, variation);
        Assert.Equal($"{color}-tulip-and-white-daisy-in-{glaze}-vase", scene.SubjectToken);
        var timer = Stopwatch.StartNew();
        var rendered = StillLifeRenderer.Render(scene);
        timer.Stop();
        Assert.Equal(Canvas, rendered.Width);
        Assert.Equal(Canvas, rendered.Height);

        var stem = SampleStem(name, seed, variation);
        var keep = ExportArtifacts.Keeps(stem, Canonical, RetainedReviewStems);
        var directory = AppContext.BaseDirectory;
        var pair = scene.VaseTulipDaisy!;
        var emptyTimer = Stopwatch.StartNew();
        var empty = StillLifeRenderer.Render(EmptyVase(scene));
        emptyTimer.Stop();
        using var plantedBitmap = Decode(rendered.PngBytes);
        using var emptyBitmap = Decode(empty.PngBytes);
        AssertStemContinuousThroughMouth(plantedBitmap, emptyBitmap, pair, pair.Tulip, name + " tulip");
        AssertStemContinuousThroughMouth(plantedBitmap, emptyBitmap, pair, pair.Daisy, name + " daisy");

        var emptyMilliseconds = (double?)null;
        var tulipMilliseconds = (double?)null;
        var daisyMilliseconds = (double?)null;
        var emptyBytes = (int?)null;
        var tulipBytes = (int?)null;
        var daisyBytes = (int?)null;
        if (name == "red-blue")
        {
            emptyMilliseconds = emptyTimer.Elapsed.TotalMilliseconds;
            emptyBytes = empty.PngBytes.Length;

            var tulipScene = Scene(
                $"a {color} tulip in a {glaze} ceramic vase on a wooden table",
                (ulong)seed,
                variation);
            var tulipTimer = Stopwatch.StartNew();
            var tulip = StillLifeRenderer.Render(tulipScene);
            tulipTimer.Stop();
            tulipMilliseconds = tulipTimer.Elapsed.TotalMilliseconds;
            tulipBytes = tulip.PngBytes.Length;

            var daisyScene = Scene(
                $"a white daisy in a {glaze} ceramic vase on a wooden table",
                (ulong)seed,
                variation);
            var daisyTimer = Stopwatch.StartNew();
            var daisy = StillLifeRenderer.Render(daisyScene);
            daisyTimer.Stop();
            daisyMilliseconds = daisyTimer.Elapsed.TotalMilliseconds;
            daisyBytes = daisy.PngBytes.Length;
        }

        if (keep)
        {
            File.WriteAllBytes(Path.Combine(directory, stem + ".png"), rendered.PngBytes);
            var silhouette = StillLifeRenderer.RenderSilhouette(scene);
            File.WriteAllBytes(Path.Combine(directory, stem + "-silhouette.png"), silhouette.PngBytes);
            var report = JsonSerializer.Serialize(
                new
                {
                    Prompt = prompt,
                    Subject = scene.SubjectToken,
                    TulipColor = pair.TulipColor,
                    DaisyAppearance = pair.DaisyAppearance,
                    VaseKind = pair.Vase.Kind,
                    VaseGlaze = pair.Vase.Glaze,
                    OpeningCenter = pair.OpeningCenter,
                    OpeningRadius = pair.OpeningRadius,
                    MouthTop = pair.MouthTop,
                    Tulip = new
                    {
                        pair.Tulip.StemBase,
                        pair.Tulip.StemTop,
                        pair.Tulip.StemThickness,
                        pair.Tulip.Bloom.Width,
                        pair.Tulip.Bloom.Height,
                        Leaves = pair.Tulip.Leaves.Length,
                        Petals = pair.Tulip.Bloom.Petals.Length,
                        pair.Tulip.Bounds,
                    },
                    Daisy = new
                    {
                        pair.Daisy.StemBase,
                        pair.Daisy.StemTop,
                        pair.Daisy.StemThickness,
                        pair.Daisy.Bloom.RadiusX,
                        pair.Daisy.Bloom.RadiusY,
                        DiskRadiusX = pair.Daisy.Bloom.Disk.RadiusX,
                        DiskRadiusY = pair.Daisy.Bloom.Disk.RadiusY,
                        Leaves = pair.Daisy.Leaves.Length,
                        Petals = pair.Daisy.Bloom.Petals.Length,
                        pair.Daisy.Bounds,
                    },
                    CombinedBounds = pair.Bounds,
                    rendered.Width,
                    rendered.Height,
                    Framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                    OperatingSystem = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                    scene.Seed,
                    scene.Variation,
                    RenderAndEncodeMilliseconds = timer.Elapsed.TotalMilliseconds,
                    EmptyVaseRenderAndEncodeMilliseconds = emptyMilliseconds,
                    TulipRenderAndEncodeMilliseconds = tulipMilliseconds,
                    DaisyRenderAndEncodeMilliseconds = daisyMilliseconds,
                    ProcessorCount = Environment.ProcessorCount,
                    RenderedBytes = rendered.PngBytes.Length,
                    EmptyVaseBytes = emptyBytes,
                    TulipBytes = tulipBytes,
                    DaisyBytes = daisyBytes,
                },
                new JsonSerializerOptions { IncludeFields = true });
            File.WriteAllText(Path.Combine(directory, stem + ".timing.json"), report);
            Assert.True(File.Exists(Path.Combine(directory, stem + ".png")));
        }
    }

    [Fact]
    public void ExportFourCombinationAndVariationReviewSheetWithManifest()
    {
        const int columns = 4;
        var rows = (int)Math.Ceiling(ReviewSamples.Length / (double)columns);
        using var sheet = new SKBitmap(Cell * columns, Cell * rows);
        using var canvas = new SKCanvas(sheet);
        canvas.Clear(new SKColor(20, 22, 24));
        var manifest = new List<object>();

        for (var index = 0; index < ReviewSamples.Length; index++)
        {
            var sample = ReviewSamples[index];
            var scene = Scene(sample.Prompt, (ulong)sample.Seed, sample.Variation, Cell, Cell);
            var timer = Stopwatch.StartNew();
            var rendered = StillLifeRenderer.Render(scene);
            timer.Stop();
            using var cellBitmap = Decode(rendered.PngBytes);
            using var empty = Decode(StillLifeRenderer.Render(EmptyVase(scene)).PngBytes);
            var pair = scene.VaseTulipDaisy!;
            AssertSmallCellReadability(cellBitmap, empty, pair, sample.Name);

            var left = (index % columns) * Cell;
            var top = (index / columns) * Cell;
            canvas.DrawBitmap(
                cellBitmap,
                new SKRect(left, top, left + Cell, top + Cell),
                new SKSamplingOptions(SKFilterMode.Linear));
            manifest.Add(new
            {
                sample.Name,
                sample.Prompt,
                TulipColor = pair.TulipColor.ToString(),
                DaisyAppearance = pair.DaisyAppearance.ToString(),
                VaseGlaze = pair.Vase.Glaze.ToString(),
                sample.Seed,
                sample.Variation,
                Subject = scene.SubjectToken,
                Petals = new { Tulip = pair.Tulip.Bloom.Petals.Length, Daisy = pair.Daisy.Bloom.Petals.Length },
                Leaves = new { Tulip = pair.Tulip.Leaves.Length, Daisy = pair.Daisy.Leaves.Length },
                FileName = SampleStem(sample.Name, sample.Seed, sample.Variation) + ".png",
                Cell = index,
                Column = index % columns,
                Row = index / columns,
                Width = rendered.Width,
                Height = rendered.Height,
                RenderAndEncodeMilliseconds = timer.Elapsed.TotalMilliseconds,
            });
        }

        var directory = AppContext.BaseDirectory;
        using var image = SKImage.FromBitmap(sheet);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(Path.Combine(directory, "formloom-vase-tulip-daisy-sheet.png"), encoded.ToArray());
        File.WriteAllText(
            Path.Combine(directory, "formloom-vase-tulip-daisy-sheet.timing.json"),
            JsonSerializer.Serialize(
                new
                {
                    Sheet = "formloom-vase-tulip-daisy-sheet.png",
                    Cell,
                    Columns = columns,
                    Rows = rows,
                    Generated = DateTimeOffset.UtcNow,
                    Layout = "row-major; first row is red/blue, red/cream, yellow/blue, yellow/cream at seed 42 and variation 0.35; following rows show alternate seeds and variation extremes",
                    Cases = manifest,
                },
                new JsonSerializerOptions { IncludeFields = true }));
        Assert.Equal(ReviewSamples.Length, manifest.Count);
        Assert.Equal(Cell * columns, sheet.Width);
        Assert.Equal(Cell * rows, sheet.Height);
        Assert.True(File.Exists(Path.Combine(directory, "formloom-vase-tulip-daisy-sheet.png")));
    }

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

    private static StillLifeScene EmptyVase(StillLifeScene scene) => scene with { VaseTulipDaisy = null };

    private static void AssertSmallCellReadability(SKBitmap planted, SKBitmap empty, VaseTulipDaisy pair, string name)
    {
        Assert.True(
            Coloured(planted, empty, TulipBloomWindow(pair.Tulip, 2, Cell, Cell), pixel => IsTulipPetal(pixel, pair.TulipColor)) > 45,
            $"{name} has no readable tulip colour at {Cell}px");
        Assert.True(
            Coloured(planted, empty, DaisyBloomWindow(pair.Daisy, 2, Cell, Cell), IsPetalWhite) > 45,
            $"{name} has no readable daisy petals at {Cell}px");
        Assert.True(
            Coloured(planted, empty, DiskWindow(pair.Daisy.Bloom.Disk, 2, Cell, Cell), IsDiskGold) > 6,
            $"{name} has no readable disk at {Cell}px");
        foreach (var leaf in pair.Tulip.Leaves)
        {
            Assert.True(
                Coloured(planted, empty, LeafWindow(leaf, 2, Cell, Cell), IsLeafGreen) > 1,
                $"{name} lost a tulip leaf at {Cell}px");
        }

        foreach (var leaf in pair.Daisy.Leaves)
        {
            Assert.True(
                Coloured(planted, empty, LeafWindow(leaf, 2, Cell, Cell), IsLeafGreen) > 1,
                $"{name} lost a daisy leaf at {Cell}px");
        }

        AssertStemVisibleAboveRim(planted, empty, pair.Tulip, pair.MouthTop, name + " tulip", Cell, Cell);
        AssertStemVisibleAboveRim(planted, empty, pair.Daisy, pair.MouthTop, name + " daisy", Cell, Cell);
        AssertStemContinuousThroughMouth(planted, empty, pair, pair.Tulip, name + " tulip", Cell, Cell);
        AssertStemContinuousThroughMouth(planted, empty, pair, pair.Daisy, name + " daisy", Cell, Cell);
    }

    private static void AssertStemVisibleAboveRim(
        SKBitmap planted,
        SKBitmap empty,
        TulipPlant plant,
        float mouthTop,
        string name,
        int width = Canvas,
        int height = Canvas) =>
        AssertStemVisibleAboveRimCore(planted, empty, plant.Stem, plant.StemTop.Y, mouthTop, name, width, height);

    private static void AssertStemVisibleAboveRim(
        SKBitmap planted,
        SKBitmap empty,
        DaisyPlant plant,
        float mouthTop,
        string name,
        int width = Canvas,
        int height = Canvas) =>
        AssertStemVisibleAboveRimCore(planted, empty, plant.Stem, plant.StemTop.Y, mouthTop, name, width, height);

    private static void AssertStemVisibleAboveRimCore(
        SKBitmap planted,
        SKBitmap empty,
        ImmutableArray<CubicSegment> segments,
        float stemTop,
        float mouthTop,
        string name,
        int width,
        int height)
    {
        var first = Math.Max(0, (int)MathF.Floor(stemTop * height));
        var last = Math.Min(height - 1, (int)MathF.Ceiling(mouthTop * height));
        var visibleRows = 0;
        for (var y = first; y <= last; y++)
        {
            var x = StemColumn(segments, (y + 0.5f) / height, width);
            var covered = false;
            for (var offset = -2; offset <= 2; offset++)
            {
                var column = x + offset;
                if (column >= 0 && column < width && Delta(planted.GetPixel(column, y), empty.GetPixel(column, y)) > 0)
                {
                    covered = true;
                }
            }

            if (covered)
            {
                visibleRows++;
            }
        }

        var expectedRows = Math.Max(5, (last - first + 1) / 3);
        Assert.True(visibleRows >= expectedRows, $"{name} stem is not visible above the rim: {visibleRows}/{last - first + 1} rows");
    }

    private static void AssertStemContinuousThroughMouth(
        SKBitmap planted,
        SKBitmap empty,
        VaseTulipDaisy pair,
        TulipPlant plant,
        string name,
        int width = Canvas,
        int height = Canvas) =>
        AssertStemContinuousThroughMouthCore(
            planted,
            empty,
            plant.Stem,
            plant.StemBase,
            plant.StemThickness,
            pair.OpeningCenter,
            pair.OpeningRadius,
            pair.MouthTop,
            name,
            width,
            height);

    private static void AssertStemContinuousThroughMouth(
        SKBitmap planted,
        SKBitmap empty,
        VaseTulipDaisy pair,
        DaisyPlant plant,
        string name,
        int width = Canvas,
        int height = Canvas) =>
        AssertStemContinuousThroughMouthCore(
            planted,
            empty,
            plant.Stem,
            plant.StemBase,
            plant.StemThickness,
            pair.OpeningCenter,
            pair.OpeningRadius,
            pair.MouthTop,
            name,
            width,
            height);

    private static void AssertStemContinuousThroughMouthCore(
        SKBitmap planted,
        SKBitmap empty,
        ImmutableArray<CubicSegment> segments,
        Vector2 stemBase,
        float thickness,
        Vector2 openingCenter,
        Vector2 openingRadius,
        float mouthTop,
        string name,
        int width,
        int height)
    {
        var extension = InteriorStemExtension(segments, stemBase, thickness, openingCenter, openingRadius);
        var completeStem = segments.Add(extension);
        var baseTangent = stemBase - segments[0].Control1;
        var continuationTangent = extension.Control1 - extension.Start;
        Assert.True(
            Vector2.Dot(Vector2.Normalize(baseTangent), Vector2.Normalize(continuationTangent)) > 0.999f,
            $"{name} interior continuation does not follow the stem's base tangent");

        var lipInner = LipInnerRow(empty, stemBase.X, openingCenter, openingRadius);
        var first = Math.Max(0, (int)MathF.Floor((mouthTop - 0.02f) * height));
        var last = Math.Min(height - 1, lipInner - 2);
        var pixelReach = Math.Max(2, (int)MathF.Ceiling(thickness * width / 2) + 2);
        Assert.True(last >= first, $"{name} has no pixels available for the mouth-continuity check");

        for (var y = first; y <= last; y++)
        {
            var expectedX = StemColumn(completeStem, (y + 0.5f) / height, width);
            var visible = false;
            for (var offset = -pixelReach; offset <= pixelReach; offset++)
            {
                var x = expectedX + offset;
                if (x < 0 || x >= width || Delta(planted.GetPixel(x, y), empty.GetPixel(x, y)) == 0)
                {
                    continue;
                }

                if ((y + 0.5f) / height > mouthTop + 0.01f)
                {
                    Assert.True(
                        InsideEllipse(openingCenter, openingRadius, x, y, width, height, 2),
                        $"{name} stem leaked onto the vase wall at {x},{y}");
                }

                visible = true;
                break;
            }

            Assert.True(visible, $"{name} stem has a gap at its vase contact or inside the opening on row {y}");
        }

        if (width >= 4096)
        {
            var probeY = (int)MathF.Round((stemBase.Y + ((extension.End.Y - stemBase.Y) * 0.36f)) * height);
            var expectedCenter = StemColumn(completeStem, (probeY + 0.5f) / height, width);
            var weightedX = 0d;
            var totalWeight = 0d;
            for (var x = expectedCenter - pixelReach; x <= expectedCenter + pixelReach; x++)
            {
                if (x >= 0 && x < width)
                {
                    var delta = Delta(planted.GetPixel(x, probeY), empty.GetPixel(x, probeY));
                    weightedX += (x + 0.5d) * delta;
                    totalWeight += delta;
                }
            }

            Assert.True(totalWeight > 0, $"{name} tangent probe is not visible at {probeY}");
            var renderedCenter = weightedX / totalWeight;
            Assert.True(
                Math.Abs(renderedCenter - expectedCenter) <= 2.5,
                $"{name} interior continuation turns abruptly: expected center {expectedCenter}, rendered {renderedCenter:0.0}");
        }

        var buriedY = Math.Min(height - 1, lipInner + 3);
        var buriedX = (int)MathF.Round(stemBase.X * width);
        for (var offset = -pixelReach; offset <= pixelReach; offset++)
        {
            var x = buriedX + offset;
            if (x >= 0 && x < width)
            {
                Assert.Equal(0, Delta(planted.GetPixel(x, buriedY), empty.GetPixel(x, buriedY)));
            }
        }
    }

    // Mirror the renderer's tangent-matched mouth extension so row checks follow the expected
    // curve below StemBase instead of assuming an artificial vertical stub.
    private static CubicSegment InteriorStemExtension(
        ImmutableArray<CubicSegment> segments,
        Vector2 stemBase,
        float thickness,
        Vector2 openingCenter,
        Vector2 openingRadius)
    {
        var end = new Vector2(stemBase.X, openingCenter.Y + openingRadius.Y + (thickness * 3));
        var length = end.Y - stemBase.Y;
        var tangent = stemBase - segments[0].Control1;
        var downward = tangent.LengthSquared() > 1e-10f ? Vector2.Normalize(tangent) : Vector2.UnitY;
        return new(
            stemBase,
            stemBase + (downward * (length * 0.4f)),
            end - new Vector2(0, length * 0.3f),
            end);
    }

    private static void AssertStemContinuationInsideOpening(
        SKBitmap planted,
        SKBitmap empty,
        VaseTulipDaisy pair,
        TulipPlant plant,
        string name) =>
        AssertStemContinuationInsideOpeningCore(planted, empty, pair.OpeningCenter, pair.OpeningRadius, pair.MouthTop, plant.Stem, plant.StemBase, name);

    private static void AssertStemContinuationInsideOpening(
        SKBitmap planted,
        SKBitmap empty,
        VaseTulipDaisy pair,
        DaisyPlant plant,
        string name) =>
        AssertStemContinuationInsideOpeningCore(planted, empty, pair.OpeningCenter, pair.OpeningRadius, pair.MouthTop, plant.Stem, plant.StemBase, name);

    private static void AssertStemContinuationInsideOpeningCore(
        SKBitmap planted,
        SKBitmap empty,
        Vector2 openingCenter,
        Vector2 openingRadius,
        float mouthTop,
        ImmutableArray<CubicSegment> segments,
        Vector2 stemBase,
        string name)
    {
        var first = Math.Max(0, (int)MathF.Floor(mouthTop * Canvas));
        var last = Math.Min(Canvas - 1, (int)MathF.Ceiling(stemBase.Y * Canvas));
        var visibleRows = 0;
        for (var y = first; y <= last; y++)
        {
            var x = StemColumn(segments, (y + 0.5f) / Canvas, Canvas);
            var inside = false;
            for (var offset = -2; offset <= 2; offset++)
            {
                var column = x + offset;
                if (column >= 0 && column < Canvas &&
                    InsideEllipse(openingCenter, openingRadius, column, y, Canvas, Canvas, 2) &&
                    Delta(planted.GetPixel(column, y), empty.GetPixel(column, y)) > 0)
                {
                    inside = true;
                }
            }

            if (inside)
            {
                visibleRows++;
            }
        }

        Assert.True(visibleRows >= 8, $"{name} continuation is missing inside the opening: {visibleRows} rows");
        var emptyLip = LipInnerRow(empty, stemBase.X, openingCenter, openingRadius);
        var buriedY = Math.Min(Canvas - 1, emptyLip + 3);
        var buriedX = (int)MathF.Round(stemBase.X * Canvas);
        for (var offset = -2; offset <= 2; offset++)
        {
            Assert.Equal(0, Delta(planted.GetPixel(buriedX + offset, buriedY), empty.GetPixel(buriedX + offset, buriedY)));
        }
    }

    private static void AssertChangedPixelsStayOnThePlantsOrInsideTheMouth(
        SKBitmap planted,
        SKBitmap empty,
        VaseTulipDaisy pair,
        string name)
    {
        var plantWindow = PlantWindow(pair, 4);
        for (var y = 0; y < Canvas; y++)
        {
            for (var x = 0; x < Canvas; x++)
            {
                if (Delta(planted.GetPixel(x, y), empty.GetPixel(x, y)) <= 8)
                {
                    continue;
                }

                if (!Inside(plantWindow, x, y, Canvas, Canvas) &&
                    !InsideEllipse(pair.OpeningCenter, pair.OpeningRadius, x, y, Canvas, Canvas, 2) &&
                    !InsideMouthContinuationColumn(pair, x, y))
                {
                    Assert.Fail($"{name} changed a pixel outside the plants and intentional mouth continuation at {x},{y}");
                }
            }
        }
    }

    private static bool InsideMouthContinuationColumn(VaseTulipDaisy pair, int x, int y)
    {
        var px = (x + 0.5f) / Canvas;
        var py = (y + 0.5f) / Canvas;
        if (py < pair.MouthTop - 0.03f || py > pair.OpeningCenter.Y)
        {
            return false;
        }

        var reach = (MathF.Max(pair.Tulip.StemThickness, pair.Daisy.StemThickness) * 2.5f) + (2f / Canvas);
        return MathF.Abs(px - pair.Tulip.StemBase.X) <= reach || MathF.Abs(px - pair.Daisy.StemBase.X) <= reach;
    }

    private static void AssertStemFitsOpening(Vector2 stemBase, float thickness, Vector2 center, Vector2 radius)
    {
        var x = (MathF.Abs(stemBase.X - center.X) + (thickness / 2)) / radius.X;
        var y = MathF.Abs(stemBase.Y - center.Y) / radius.Y;
        Assert.True((x * x) + (y * y) < 0.95f);
    }

    private static Window TulipBloomWindow(TulipPlant plant, int grow, int width = Canvas, int height = Canvas)
    {
        var bounds = Bounds(plant.Bloom.Petals.SelectMany(petal => petal.Outline));
        return Grow(bounds, grow, width, height);
    }

    private static Window DaisyBloomWindow(DaisyPlant plant, int grow, int width = Canvas, int height = Canvas)
    {
        var bounds = Bounds(plant.Bloom.Petals.SelectMany(petal => petal.Outline));
        var disk = plant.Bloom.Disk;
        bounds = Union(bounds, new ProfileExtent(
            disk.Center.X - disk.RadiusX,
            disk.Center.Y - disk.RadiusY,
            disk.Center.X + disk.RadiusX,
            disk.Center.Y + disk.RadiusY));
        return Grow(bounds, grow, width, height);
    }

    private static Window DiskWindow(DaisyDisk disk, int grow, int width = Canvas, int height = Canvas) => Grow(
        new ProfileExtent(
            disk.Center.X - disk.RadiusX,
            disk.Center.Y - disk.RadiusY,
            disk.Center.X + disk.RadiusX,
            disk.Center.Y + disk.RadiusY),
        grow,
        width,
        height);

    private static Window LeafWindow(TulipLeaf leaf, int grow, int width = Canvas, int height = Canvas) => LeafWindow(leaf.Attachment, leaf.Tip, grow, width, height);

    private static Window LeafWindow(DaisyLeaf leaf, int grow, int width = Canvas, int height = Canvas) => LeafWindow(leaf.Attachment, leaf.Tip, grow, width, height);

    private static Window LeafWindow(Vector2 attachment, Vector2 tip, int grow, int width, int height)
    {
        var middle = (attachment + tip) / 2;
        var reach = 0.012f + (grow / (float)Math.Min(width, height));
        return new(middle.X - reach, middle.Y - reach, middle.X + reach, middle.Y + reach);
    }

    private static Window PlantWindow(VaseTulipDaisy pair, int grow) => Grow(
        new ProfileExtent(pair.Bounds.Left, pair.Bounds.Top, pair.Bounds.Right, pair.Bounds.Bottom),
        grow,
        Canvas,
        Canvas);

    private static Window Grow(ProfileExtent bounds, int grow, int width, int height) => new(
        bounds.Left - (grow / (float)width),
        bounds.Top - (grow / (float)height),
        bounds.Right + (grow / (float)width),
        bounds.Bottom + (grow / (float)height));

    private static ProfileExtent Bounds(IEnumerable<CubicSegment> segments)
    {
        var extents = segments.ToArray();
        return CurveBuilder.Extent(ImmutableArray.CreateRange(extents));
    }

    private static ProfileExtent Union(ProfileExtent first, ProfileExtent second) => new(
        MathF.Min(first.Left, second.Left),
        MathF.Min(first.Top, second.Top),
        MathF.Max(first.Right, second.Right),
        MathF.Max(first.Bottom, second.Bottom));

    private static bool Inside(Window window, int x, int y, int width, int height)
    {
        var px = (x + 0.5f) / width;
        var py = (y + 0.5f) / height;
        return px >= window.Left && px <= window.Right && py >= window.Top && py <= window.Bottom;
    }

    private static int CountChanged(SKBitmap first, SKBitmap second, Window window)
    {
        var left = Math.Max(0, (int)MathF.Floor(window.Left * first.Width));
        var right = Math.Min(first.Width - 1, (int)MathF.Ceiling(window.Right * first.Width));
        var top = Math.Max(0, (int)MathF.Floor(window.Top * first.Height));
        var bottom = Math.Min(first.Height - 1, (int)MathF.Ceiling(window.Bottom * first.Height));
        var count = 0;
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

    private static int Coloured(SKBitmap planted, SKBitmap empty, Window window, Func<SKColor, bool> predicate)
    {
        var left = Math.Max(0, (int)MathF.Floor(window.Left * planted.Width));
        var right = Math.Min(planted.Width - 1, (int)MathF.Ceiling(window.Right * planted.Width));
        var top = Math.Max(0, (int)MathF.Floor(window.Top * planted.Height));
        var bottom = Math.Min(planted.Height - 1, (int)MathF.Ceiling(window.Bottom * planted.Height));
        var count = 0;
        for (var y = top; y <= bottom; y++)
        {
            for (var x = left; x <= right; x++)
            {
                var pixel = planted.GetPixel(x, y);
                if (Delta(pixel, empty.GetPixel(x, y)) > 0 && predicate(pixel))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static int StemColumn(ImmutableArray<CubicSegment> segments, float y, int width)
    {
        var bestDistance = float.MaxValue;
        var bestX = 0.5f;
        foreach (var segment in segments)
        {
            for (var step = 0; step <= 64; step++)
            {
                var point = segment.Evaluate(step / 64f);
                var distance = MathF.Abs(point.Y - y);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestX = point.X;
                }
            }
        }

        return (int)MathF.Round(bestX * width);
    }

    private static int LipInnerRow(SKBitmap empty, float x, Vector2 center, Vector2 radius)
    {
        var column = Math.Clamp((int)MathF.Round(x * empty.Width), 0, empty.Width - 1);
        var centerRow = Math.Clamp((int)MathF.Round(center.Y * empty.Height), 0, empty.Height - 1);
        var bottom = Math.Clamp((int)MathF.Round((center.Y + radius.Y) * empty.Height), 0, empty.Height - 1);
        var row = bottom;
        var best = 0;
        var previous = Brightness(empty.GetPixel(column, centerRow));
        for (var y = centerRow + 1; y <= bottom; y++)
        {
            var here = Brightness(empty.GetPixel(column, y));
            if (here - previous > best)
            {
                best = here - previous;
                row = y;
            }

            previous = here;
        }

        return row;
    }

    private static bool InsideEllipse(Vector2 center, Vector2 radius, int x, int y, int width, int height, int grow)
    {
        var radiusX = radius.X + (grow / (float)width);
        var radiusY = radius.Y + (grow / (float)height);
        var dx = ((x + 0.5f) / width) - center.X;
        var dy = ((y + 0.5f) / height) - center.Y;
        return ((dx * dx) / (radiusX * radiusX)) + ((dy * dy) / (radiusY * radiusY)) <= 1f;
    }

    private static bool IsTulipPetal(SKColor pixel, TulipColor color) => color == TulipColor.Red
        ? pixel.Red > pixel.Green + 40 && pixel.Red > pixel.Blue + 25
        : pixel.Red > 150 && pixel.Green > 105 && pixel.Blue < 115 && pixel.Red > pixel.Green;

    private static bool IsPetalWhite(SKColor pixel) =>
        pixel.Red > 195 && pixel.Green > 195 && pixel.Blue > 185 && Math.Abs(pixel.Red - pixel.Blue) < 24;

    private static bool IsLeafGreen(SKColor pixel) =>
        pixel.Green > 85 && pixel.Green > pixel.Red + 12 && pixel.Green > pixel.Blue + 12;

    private static bool IsDiskGold(SKColor pixel) =>
        pixel.Red > 150 && pixel.Green > 110 && pixel.Blue < 120 && pixel.Red > pixel.Green && pixel.Green > pixel.Blue;

    private static int Brightness(SKColor pixel) => (pixel.Red + pixel.Green + pixel.Blue) / 3;

    private static SKBitmap Decode(byte[] png) =>
        SKBitmap.Decode(png) ?? throw new InvalidOperationException("The renderer produced an undecodable PNG.");

    private static int Delta(SKColor first, SKColor second) => Math.Max(
        Math.Max(Math.Abs(first.Red - second.Red), Math.Abs(first.Green - second.Green)),
        Math.Max(Math.Abs(first.Blue - second.Blue), Math.Abs(first.Alpha - second.Alpha)));

    private static string SampleStem(string name, int seed, double variation) =>
        FormattableString.Invariant($"formloom-vase-tulip-daisy-{name}-seed-{seed}-variation-{variation:0.00}");

    private static string SampleStem(Sample sample) => SampleStem(sample.Name, sample.Seed, sample.Variation);

    private readonly record struct Sample(
        string Prompt,
        string Name,
        TulipColor TulipColor,
        CeramicGlaze Glaze,
        int Seed,
        double Variation);

    private readonly record struct Window(float Left, float Top, float Right, float Bottom);
}
