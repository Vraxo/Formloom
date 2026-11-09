using System.Collections.Immutable;
using System.Diagnostics;
using Formloom.Core.Ceramics;
using Formloom.Core.Scenes;
using Formloom.Rendering.Skia.Scenes;
using Formloom.Tests.Core;
using SkiaSharp;
using Xunit;

namespace Formloom.Tests.Ceramics;

public sealed class CeramicRenderingTests
{
    private const string CanonicalStem = "formloom-ceramic-vase-blue-seed-42-variation-0.35";

    private static readonly string[] ReviewPrompts =
    [
        "a blue ceramic bowl on a wooden table",
        "a cream ceramic bowl on a wooden table",
        "a blue ceramic cup on a wooden table",
        "a cream ceramic cup on a wooden table",
        "a blue ceramic vase on a wooden table",
        "a cream ceramic vase on a wooden table",
        "a red apple and a blue ceramic bowl on a wooden table",
        "a green pear and a cream ceramic cup on a wooden table",
        "an orange and a blue ceramic vase on a wooden table",
        "a red apple, a cream ceramic bowl, and an orange on a wooden table",
        "a cream ceramic cup, a green pear, and a blue ceramic vase on a wooden table",
        "a blue ceramic vase and a blue ceramic bowl on a wooden table",
    ];

    [Theory]
    [InlineData("a blue ceramic bowl on a wooden table", "ceramic-bowl")]
    [InlineData("a cream ceramic bowl on a wooden table", "ceramic-bowl")]
    [InlineData("a blue ceramic cup on a wooden table", "ceramic-cup")]
    [InlineData("a cream ceramic cup on a wooden table", "ceramic-cup")]
    [InlineData("a blue ceramic vase on a wooden table", "ceramic-vase")]
    [InlineData("a cream ceramic vase on a wooden table", "ceramic-vase")]
    public void VesselsRenderAtDefaultSizeAndRepeatExactly(string prompt, string subject)
    {
        var scene = SceneGenerator.Generate(new(prompt)).Scene!;
        var image = StillLifeRenderer.Render(scene);
        using var bitmap = SKBitmap.Decode(image.PngBytes);
        Assert.NotNull(bitmap);
        Assert.Equal(1024, bitmap.Width);
        Assert.Equal(1024, bitmap.Height);
        Assert.Equal(subject, image.Subject);
        Assert.Equal(image.PngBytes, StillLifeRenderer.Render(scene).PngBytes);
        var opaque = 0;
        for (var y = 384; y < 1024; y += 8)
        {
            for (var x = 0; x < 1024; x += 8)
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
    public void GlazesAreDistinctMaterials()
    {
        foreach (var kind in CeramicKinds())
        {
            var blue = SceneGenerator.Generate(new($"a blue ceramic {kind} on a wooden table", Width: 256, Height: 256)).Scene!;
            var cream = SceneGenerator.Generate(new($"a cream ceramic {kind} on a wooden table", Width: 256, Height: 256)).Scene!;
            using var bluePixels = SKBitmap.Decode(StillLifeRenderer.Render(blue).PngBytes);
            using var creamPixels = SKBitmap.Decode(StillLifeRenderer.Render(cream).PngBytes);
            var ceramic = Assert.Single(blue.Ceramics);
            var x = (int)(ceramic.Body.Center.X * 256);
            var y = (int)((ceramic.Body.Center.Y + (ceramic.Body.Size.Y * 0.1f)) * 256);
            var blueColor = bluePixels.GetPixel(x, y);
            var creamColor = creamPixels.GetPixel(x, y);
            Assert.True(creamColor.Red > blueColor.Red);
            Assert.True(creamColor.Blue < blueColor.Blue + 40);
            Assert.NotEqual(blueColor, creamColor);
        }
    }

    [Theory]
    [InlineData("a blue ceramic bowl on a wooden table")]
    [InlineData("a red apple and a cream ceramic cup on a wooden table")]
    [InlineData("a cream ceramic cup, a green pear, and a blue ceramic vase on a wooden table")]
    public void MixedScenesEncodeAtSupportedSizesAndRespondToSeeds(string prompt)
    {
        (int Width, int Height)[] sizes = [(256, 256), (1024, 1024), (1280, 1280), (640, 480)];
        foreach (var size in sizes)
        {
            var request = new GenerationRequest(prompt, Width: size.Width, Height: size.Height);
            var scene = SceneGenerator.Generate(request).Scene!;
            var image = StillLifeRenderer.Render(scene);
            using var first = SKBitmap.Decode(image.PngBytes);
            using var repeated = SKBitmap.Decode(StillLifeRenderer.Render(scene).PngBytes);
            Assert.Equal(size.Width, first.Width);
            Assert.Equal(size.Height, first.Height);
            Assert.Equal(first.Bytes, repeated.Bytes);
            if (size.Width == 256)
            {
                using var changed = SKBitmap.Decode(StillLifeRenderer.Render(SceneGenerator.Generate(request with { Seed = 1337 }).Scene!).PngBytes);
                Assert.False(first.Bytes.SequenceEqual(changed.Bytes));
            }
        }
    }

    [Fact]
    public void BowlCavityReadsAsAConcaveOpening()
    {
        var scene = SceneGenerator.Generate(new("a blue ceramic bowl on a wooden table", Width: 512, Height: 512)).Scene!;
        var bowl = Assert.Single(scene.Ceramics);
        var profile = CeramicForms.For(CeramicKind.Bowl);
        using var bitmap = SKBitmap.Decode(StillLifeRenderer.Render(scene).PngBytes);
        var interior = Pixel(bitmap, scene, bowl, profile, 0.5f, profile.MouthCenter.Y);
        var sheen = Pixel(bitmap, scene, bowl, profile, 0.5f, profile.MouthCenter.Y - (profile.MouthRadius.Y * 0.28f));
        var frontWall = Pixel(bitmap, scene, bowl, profile, 0.5f, profile.MouthCenter.Y + (profile.MouthRadius.Y * 2.6f));
        Assert.Equal((byte)255, interior.Alpha);
        Assert.True(Sum(interior) < Sum(frontWall));
        Assert.True(Sum(sheen) > Sum(interior) + 15);
    }

    [Fact]
    public void CupHandleHoleStaysOpenAndTheRingJoinsTheBody()
    {
        var scene = SceneGenerator.Generate(new("a cream ceramic cup on a wooden table", Width: 512, Height: 512)).Scene!;
        var cup = Assert.Single(scene.Ceramics);
        var profile = CeramicForms.For(CeramicKind.Cup);
        var handle = profile.Handle!;
        using var silhouette = SKBitmap.Decode(StillLifeRenderer.RenderSilhouette(scene).PngBytes);
        var hole = Pixel(silhouette, scene, cup, profile, 0.865f, handle.Center.Y);
        var ring = Pixel(silhouette, scene, cup, profile, 0.865f, 0.175f);
        Assert.Equal((byte)0, hole.Alpha);
        Assert.Equal((byte)255, ring.Alpha);

        var open = 0;
        var entered = false;
        for (var step = 0; step <= 200; step++)
        {
            var x = 0.87f - (step * 0.00175f);
            var sample = Alpha(silhouette, scene, cup, profile, x, handle.Center.Y);
            if (sample == 0)
            {
                if (entered)
                {
                    Assert.Fail("The cup handle has a gap between the ring and the body.");
                }

                open++;
            }
            else if (sample == 255)
            {
                entered = true;
            }
        }

        Assert.True(open > 10);
    }

    [Fact]
    public void VaseMouthIsAnOpenRimRatherThanAClosedTop()
    {
        var scene = SceneGenerator.Generate(new("a blue ceramic vase on a wooden table", Width: 512, Height: 512)).Scene!;
        var vase = Assert.Single(scene.Ceramics);
        var profile = CeramicForms.For(CeramicKind.Vase);
        using var bitmap = SKBitmap.Decode(StillLifeRenderer.Render(scene).PngBytes);
        var interior = Pixel(bitmap, scene, vase, profile, 0.5f, profile.MouthCenter.Y);
        var lip = Pixel(bitmap, scene, vase, profile, 0.5f - (profile.MouthRadius.X * 0.8f), profile.MouthCenter.Y);
        var shoulder = Pixel(bitmap, scene, vase, profile, 0.5f, 0.45f);
        Assert.Equal((byte)255, interior.Alpha);
        Assert.True(Sum(interior) < Sum(shoulder));
        Assert.True(Sum(lip) > Sum(interior));
    }

    [Fact]
    public void CeramicSilhouettesAreClosedOpeningsExcludedAndClearOfEdges()
    {
        foreach (var kind in CeramicKinds())
        {
            var scene = SceneGenerator.Generate(new($"a blue ceramic {kind} on a wooden table", Variation: 1, Width: 256, Height: 256)).Scene!;
            using var silhouette = SKBitmap.Decode(StillLifeRenderer.RenderSilhouette(scene).PngBytes);
            var visible = 0;
            for (var y = 0; y < scene.Height; y++)
            {
                for (var x = 0; x < scene.Width; x++)
                {
                    var pixel = silhouette.GetPixel(x, y);
                    if (pixel.Alpha == 255)
                    {
                        visible++;
                    }

                    if (x == 0 || y == 0 || x == scene.Width - 1 || y == scene.Height - 1)
                    {
                        Assert.Equal((byte)0, pixel.Alpha);
                    }
                }
            }

            Assert.True(visible > 3000);
        }
    }

    [Theory]
    [InlineData("a red apple and a blue ceramic bowl on a wooden table")]
    [InlineData("a cream ceramic cup, a green pear, and a blue ceramic vase on a wooden table")]
    public void DetailStaysInsideEachObjectsOwnSilhouette(string prompt)
    {
        var scene = SceneGenerator.Generate(new(prompt, Variation: 1, Width: 256, Height: 256)).Scene!;
        foreach (var entry in scene.Objects)
        {
            using var detailed = SKBitmap.Decode(StillLifeRenderer.Render(scene).PngBytes);
            using var smooth = SKBitmap.Decode(StillLifeRenderer.RenderWithoutInstanceDetail(scene, entry.Depth).PngBytes);
            using var mask = SKBitmap.Decode(StillLifeRenderer.RenderSilhouette(Single(scene, entry)).PngBytes);
            var changed = 0;
            for (var y = 0; y < scene.Height; y++)
            {
                for (var x = 0; x < scene.Width; x++)
                {
                    var before = detailed.GetPixel(x, y);
                    var after = smooth.GetPixel(x, y);
                    if (mask.GetPixel(x, y).Alpha == 0)
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
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OverlappingObjectsCompositeInDepthOrder(bool vesselInFront)
    {
        var scene = SceneGenerator.Generate(new("a red apple and a blue ceramic bowl on a wooden table", Width: 256, Height: 256)).Scene!;
        var vessel = scene.Objects.First(entry => entry.IsCeramic);
        var fruit = scene.Objects.First(entry => entry.IsFruit);
        var (back, front) = vesselInFront ? (fruit, vessel) : (vessel, fruit);
        back = back.WithCenter(new(0.46f, 0.60f), back.Size, 0);
        front = front.WithCenter(new(0.57f, 0.60f), front.Size, 3);
        var overlapped = scene with { Entries = ImmutableArray.Create(back, front), Instances = null };
        Assert.Null(SceneValidator.Validate(overlapped));
        using var union = SKBitmap.Decode(StillLifeRenderer.Render(overlapped).PngBytes);
        using var backMask = SKBitmap.Decode(StillLifeRenderer.RenderSilhouette(Single(scene, back)).PngBytes);
        using var frontMask = SKBitmap.Decode(StillLifeRenderer.RenderSilhouette(Single(scene, front)).PngBytes);
        using var foreground = SKBitmap.Decode(StillLifeRenderer.Render(Single(scene, front)).PngBytes);
        var overlap = 0;
        for (var y = 0; y < scene.Height; y++)
        {
            for (var x = 0; x < scene.Width; x++)
            {
                if (backMask.GetPixel(x, y).Alpha == 255 && frontMask.GetPixel(x, y).Alpha == 255)
                {
                    overlap++;
                    Assert.Equal(foreground.GetPixel(x, y), union.GetPixel(x, y));
                }
            }
        }

        Assert.True(overlap > 200);
    }

    [Fact]
    public void VesselShadowMasksAreGroundedDirectionalAndClearOfEdges()
    {
        ulong[] seeds = [0, 42, 1337];
        double[] variations = [0, 1];
        foreach (var kind in CeramicKinds())
        {
            foreach (var glaze in new[] { "blue", "cream" })
            {
                foreach (var seed in seeds)
                {
                    foreach (var variation in variations)
                    {
                        var scene = SceneGenerator.Generate(
                            new($"a {glaze} ceramic {kind} on a wooden table", seed, variation, Width: 256, Height: 256)).Scene!;
                        using var shadows = SKBitmap.Decode(StillLifeRenderer.RenderShadowMask(scene).PngBytes);
                        AssertClearOfCanvasEdges(shadows, scene.Width, scene.Height);
                        var vessel = Assert.Single(scene.Ceramics);
                        var contactX = (int)(vessel.Body.Center.X * scene.Width);
                        var contactY = Math.Clamp((int)((vessel.Body.Center.Y + ((CeramicForms.For(vessel.Kind).BaseBottom - 0.5f) * vessel.Body.Size.Y)) * scene.Height), 0, scene.Height - 1);
                        Assert.True(shadows.GetPixel(contactX, contactY).Alpha > 10);
                        var right = 0;
                        var left = 0;
                        for (var offset = 0; offset < scene.Width * 0.30f; offset++)
                        {
                            for (var row = -2; row <= 6; row++)
                            {
                                right += shadows.GetPixel(Math.Min(scene.Width - 1, contactX + (int)offset), Math.Clamp(contactY + row, 0, scene.Height - 1)).Alpha;
                                left += shadows.GetPixel(Math.Max(0, contactX - (int)offset), Math.Clamp(contactY + row, 0, scene.Height - 1)).Alpha;
                            }
                        }

                        Assert.True(right > 0);
                        Assert.True(right > left);
                    }
                }
            }
        }
    }

    [Theory]
    [InlineData("a red apple and a blue ceramic bowl on a wooden table")]
    [InlineData("a cream ceramic cup, a green pear, and a blue ceramic vase on a wooden table")]
    public void MixedSceneShadowMasksCoverEveryObjectAndStayClearOfEdges(string prompt)
    {
        ulong[] seeds = [0, 42, 1337];
        double[] variations = [0, 1];
        foreach (var seed in seeds)
        {
            foreach (var variation in variations)
            {
                var scene = SceneGenerator.Generate(new(prompt, seed, variation, Width: 256, Height: 256)).Scene!;
                using var shadows = SKBitmap.Decode(StillLifeRenderer.RenderShadowMask(scene).PngBytes);
                AssertClearOfCanvasEdges(shadows, scene.Width, scene.Height);
                foreach (var entry in scene.Objects)
                {
                    var contactX = Math.Clamp((int)(entry.Center.X * scene.Width), 0, scene.Width - 1);
                    var contactY = Math.Clamp((int)((entry.Center.Y + (entry.GroundOffset * entry.Size.Y)) * scene.Height), 0, scene.Height - 1);
                    var band = 0;
                    for (var x = -6; x <= 6; x++)
                    {
                        for (var row = -2; row <= 4; row++)
                        {
                            band += shadows.GetPixel(Math.Clamp(contactX + x, 0, scene.Width - 1), Math.Clamp(contactY + row, 0, scene.Height - 1)).Alpha;
                        }
                    }

                    Assert.True(band > 0);
                }
            }
        }
    }

    [Fact]
    public void DiagnosticPassesDoNotChangeSubsequentMixedRendering()
    {
        var scene = SceneGenerator.Generate(new("a cream ceramic cup, a green pear, and a blue ceramic vase on a wooden table", Width: 128, Height: 128)).Scene!;
        var original = StillLifeRenderer.Render(scene);
        using var shadows = SKBitmap.Decode(StillLifeRenderer.RenderShadowMask(scene).PngBytes);
        using var changed = SKBitmap.Decode(StillLifeRenderer.RenderWithoutInstanceDetail(scene, scene.Objects[0].Depth).PngBytes);
        Assert.NotNull(shadows);
        Assert.NotNull(changed);
        Assert.Equal(original.PngBytes, StillLifeRenderer.Render(scene).PngBytes);
    }

    [Theory]
    [InlineData("a blue ceramic bowl on a wooden table", "bowl-blue", 42, 0.35)]
    [InlineData("a cream ceramic bowl on a wooden table", "bowl-cream", 42, 0.35)]
    [InlineData("a blue ceramic cup on a wooden table", "cup-blue", 42, 0.35)]
    [InlineData("a cream ceramic cup on a wooden table", "cup-cream", 42, 0.35)]
    [InlineData("a blue ceramic vase on a wooden table", "vase-blue", 42, 0.35)]
    [InlineData("a cream ceramic vase on a wooden table", "vase-cream", 42, 0.35)]
    [InlineData("a red apple and a blue ceramic bowl on a wooden table", "apple-bowl", 42, 0.35)]
    [InlineData("a green pear and a cream ceramic cup on a wooden table", "pear-cup", 42, 0.35)]
    [InlineData("an orange and a blue ceramic vase on a wooden table", "orange-vase", 42, 0.35)]
    [InlineData("a red apple, a cream ceramic bowl, and an orange on a wooden table", "mixed-trio", 42, 0.35)]
    [InlineData("a cream ceramic cup, a green pear, and a blue ceramic vase on a wooden table", "vessel-trio", 42, 0.35)]
    [InlineData("a blue ceramic bowl and a cream ceramic cup on a wooden table", "bowl-cup", 42, 0.35)]
    [InlineData("a blue ceramic bowl and a cream ceramic cup on a wooden table", "bowl-cup", 1337, 0.35)]
    [InlineData("a red apple and a blue ceramic bowl on a wooden table", "apple-bowl", 42, 0)]
    [InlineData("a red apple and a blue ceramic bowl on a wooden table", "apple-bowl", 42, 1)]
    [InlineData("a cream ceramic cup, a green pear, and a blue ceramic vase on a wooden table", "vessel-trio", 42, 1)]
    public void ExportCeramicScenesTimingsAndSilhouettes(string prompt, string name, int seed, double variation)
    {
        var scene = SceneGenerator.Generate(new(prompt, (ulong)seed, variation)).Scene!;
        var timer = Stopwatch.StartNew();
        var rendered = StillLifeRenderer.Render(scene);
        timer.Stop();
        var stem = FormattableString.Invariant($"formloom-ceramic-{name}-seed-{seed}-variation-{variation:0.00}");
        var keep = ExportArtifacts.Keeps(stem, CanonicalStem);
        if (keep)
        {
            File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory, stem + ".png"), rendered.PngBytes);
        }

        var report = System.Text.Json.JsonSerializer.Serialize(
            new
            {
                Prompt = prompt,
                Count = scene.Objects.Length,
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
        if (keep)
        {
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, stem + ".timing.json"), report);
            File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory, stem + "-silhouette.png"), StillLifeRenderer.RenderSilhouette(scene).PngBytes);
        }
    }

    [Fact]
    public void ExportCompactCeramicReviewSheet()
    {
        const int Cell = 256;
        const int Columns = 4;
        var rows = (int)Math.Ceiling(ReviewPrompts.Length / (double)Columns);
        using var sheet = new SKBitmap(Cell * Columns, Cell * rows);
        using var canvas = new SKCanvas(sheet);
        canvas.Clear(new SKColor(22, 24, 26));
        for (var index = 0; index < ReviewPrompts.Length; index++)
        {
            var scene = SceneGenerator.Generate(new(ReviewPrompts[index], Width: Cell, Height: Cell)).Scene!;
            using var cell = SKBitmap.Decode(StillLifeRenderer.Render(scene).PngBytes);
            canvas.DrawBitmap(
                cell,
                new SKRect((index % Columns) * Cell, (index / Columns) * Cell, ((index % Columns) + 1) * Cell, ((index / Columns) + 1) * Cell),
                new SKSamplingOptions(SKFilterMode.Linear));
        }

        using var image = SKImage.FromBitmap(sheet);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory, "formloom-ceramic-sheet.png"), encoded.ToArray());
        Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, "formloom-ceramic-sheet.png")));
    }

    private static IEnumerable<string> CeramicKinds() => ["bowl", "cup", "vase"];

    private static void AssertClearOfCanvasEdges(SKBitmap shadows, int width, int height)
    {
        for (var edge = 0; edge < width; edge++)
        {
            Assert.Equal((byte)0, shadows.GetPixel(edge, 0).Alpha);
            Assert.Equal((byte)0, shadows.GetPixel(edge, height - 1).Alpha);
            Assert.Equal((byte)0, shadows.GetPixel(0, edge).Alpha);
            Assert.Equal((byte)0, shadows.GetPixel(width - 1, edge).Alpha);
        }
    }

    private static StillLifeScene Single(StillLifeScene scene, SceneEntry entry) =>
        scene with { Entries = ImmutableArray.Create(entry), Instances = null };

    private static SKColor Pixel(SKBitmap bitmap, StillLifeScene scene, CeramicInstance ceramic, CeramicProfile profile, float x, float y) =>
        bitmap.GetPixel(X(scene, ceramic, x), Y(scene, ceramic, profile, y));

    private static byte Alpha(SKBitmap bitmap, StillLifeScene scene, CeramicInstance ceramic, CeramicProfile profile, float x, float y) =>
        Pixel(bitmap, scene, ceramic, profile, x, y).Alpha;

    private static int X(StillLifeScene scene, CeramicInstance ceramic, float profileX) =>
        Math.Clamp(
            (int)((ceramic.Body.Center.X - (ceramic.Body.Size.X / 2) + (profileX * ceramic.Body.Size.X)) * scene.Width),
            0,
            scene.Width - 1);

    private static int Y(StillLifeScene scene, CeramicInstance ceramic, CeramicProfile profile, float profileY) =>
        Math.Clamp(
            (int)((ceramic.Body.Center.Y - (ceramic.Body.Size.Y / 2) + (profileY * ceramic.Body.Size.Y)) * scene.Height),
            0,
            scene.Height - 1);

    private static int Sum(SKColor color) => color.Red + color.Green + color.Blue;
}
