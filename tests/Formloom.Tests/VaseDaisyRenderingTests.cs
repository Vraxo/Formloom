using System.Diagnostics;
using Formloom.Core;
using Formloom.Rendering.Skia;
using SkiaSharp;
using Xunit;

namespace Formloom.Tests;

public sealed class VaseDaisyRenderingTests
{
    // Everything in the rim band the stem passes behind, in pixels at the 1024 reference size.
    private const int RimBand = 14;

    private const int Canvas = 1024;

    // The canonical case is kept on every run; the rest follow with FORMFLOOM_EXPORT_ALL=1.
    private const string CanonicalDaisy = "formloom-vasedaisy-white-blue-seed-42-variation-0.35";

    private static readonly (string Prompt, string Name, int Seed, double Variation)[] Cases =
    [
        ("a white daisy in a blue ceramic vase on a wooden table", "white-blue", 42, 0.35),
        ("a white daisy in a cream ceramic vase on a wooden table", "white-cream", 42, 0.35),
        ("a white daisy in a blue ceramic vase on a wooden table", "white-blue-altseed", 1337, 0.35),
        ("a white daisy in a cream ceramic vase on a wooden table", "white-cream-altseed", 20261105, 0.35),
        ("a white daisy in a blue ceramic vase on a wooden table", "white-blue-lowvariation", 42, 0),
        ("a white daisy in a cream ceramic vase on a wooden table", "white-cream-highvariation", 42, 1),
        ("a white daisy in a blue ceramic vase on a wooden table", "white-blue-highvariation", 1337, 1),
        ("a white daisy in a cream ceramic vase on a wooden table", "white-cream-lowvariation", 20261105, 0),
    ];

    public static TheoryData<string, string, string, int, double> ExportCases()
    {
        var data = new TheoryData<string, string, string, int, double>();
        foreach (var (prompt, name, seed, variation) in Cases)
        {
            data.Add(prompt, name, name.Split('-')[1], seed, variation);
        }

        return data;
    }

    public static TheoryData<int, int> Dimensions()
    {
        var data = new TheoryData<int, int>();
        foreach (var (width, height) in new[] { (64, 64), (256, 256), (640, 480), (2048, 1024), (4096, 1024) })
        {
            data.Add(width, height);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ExportCases))]
    public void EveryCombinationRendersAndKeepsTheVaseIntact(string prompt, string name, string glaze, int seed, double variation)
    {
        var scene = Scene(prompt, (ulong)seed, variation);
        var daisy = scene.VaseDaisy!;
        var plant = daisy.Plant;
        var image = StillLifeRenderer.Render(scene);
        Assert.Equal(Canvas, image.Width);
        Assert.Equal(Canvas, image.Height);
        Assert.Equal($"white-daisy-in-{glaze}-vase", image.Subject);
        Assert.Equal(image.PngBytes, StillLifeRenderer.Render(scene).PngBytes);

        using var planted = Decode(image.PngBytes);
        using var empty = Decode(StillLifeRenderer.Render(EmptyVase(scene)).PngBytes);
        var rim = (int)MathF.Ceiling(daisy.MouthTop * Canvas);
        var changed = 0;
        var belowTheRim = 0;
        var insideTheOpening = 0;
        for (var y = 0; y < Canvas; y++)
        {
            for (var x = 0; x < Canvas; x++)
            {
                if (Delta(planted.GetPixel(x, y), empty.GetPixel(x, y)) == 0)
                {
                    continue;
                }

                changed++;
                if (y > rim)
                {
                    if (InsideOpening(daisy, x, y, 2f))
                    {
                        insideTheOpening++;
                    }
                    else
                    {
                        belowTheRim++;
                    }
                }
            }
        }

        // The daisy is drawn, the stem is visibly carried into the opening, and the vase, the
        // table and the background below the rim are byte-identical to the same vase without a
        // flower everywhere except the mouth's own opening.
        Assert.True(changed > 400, $"{name} drew almost nothing: {changed} changed pixels");
        Assert.Equal(0, belowTheRim);
        Assert.True(insideTheOpening > 60, $"{name} never entered the opening: {insideTheOpening} pixels");

        // Everything the daisy changed above the rim band lies inside its own measured bounds: it
        // cannot leak into the tabletop or the far background.
        var bounds = BoundsWindow(plant, 3);
        for (var y = 0; y <= rim - RimBand; y++)
        {
            for (var x = 0; x < Canvas; x++)
            {
                if (Delta(planted.GetPixel(x, y), empty.GetPixel(x, y)) != 0 && !Inside(bounds, x, y))
                {
                    Assert.Fail($"{name} changed a pixel outside the daisy at {x},{y}");
                }
            }
        }

        // The head fills its box, both leaves flank the stem and the stem reaches the opening.
        Assert.True(Count(planted, empty, BloomWindow(plant, 0)) > 1200, $"{name} lost its head");
        foreach (var leaf in plant.Leaves)
        {
            Assert.True(Count(planted, empty, LeafWindow(leaf)) > 40, $"{name} lost a leaf");
        }

        Assert.True(Count(planted, empty, StemWindow(daisy)) > 40, $"{name} lost the stem above the rim");
    }

    [Fact]
    public void TheHeadIsWhiteWithAGoldenDiskAboveTheRim()
    {
        foreach (var (prompt, _, seed, variation) in Cases)
        {
            var scene = Scene(prompt, (ulong)seed, variation);
            var daisy = scene.VaseDaisy!;
            var plant = daisy.Plant;
            using var planted = Decode(StillLifeRenderer.Render(scene).PngBytes);
            using var empty = Decode(StillLifeRenderer.Render(EmptyVase(scene)).PngBytes);

            // The petals read as white rather than as a grey or cream wash: bright and close to
            // neutral, counted only where the daisy actually painted over the empty vase.
            var head = BloomWindow(plant, 0);
            var petals = Coloured(planted, empty, head, IsPetalWhite);
            Assert.True(petals > 1500, $"{prompt} does not read as white: {petals} petal pixels");

            // The centre is golden, drawn over the petal roots.
            var disk = DiskWindow(plant.Bloom);
            var gold = Coloured(planted, empty, disk, IsDiskGold);
            Assert.True(gold > 260, $"{prompt} lost its golden centre: {gold} disk pixels");

            // Nothing of the head is cut by the vase. The warm tabletop and the glaze highlights
            // are neither white petals nor a golden disk once the comparison is made against the
            // same empty vase, and the only spot below the mouth where the daisy may legitimately
            // change pixels is the opening, where its stem is carried under the near lip. So the
            // scan runs below the mouth, outside the opening, and starts below the shared clip's
            // own 0.002 overshoot at the tangent.
            var firstRow = (int)MathF.Ceiling(daisy.MouthTop * Canvas) + 2;
            var horizon = (int)MathF.Floor((float)scene.TableHorizon * Canvas);
            var leaked = 0;
            var firstOffender = string.Empty;
            for (var y = firstRow; y < horizon; y++)
            {
                for (var x = 0; x < Canvas; x++)
                {
                    var pixel = planted.GetPixel(x, y);
                    if (Delta(pixel, empty.GetPixel(x, y)) == 0 || InsideOpening(daisy, x, y, 2f))
                    {
                        continue;
                    }

                    if (IsPetalWhite(pixel) || IsDiskGold(pixel))
                    {
                        leaked++;
                        if (firstOffender.Length == 0)
                        {
                            firstOffender = $" first at {x},{y} = {pixel}";
                        }
                    }
                }
            }

            Assert.True(leaked == 0, $"{prompt} leaked {leaked} head pixels below the mouth{firstOffender}");
        }
    }

    private static bool IsPetalWhite(SKColor pixel) =>
        pixel.Red > 195 && pixel.Green > 195 && pixel.Blue > 185 && Math.Abs(pixel.Red - pixel.Blue) < 24;

    private static bool IsDiskGold(SKColor pixel) =>
        pixel.Red > 150 && pixel.Green > 110 && pixel.Blue < 120 && pixel.Red > pixel.Green && pixel.Green > pixel.Blue;

    [Theory]
    [MemberData(nameof(ExportCases))]
    public void TheStemEntersTheOpeningContinuouslyAndReachesTheNearLip(string prompt, string name, string glaze, int seed, double variation)
    {
        var scene = Scene(prompt, (ulong)seed, variation);
        var daisy = scene.VaseDaisy!;
        var image = StillLifeRenderer.Render(scene);
        Assert.Equal($"white-daisy-in-{glaze}-vase", image.Subject);
        using var planted = Decode(image.PngBytes);
        using var empty = Decode(StillLifeRenderer.Render(EmptyVase(scene)).PngBytes);
        var rim = (int)MathF.Round(daisy.MouthTop * Canvas);
        var bottom = (int)MathF.Round((daisy.OpeningCenter.Y + daisy.OpeningRadius.Y) * Canvas);
        var lipInner = LipInnerRow(empty, daisy, bottom);

        // The stem covers the rim band and the opening on every row from just above the tangent
        // down to the near lip, so the planted render differs from the empty vase at the stem's
        // own column all the way: a rim stroke painted over the stem, or a stroke that stops
        // inside the opening, both leave a row that matches the empty vase.
        for (var y = rim - 8; y <= lipInner - 2; y++)
        {
            var centre = StemColumn(daisy.Plant, y);
            var covered = false;
            for (var column = centre - 2; column <= centre + 2; column++)
            {
                covered |= Delta(planted.GetPixel(column, y), empty.GetPixel(column, y)) > 0;
            }

            Assert.True(covered, $"{name} lost the stem at the mouth on row {y}");
        }

        // And the lip covers the end: a few rows into the lip the stem is gone.
        var under = StemColumn(daisy.Plant, lipInner + 3);
        for (var column = under - 2; column <= under + 2; column++)
        {
            Assert.Equal(0, Delta(planted.GetPixel(column, lipInner + 3), empty.GetPixel(column, lipInner + 3)));
        }
    }

    [Theory]
    [MemberData(nameof(Dimensions))]
    public void DaisiesRenderAtOtherSupportedDimensions(int width, int height)
    {
        var scene = SceneGenerator.Generate(new(
            "a white daisy in a cream ceramic vase on a wooden table",
            Width: width,
            Height: height)).Scene!;
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

        // The daisy is drawn at every supported size: it is not a 1024-only pixel offset.
        Assert.True(changed > 10, $"expected daisy pixels at {width}x{height}, saw {changed}");

        // And it is drawn only inside its own measured bounds, in that canvas's own units.
        var daisy = scene.VaseDaisy!;
        var bounds = daisy.Plant.Bounds;
        var left = (bounds.Left * width) - 3;
        var right = (bounds.Right * width) + 3;
        var top = (bounds.Top * height) - 3;
        var bottom = (bounds.Bottom * height) + 3;
        var outside = 0;
        var sample = string.Empty;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (Delta(planted.GetPixel(x, y), empty.GetPixel(x, y)) == 0)
                {
                    continue;
                }

                if (x >= left && x <= right && y >= top && y <= bottom)
                {
                    continue;
                }

                // The mouth's own continuation is the one documented exception below the rim.
                if (y > daisy.MouthTop * height && Math.Abs(((x + 0.5f) / width) - daisy.OpeningCenter.X) < daisy.OpeningRadius.X)
                {
                    continue;
                }

                outside++;
                if (sample.Length == 0)
                {
                    sample = string.Create(
                        System.Globalization.CultureInfo.InvariantCulture,
                        $" first at ({(x + 0.5f) / width:0.0000}, {(y + 0.5f) / height:0.0000}) of {width}x{height}; bounds=({bounds.Left:0.0000},{bounds.Top:0.0000},{bounds.Right:0.0000},{bounds.Bottom:0.0000}); mouthTop={daisy.MouthTop:0.0000} opening={daisy.OpeningCenter.X:0.0000}/{daisy.OpeningRadius.X:0.0000}");
                }
            }
        }

        Assert.True(outside == 0, $"expected no daisy ink outside its bounds at {width}x{height}, saw {outside}{sample}");

        // The mouth's continuation has to land in the opening at every size, in that canvas's own
        // units: a continuation painted in the 1024 reference space would leave the opening empty
        // and drop a stray stub at reference coordinates instead.
        var inOpening = 0;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (Delta(planted.GetPixel(x, y), empty.GetPixel(x, y)) == 0)
                {
                    continue;
                }

                var dx = ((x + 0.5f) / width) - daisy.OpeningCenter.X;
                var dy = ((y + 0.5f) / height) - daisy.OpeningCenter.Y;
                var reachX = daisy.OpeningRadius.X + (2f / width);
                var reachY = daisy.OpeningRadius.Y + (2f / height);
                if (((dx * dx) / (reachX * reachX)) + ((dy * dy) / (reachY * reachY)) <= 1f)
                {
                    inOpening++;
                }
            }
        }

        Assert.True(inOpening > 0, $"expected the stem inside the opening at {width}x{height}, saw {inOpening} pixels");
    }

    [Fact]
    public void VaseGlazeNeverRecoloursTheDaisy()
    {
        var blue = Scene("a white daisy in a blue ceramic vase on a wooden table", 42, 0.5);
        var cream = Scene("a white daisy in a cream ceramic vase on a wooden table", 42, 0.5);
        using var bluePixels = Decode(StillLifeRenderer.Render(blue).PngBytes);
        using var creamPixels = Decode(StillLifeRenderer.Render(cream).PngBytes);
        var rim = (int)MathF.Floor(blue.VaseDaisy!.MouthTop * Canvas) - 4;
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

        // Everything above the vase is the background plus the daisy: the glaze must not reach
        // either of them.
        Assert.Equal(0, differences);
    }

    [Fact]
    public void ShadowMaskIsUnchangedAndTheSilhouetteGainsTheHead()
    {
        var scene = Scene("a white daisy in a blue ceramic vase on a wooden table");

        // The daisy casts no tabletop shadow and does not touch the vase's own shadow.
        Assert.Equal(
            StillLifeRenderer.RenderShadowMask(EmptyVase(scene)).PngBytes,
            StillLifeRenderer.RenderShadowMask(scene).PngBytes);

        using var empty = Decode(StillLifeRenderer.RenderSilhouette(EmptyVase(scene)).PngBytes);
        using var planted = Decode(StillLifeRenderer.RenderSilhouette(scene).PngBytes);
        var rim = (int)MathF.Ceiling(scene.VaseDaisy!.MouthTop * Canvas) + RimBand;
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
        Assert.True(added > 800, $"expected the head in the silhouette, saw {added} pixels");
    }

    [Fact]
    public void DetailOmissionAndDiagnosticPassesStayInsideTheDaisy()
    {
        var scene = Scene("a white daisy in a blue ceramic vase on a wooden table", 1337, 0.6);
        var daisy = scene.VaseDaisy!;
        var bounds = BoundsWindow(daisy.Plant, 3);
        using var full = Decode(StillLifeRenderer.Render(scene).PngBytes);
        using var plain = Decode(StillLifeRenderer.RenderWithoutInstanceDetail(scene, 0).PngBytes);

        var rim = (int)MathF.Ceiling(daisy.MouthTop * Canvas) - RimBand;
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

        Assert.True(changed > 100, $"expected petal edges and veins to be omitted, saw {changed} pixels");
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

    [Fact]
    public void AlternateSeedsReshapeTheDaisyWithoutChangingTheSubject()
    {
        var first = Scene("a white daisy in a blue ceramic vase on a wooden table", 42, 0.35);
        var second = Scene("a white daisy in a blue ceramic vase on a wooden table", 1337, 0.35);
        Assert.Equal(first.SubjectToken, second.SubjectToken);
        Assert.Equal(first.VaseDaisy!.Appearance, second.VaseDaisy!.Appearance);
        Assert.NotEqual(first.VaseDaisy.Plant.Bounds, second.VaseDaisy.Plant.Bounds);
        Assert.Equal(DaisyGeometry.PetalCount, second.VaseDaisy.Plant.Bloom.Petals.Length);
        Assert.Equal(2, second.VaseDaisy.Plant.Leaves.Length);

        using var one = Decode(StillLifeRenderer.Render(first).PngBytes);
        using var two = Decode(StillLifeRenderer.Render(second).PngBytes);
        var box = BloomWindow(first.VaseDaisy.Plant, 24);
        Assert.True(
            CountWithin(one, two, box) > 200,
            $"expected a reshaped head, saw {CountWithin(one, two, box)} pixels");
    }

    // Full-resolution samples, silhouettes and timing sidecars. The canonical case is written on
    // every run so the artifact always carries one complete daisy image; the rest of the matrix
    // follows when FORMFLOOM_EXPORT_ALL=1, the same trim the other families use.
    [Theory]
    [MemberData(nameof(ExportCases))]
    public void ExportVaseDaisiesTimingsAndSilhouettes(string prompt, string name, string glaze, int seed, double variation)
    {
        var scene = Scene(prompt, (ulong)seed, variation);
        Assert.Equal($"white-daisy-in-{glaze}-vase", scene.SubjectToken);
        var empty = EmptyVase(scene);
        var tulip = Scene($"a red tulip in a {glaze} ceramic vase on a wooden table", (ulong)seed, variation);

        var timer = Stopwatch.StartNew();
        var rendered = StillLifeRenderer.Render(scene);
        timer.Stop();
        var emptyTimer = Stopwatch.StartNew();
        var emptyRendered = StillLifeRenderer.Render(empty);
        emptyTimer.Stop();
        var tulipTimer = Stopwatch.StartNew();
        var tulipRendered = StillLifeRenderer.Render(tulip);
        tulipTimer.Stop();

        var stem = FormattableString.Invariant($"formloom-vasedaisy-{name}-seed-{seed}-variation-{variation:0.00}");
        var directory = AppContext.BaseDirectory;
        var keep = ExportArtifacts.Keeps(stem, CanonicalDaisy);
        var silhouette = StillLifeRenderer.RenderSilhouette(scene);
        Assert.NotEqual(silhouette.PngBytes, StillLifeRenderer.RenderSilhouette(empty).PngBytes);
        if (keep)
        {
            File.WriteAllBytes(Path.Combine(directory, stem + ".png"), rendered.PngBytes);
            File.WriteAllBytes(Path.Combine(directory, stem + "-silhouette.png"), silhouette.PngBytes);
        }

        var live = scene.VaseDaisy!;
        var report = System.Text.Json.JsonSerializer.Serialize(
            new
            {
                Prompt = prompt,
                Subject = scene.SubjectToken,
                VaseDaisy = new
                {
                    Flower = live.Flower,
                    Appearance = live.Appearance,
                    live.Vase.Kind,
                    live.Vase.Glaze,
                    Opening = new { live.OpeningCenter, live.OpeningRadius },
                    MouthTop = live.MouthTop,
                    live.Plant.StemBase,
                    live.Plant.StemTop,
                    live.Plant.StemThickness,
                    Bloom = new
                    {
                        live.Plant.Bloom.Center,
                        live.Plant.Bloom.RadiusX,
                        live.Plant.Bloom.RadiusY,
                        live.Plant.Bloom.Top,
                        live.Plant.Bloom.Bottom,
                        Disk = new { live.Plant.Bloom.Disk.RadiusX, live.Plant.Bloom.Disk.RadiusY },
                    },
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
                TulipRenderAndEncodeMilliseconds = tulipTimer.Elapsed.TotalMilliseconds,
                AdditionalMillisecondsOverEmptyVase = timer.Elapsed.TotalMilliseconds - emptyTimer.Elapsed.TotalMilliseconds,
                AdditionalMillisecondsOverTulip = timer.Elapsed.TotalMilliseconds - tulipTimer.Elapsed.TotalMilliseconds,
                ProcessorCount = Environment.ProcessorCount,
                Objects = scene.Objects,
                EmptyVaseBytes = emptyRendered.PngBytes.Length,
                TulipBytes = tulipRendered.PngBytes.Length,
            },
            new System.Text.Json.JsonSerializerOptions { IncludeFields = true });
        if (keep)
        {
            File.WriteAllText(Path.Combine(directory, stem + ".timing.json"), report);
            Assert.True(File.Exists(Path.Combine(directory, stem + ".png")));
        }
    }

    // One review sheet for the whole daisy matrix plus the manifest that is the authoritative key
    // for every cell. No text is drawn into the cells: the CI image has no font stack.
    [Fact]
    public void ExportVaseDaisyReviewSheetAndManifest()
    {
        const int Cell = 320;
        const int Columns = 4;
        var rows = (int)Math.Ceiling(Cases.Length / (double)Columns);
        using var sheet = new SKBitmap(Cell * Columns, Cell * rows);
        using var canvas = new SKCanvas(sheet);
        canvas.Clear(new SKColor(20, 22, 24));

        var manifest = new List<object>();
        for (var index = 0; index < Cases.Length; index++)
        {
            var (prompt, name, seed, variation) = Cases[index];
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

            var daisy = scene.VaseDaisy!;
            manifest.Add(new
            {
                name,
                prompt,
                seed,
                variation,
                subject = scene.SubjectToken,
                flower = daisy.Flower.ToString(),
                appearance = daisy.Appearance.ToString(),
                glaze = daisy.Vase.Glaze.ToString(),
                petals = daisy.Plant.Bloom.Petals.Length,
                leaves = daisy.Plant.Leaves.Length,
                fileName = FormattableString.Invariant($"formloom-vasedaisy-{name}-seed-{seed}-variation-{variation:0.00}.png"),
                silhouetteFileName = FormattableString.Invariant($"formloom-vasedaisy-{name}-seed-{seed}-variation-{variation:0.00}-silhouette.png"),
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
        File.WriteAllBytes(Path.Combine(directory, "formloom-vasedaisy-sheet.png"), encoded.ToArray());
        File.WriteAllText(
            Path.Combine(directory, "formloom-vasedaisy-sheet.timing.json"),
            System.Text.Json.JsonSerializer.Serialize(
                new
                {
                    Sheet = "formloom-vasedaisy-sheet.png",
                    Cell,
                    Columns,
                    Rows = rows,
                    Generated = DateTimeOffset.UtcNow,
                    Layout = "row-major, left to right, top to bottom",
                    Cases = manifest,
                },
                new System.Text.Json.JsonSerializerOptions { IncludeFields = true }));
        Assert.True(File.Exists(Path.Combine(directory, "formloom-vasedaisy-sheet.png")));
        Assert.Equal(Cases.Length, manifest.Count);
    }

    private static StillLifeScene Scene(string prompt, ulong seed = 20261105, double variation = 0.35) =>
        SceneGenerator.Generate(new(prompt, seed, variation)).Scene!;

    // The same vase with no daisy: the reference a planted render must match everywhere except
    // the daisy and the rim band the stem passes behind.
    private static StillLifeScene EmptyVase(StillLifeScene scene) => scene with { VaseDaisy = null };

    private readonly record struct Window(float Left, float Top, float Right, float Bottom);

    private static Window BloomWindow(DaisyPlant plant, int grow, int canvas = Canvas)
    {
        var bloom = plant.Bloom;
        return new(
            ((bloom.Left * canvas) - grow) / canvas,
            ((bloom.Top * canvas) - grow) / canvas,
            ((bloom.Right * canvas) + grow) / canvas,
            ((bloom.Bottom * canvas) + grow) / canvas);
    }

    private static Window DiskWindow(DaisyBloom bloom)
    {
        var disk = bloom.Disk;
        return new(
            disk.Center.X - disk.RadiusX,
            disk.Center.Y - disk.RadiusY,
            disk.Center.X + disk.RadiusX,
            disk.Center.Y + disk.RadiusY);
    }

    private static Window LeafWindow(DaisyLeaf leaf)
    {
        // A small box at the middle of the blade, well away from the stem and the head.
        var middle = (leaf.Attachment + leaf.Tip) / 2;
        const float Reach = 0.012f;
        return new(middle.X - Reach, middle.Y - Reach, middle.X + Reach, middle.Y + Reach);
    }

    private static Window StemWindow(VaseDaisy daisy)
    {
        var basePoint = daisy.Plant.StemBase;
        return new(basePoint.X - 0.018f, daisy.MouthTop - 0.034f, basePoint.X + 0.018f, daisy.MouthTop - 0.014f);
    }

    // Where the stem's centre line sits on a row, in pixels. The stem is a cubic and leans with
    // the seed and the variation, so the point is taken from the path itself.
    private static int StemColumn(DaisyPlant plant, int y)
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

    // The row where the near lip's inner edge crosses the stem's column, read off the empty vase
    // rather than from a pixel count: the lip is the brightest step below the opening's centre.
    private static int LipInnerRow(SKBitmap empty, VaseDaisy daisy, int bottom)
    {
        var column = StemColumn(daisy.Plant, bottom);
        var row = bottom;
        var best = 0;
        var previous = Brightness(empty.GetPixel(column, (int)MathF.Round(daisy.OpeningCenter.Y * Canvas)));
        for (var y = (int)MathF.Round(daisy.OpeningCenter.Y * Canvas) + 1; y <= bottom; y++)
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

    private static int Brightness(SKColor pixel) => (pixel.Red + pixel.Green + pixel.Blue) / 3;

    private static bool InsideOpening(VaseDaisy daisy, int x, int y, float grow)
    {
        var radiusX = (daisy.OpeningRadius.X * Canvas) + grow;
        var radiusY = (daisy.OpeningRadius.Y * Canvas) + grow;
        var dx = (x + 0.5f) - (daisy.OpeningCenter.X * Canvas);
        var dy = (y + 0.5f) - (daisy.OpeningCenter.Y * Canvas);
        return ((dx * dx) / (radiusX * radiusX)) + ((dy * dy) / (radiusY * radiusY)) <= 1f;
    }

    private static Window BoundsWindow(DaisyPlant plant, int grow)
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

    // Counts pixels inside a window that satisfy a predicate *and* differ from the empty vase, so
    // the warm tabletop and the background can never be mistaken for petal or disk colour.
    private static int Coloured(SKBitmap bitmap, SKBitmap empty, Window window, Func<SKColor, bool> predicate)
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
                if (Delta(bitmap.GetPixel(x, y), empty.GetPixel(x, y)) != 0 && predicate(bitmap.GetPixel(x, y)))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static int CountWithin(SKBitmap first, SKBitmap second, Window window)
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
                if (Delta(first.GetPixel(x, y), second.GetPixel(x, y)) != 0)
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
