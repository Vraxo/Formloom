using Formloom.Core;
using Formloom.Rendering.Skia;
using SkiaSharp;
using Xunit;

namespace Formloom.Tests;

public sealed class FruitRenderingTests
{
    [Theory]
    [InlineData("a green pear on a wooden table")]
    [InlineData("a yellow pear on a wooden table")]
    [InlineData("an orange on a wooden table")]
    public void NewSubjectsRenderAtSupportedSizesAndVariationExtremes(string prompt)
    {
        (int Width, int Height)[] sizes = [(256, 256), (1024, 1024), (1280, 1280), (640, 480)];
        double[] variations = [0, 1];
        foreach (var size in sizes)
        {
            foreach (var variation in variations)
            {
                var scene = SceneGenerator.Generate(new(prompt, Variation: variation, Width: size.Width, Height: size.Height)).Scene!;
                var result = StillLifeRenderer.Render(scene);
                byte[] signature = [137, 80, 78, 71, 13, 10, 26, 10];
                Assert.Equal(signature, result.PngBytes.Take(8).ToArray());
                Assert.Equal(scene.Subject.ToString().ToLowerInvariant(), result.Subject);
                using var decoded = SKBitmap.Decode(result.PngBytes);
                Assert.NotNull(decoded);
                Assert.Equal(size.Width, decoded.Width);
                Assert.Equal(size.Height, decoded.Height);
            }
        }
    }

    [Theory]
    [InlineData("a green pear on a wooden table")]
    [InlineData("an orange on a wooden table")]
    public void NewSubjectPixelsAreRepeatableAndSeedDependent(string prompt)
    {
        var request = new GenerationRequest(prompt, Width: 256, Height: 256);
        var scene = SceneGenerator.Generate(request).Scene!;
        using var first = SKBitmap.Decode(StillLifeRenderer.Render(scene).PngBytes);
        using var repeated = SKBitmap.Decode(StillLifeRenderer.Render(scene).PngBytes);
        var changedScene = SceneGenerator.Generate(request with { Seed = 1337 }).Scene!;
        using var different = SKBitmap.Decode(StillLifeRenderer.Render(changedScene).PngBytes);
        Assert.Equal(scene.Subject, changedScene.Subject);
        Assert.Equal(first.Bytes, repeated.Bytes);
        Assert.False(first.Bytes.SequenceEqual(different.Bytes));
    }

    [Fact]
    public void PeelDetailChangesOnlyPixelsInsideTheOrangeContour()
    {
        var scene = SceneGenerator.Generate(new("an orange on a wooden table", Width: 256, Height: 256)).Scene!;
        using var detailed = SKBitmap.Decode(StillLifeRenderer.Render(scene).PngBytes);
        using var smooth = SKBitmap.Decode(StillLifeRenderer.Render(scene with { SurfaceDetail = 0 }).PngBytes);
        using var mask = DrawMask(scene, includePlacement: true);
        var detailPixels = 0;
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
                    detailPixels++;
                }
            }
        }

        Assert.True(detailPixels > 20);
    }

    [Fact]
    public void BlackSilhouettesAreDistinctAndExportableForReview()
    {
        string[] prompts = ["a red apple on a wooden table", "a green pear on a wooden table", "an orange on a wooden table"];
        var silhouettes = new List<byte[]>();
        foreach (var prompt in prompts)
        {
            var scene = SceneGenerator.Generate(new(prompt, Width: 256, Height: 256)).Scene!;
            using var mask = DrawMask(scene, includePlacement: false);
            silhouettes.Add(mask.Bytes);
            using var image = SKImage.FromBitmap(mask);
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory, $"formloom-{scene.Subject.ToString().ToLowerInvariant()}-body-silhouette.png"), encoded.ToArray());
        }

        Assert.False(silhouettes[0].SequenceEqual(silhouettes[1]));
        Assert.False(silhouettes[1].SequenceEqual(silhouettes[2]));
        Assert.False(silhouettes[0].SequenceEqual(silhouettes[2]));
    }

    [Theory]
    [InlineData("a red apple on a wooden table", 42, 0)]
    [InlineData("a red apple on a wooden table", 1337, 1)]
    [InlineData("a green pear on a wooden table", 42, 0)]
    [InlineData("a green pear on a wooden table", 1337, 1)]
    [InlineData("a yellow pear on a wooden table", 42, 0.35)]
    [InlineData("an orange on a wooden table", 42, 0)]
    [InlineData("an orange on a wooden table", 1337, 1)]
    public void CompleteSilhouettesIncludeAttachmentsWithoutSettingOrClipping(string prompt, int seed, double variation)
    {
        var scene = SceneGenerator.Generate(new(prompt, (ulong)seed, variation, Width: 256, Height: 256)).Scene!;
        var rendered = StillLifeRenderer.RenderSilhouette(scene);
        using var complete = SKBitmap.Decode(rendered.PngBytes);
        using var repeated = SKBitmap.Decode(StillLifeRenderer.RenderSilhouette(scene).PngBytes);
        using var body = DrawMask(scene, includePlacement: true);
        Assert.Equal(complete.Bytes, repeated.Bytes);
        var addedPixels = 0;
        var bodyPixels = 0;
        for (var y = 0; y < scene.Height; y++)
        {
            for (var x = 0; x < scene.Width; x++)
            {
                var pixel = complete.GetPixel(x, y);
                Assert.Equal((byte)0, pixel.Red);
                Assert.Equal((byte)0, pixel.Green);
                Assert.Equal((byte)0, pixel.Blue);
                if (x == 0 || y == 0 || x == scene.Width - 1 || y == scene.Height - 1)
                {
                    Assert.Equal((byte)0, pixel.Alpha);
                }

                if (body.GetPixel(x, y).Alpha == 255)
                {
                    bodyPixels++;
                    Assert.Equal((byte)255, pixel.Alpha);
                }
                else if (body.GetPixel(x, y).Alpha == 0 && pixel.Alpha == 255)
                {
                    addedPixels++;
                }
            }
        }

        Assert.True(bodyPixels > 1000);
        if (scene.Subject != FruitKind.Orange)
        {
            Assert.True(addedPixels > 50);
            var attachment = OrganicForms.Create(scene.Subject, scene.Body.Asymmetry, scene.Seed, scene.Variation).TopAttachment;
            var angle = scene.Body.LeafAngle;
            var leafX = attachment.X + (scene.Body.StemLean * 0.2) + (0.17 * Math.Cos(angle));
            var leafY = attachment.Y - 0.155 + (0.17 * Math.Sin(angle));
            var x = (int)((scene.Body.Center.X + ((leafX - 0.5) * scene.Body.Size.X)) * scene.Width);
            var y = (int)((scene.Body.Center.Y + ((leafY - 0.5) * scene.Body.Size.Y)) * scene.Height);
            Assert.Equal((byte)255, complete.GetPixel(x, y).Alpha);
            Assert.Equal((byte)0, body.GetPixel(x, y).Alpha);
        }

        using var review = new SKBitmap(scene.Width, scene.Height);
        using var canvas = new SKCanvas(review);
        canvas.Clear(SKColors.White);
        canvas.DrawBitmap(complete, 0, 0);
        using var image = SKImage.FromBitmap(review);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        var name = FormattableString.Invariant($"formloom-{scene.Subject.ToString().ToLowerInvariant()}-complete-silhouette-seed-{seed}-variation-{variation:0.00}.png");
        File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory, name), encoded.ToArray());
    }

    [Theory]
    [InlineData("a red apple on a wooden table", "apple", 42, 0.35)]
    [InlineData("a green pear on a wooden table", "pear-green", 42, 0.35)]
    [InlineData("a yellow pear on a wooden table", "pear-yellow", 42, 0.35)]
    [InlineData("an orange on a wooden table", "orange", 42, 0.35)]
    [InlineData("a green pear on a wooden table", "pear-green", 1337, 0.35)]
    [InlineData("an orange on a wooden table", "orange", 1337, 0.35)]
    [InlineData("a green pear on a wooden table", "pear-green", 42, 0)]
    [InlineData("a green pear on a wooden table", "pear-green", 42, 1)]
    [InlineData("an orange on a wooden table", "orange", 42, 0)]
    [InlineData("an orange on a wooden table", "orange", 42, 1)]
    public void ExportReviewSamples(string prompt, string name, int seed, double variation)
    {
        var scene = SceneGenerator.Generate(new(prompt, (ulong)seed, variation)).Scene!;
        var rendered = StillLifeRenderer.Render(scene);
        var filename = FormattableString.Invariant($"formloom-{name}-seed-{seed}-variation-{variation:0.00}.png");
        File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory, filename), rendered.PngBytes);
        Assert.Equal(1024, rendered.Width);
        Assert.Equal(1024, rendered.Height);
    }

    private static SKBitmap DrawMask(StillLifeScene scene, bool includePlacement)
    {
        var bitmap = new SKBitmap(scene.Width, scene.Height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        canvas.Scale(scene.Width, scene.Height);
        if (includePlacement)
        {
            canvas.Translate(scene.Body.Center.X - (scene.Body.Size.X / 2), scene.Body.Center.Y - (scene.Body.Size.Y / 2));
            canvas.Scale(scene.Body.Size.X, scene.Body.Size.Y);
        }
        else
        {
            canvas.Translate(0.06f, 0.06f);
            canvas.Scale(0.88f, 0.88f);
        }

        var contour = OrganicForms.Create(scene.Subject, scene.Body.Asymmetry, scene.Seed, scene.Variation);
        using var builder = new SKPathBuilder();
        builder.MoveTo(contour.Segments[0].Start.X, contour.Segments[0].Start.Y);
        foreach (var segment in contour.Segments)
        {
            builder.CubicTo(segment.Control1.X, segment.Control1.Y, segment.Control2.X, segment.Control2.Y, segment.End.X, segment.End.Y);
        }

        builder.Close();
        using var path = builder.Detach();
        using var paint = new SKPaint { IsAntialias = true, Color = SKColors.Black };
        canvas.DrawPath(path, paint);
        return bitmap;
    }
}
