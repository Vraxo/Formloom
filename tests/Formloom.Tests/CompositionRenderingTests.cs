using System.Diagnostics;
using Formloom.Core;
using Formloom.Rendering.Skia;
using SkiaSharp;
using Xunit;

namespace Formloom.Tests;

public sealed class CompositionRenderingTests
{
    private const string CanonicalStem = "formloom-group-apple-pear-seed-42-variation-0.35";

    [Theory]
    [InlineData("a red apple and a green pear on a wooden table")]
    [InlineData("a green pear and an orange on a wooden table")]
    [InlineData("a red apple, a green pear, and an orange on a wooden table")]
    public void GroupsEncodeAtSupportedSizesAndRepeatPixels(string prompt)
    {
        (int Width, int Height)[] sizes = [(256, 256), (1024, 1024), (1280, 1280), (640, 480)];
        foreach (var size in sizes)
        {
            var request = new GenerationRequest(prompt, Width: size.Width, Height: size.Height);
            var scene = SceneGenerator.Generate(request).Scene!;
            var image = StillLifeRenderer.Render(scene);
            using var first = SKBitmap.Decode(image.PngBytes);
            using var repeated = SKBitmap.Decode(StillLifeRenderer.Render(scene).PngBytes);
            Assert.NotNull(first);
            Assert.Equal(size.Width, first.Width);
            Assert.Equal(size.Height, first.Height);
            Assert.Equal("fruit-group", image.Subject);
            Assert.Equal(first.Bytes, repeated.Bytes);
            if (size.Width == 256)
            {
                using var changed = SKBitmap.Decode(StillLifeRenderer.Render(SceneGenerator.Generate(request with { Seed = 1337 }).Scene!).PngBytes);
                Assert.False(first.Bytes.SequenceEqual(changed.Bytes));
            }
        }
    }

    [Theory]
    [InlineData("a red apple and a green pear on a wooden table")]
    [InlineData("a red apple, a green pear, and an orange on a wooden table")]
    public void CompleteSubjectMaskIsTheUnionOfAllInstances(string prompt)
    {
        var scene = SceneGenerator.Generate(new(prompt, Variation: 1, Width: 256, Height: 256)).Scene!;
        using var union = SKBitmap.Decode(StillLifeRenderer.RenderSilhouette(scene).PngBytes);
        foreach (var fruit in scene.Fruits)
        {
            using var single = SKBitmap.Decode(StillLifeRenderer.RenderSilhouette(ForFruit(scene, fruit)).PngBytes);
            var visible = 0;
            for (var y = 0; y < scene.Height; y++)
            {
                for (var x = 0; x < scene.Width; x++)
                {
                    if (single.GetPixel(x, y).Alpha == 255)
                    {
                        visible++;
                        Assert.Equal((byte)255, union.GetPixel(x, y).Alpha);
                    }

                    if (x == 0 || y == 0 || x == scene.Width - 1 || y == scene.Height - 1)
                    {
                        Assert.Equal((byte)0, single.GetPixel(x, y).Alpha);
                    }
                }
            }

            Assert.True(visible > 500);
        }
    }

    [Fact]
    public void DetailIsClippedAndDepthDeterminesTheVisibleForeground()
    {
        var scene = SceneGenerator.Generate(new("a green pear, an orange, and a red apple on a wooden table", Width: 256, Height: 256)).Scene!;
        using var detailed = SKBitmap.Decode(StillLifeRenderer.Render(scene).PngBytes);
        using var smooth = SKBitmap.Decode(StillLifeRenderer.Render(scene with { SurfaceDetail = 0 }).PngBytes);
        using var mask = SKBitmap.Decode(StillLifeRenderer.RenderSilhouette(scene).PngBytes);
        var differences = 0;
        for (var y = 0; y < scene.Height; y++)
        {
            for (var x = 0; x < scene.Width; x++)
            {
                if (mask.GetPixel(x, y).Alpha == 0)
                {
                    Assert.Equal(smooth.GetPixel(x, y), detailed.GetPixel(x, y));
                }
                else if (smooth.GetPixel(x, y) != detailed.GetPixel(x, y))
                {
                    differences++;
                }
            }
        }

        Assert.True(differences > 20);
        foreach (var fruit in scene.Fruits)
        {
            var later = scene.Fruits.Where(other => other.Depth > fruit.Depth).ToArray();
            using var own = SKBitmap.Decode(StillLifeRenderer.Render(ForFruit(scene, fruit)).PngBytes);
            var x = (int)(fruit.Body.Center.X * scene.Width);
            var y = (int)(fruit.Body.Center.Y * scene.Height);
            Assert.DoesNotContain(later, other => Math.Abs(other.Body.Center.X - fruit.Body.Center.X) < other.Body.Size.X / 2);
            Assert.Equal(own.GetPixel(x, y), detailed.GetPixel(x, y));
        }
    }

    [Theory]
    [InlineData("a red apple and a green pear on a wooden table")]
    [InlineData("a red apple, a green pear, and an orange on a wooden table")]
    public void EachFruitHasADarkContactBelowItsBody(string prompt)
    {
        var scene = SceneGenerator.Generate(new(prompt, Width: 512, Height: 512)).Scene! with { GrainStrength = 0 };
        using var bitmap = SKBitmap.Decode(StillLifeRenderer.Render(scene).PngBytes);
        foreach (var fruit in scene.Fruits)
        {
            var x = (int)(fruit.Body.Center.X * scene.Width);
            var y = (int)((fruit.Body.Center.Y + (fruit.Body.Size.Y * 0.51f)) * scene.Height);
            var contact = bitmap.GetPixel(x, y);
            var bareTable = bitmap.GetPixel(scene.Width - 2, y);
            Assert.True(contact.Red + contact.Green + contact.Blue < bareTable.Red + bareTable.Green + bareTable.Blue);
        }
    }

    [Theory]
    [InlineData("a red apple and a green pear on a wooden table", "apple-pear", 42, 0.35)]
    [InlineData("a green pear and an orange on a wooden table", "pear-orange", 42, 0.35)]
    [InlineData("an orange and a yellow apple on a wooden table", "orange-apple", 42, 0.35)]
    [InlineData("a red apple, a green pear, and an orange on a wooden table", "red-trio", 42, 0.35)]
    [InlineData("a yellow apple, a yellow pear, and an orange on a wooden table", "yellow-trio", 42, 0.35)]
    [InlineData("a red apple and a green pear on a wooden table", "apple-pear", 1337, 0.35)]
    [InlineData("a red apple, a green pear, and an orange on a wooden table", "red-trio", 1337, 0.35)]
    [InlineData("a red apple and a green pear on a wooden table", "apple-pear", 42, 0)]
    [InlineData("a red apple and a green pear on a wooden table", "apple-pear", 42, 1)]
    [InlineData("a red apple, a green pear, and an orange on a wooden table", "red-trio", 42, 0)]
    [InlineData("a red apple, a green pear, and an orange on a wooden table", "red-trio", 42, 1)]
    public void ExportCompositionsAndAdvisoryTimings(string prompt, string name, int seed, double variation)
    {
        var scene = SceneGenerator.Generate(new(prompt, (ulong)seed, variation)).Scene!;
        var timer = Stopwatch.StartNew();
        var rendered = StillLifeRenderer.Render(scene);
        timer.Stop();
        var stem = FormattableString.Invariant($"formloom-group-{name}-seed-{seed}-variation-{variation:0.00}");
        var keep = ExportArtifacts.Keeps(stem, CanonicalStem);
        if (keep)
        {
            File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory, stem + ".png"), rendered.PngBytes);
        }

        var report = System.Text.Json.JsonSerializer.Serialize(
            new
            {
                Prompt = prompt,
                Count = scene.Fruits.Length,
                rendered.Width,
                rendered.Height,
                Framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                OperatingSystem = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                scene.Seed,
                scene.Variation,
                RenderAndEncodeMilliseconds = timer.Elapsed.TotalMilliseconds,
                ProcessorCount = Environment.ProcessorCount,
                Placements = scene.Fruits,
            },
            new System.Text.Json.JsonSerializerOptions { IncludeFields = true });
        if (keep)
        {
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, stem + ".timing.json"), report);
            File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory, stem + "-silhouette.png"), StillLifeRenderer.RenderSilhouette(scene).PngBytes);
        }
    }

    private static StillLifeScene ForFruit(StillLifeScene scene, FruitInstance fruit) => scene with
    {
        Apple = fruit.Body,
        Subject = fruit.Subject,
        Seed = fruit.Seed,
        Instances = null,
    };
}
