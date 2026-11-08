using Formloom.Core;
using Formloom.Rendering.Skia;
using SkiaSharp;
using Xunit;

namespace Formloom.Tests;

public sealed class RenderingTests
{
    [Theory]
    [InlineData(256, 256)]
    [InlineData(1024, 1024)]
    [InlineData(1280, 1280)]
    [InlineData(640, 480)]
    public void RendererProducesDecodablePngAtRequestedSize(int width, int height)
    {
        var scene = SceneGenerator.Generate(new("a red apple on a wooden table", Width: width, Height: height)).Scene!;
        var rendered = StillLifeRenderer.Render(scene);
        byte[] signature = [137, 80, 78, 71, 13, 10, 26, 10];
        Assert.Equal(signature, rendered.PngBytes.Take(8).ToArray());
        Assert.Equal(width, rendered.Width);
        Assert.Equal(height, rendered.Height);
        using var decoded = SKBitmap.Decode(rendered.PngBytes);
        Assert.NotNull(decoded);
        Assert.Equal(width, decoded.Width);
        Assert.Equal(height, decoded.Height);
        Assert.NotEqual(decoded.GetPixel(0, 0), decoded.GetPixel(width / 2, height / 2));
    }

    [Fact]
    public void ExportDefaultPreviewForVisualReview()
    {
        var scene = SceneGenerator.Generate(new("a red apple on a wooden table")).Scene!;
        var rendered = StillLifeRenderer.Render(scene);
        var path = Path.Combine(AppContext.BaseDirectory, "formloom-apple-seed-42.png");
        File.WriteAllBytes(path, rendered.PngBytes);
        using var decoded = SKBitmap.Decode(path);
        Assert.NotNull(decoded);
        Assert.Equal(1024, decoded.Width);
        Assert.Equal(1024, decoded.Height);
    }

    [Fact]
    public void IdenticalInputsProduceIdenticalPixels()
    {
        var scene = SceneGenerator.Generate(new("a red apple on a wooden table", Width: 256, Height: 256)).Scene!;
        using var first = SKBitmap.Decode(StillLifeRenderer.Render(scene).PngBytes);
        using var second = SKBitmap.Decode(StillLifeRenderer.Render(scene).PngBytes);
        Assert.Equal(first.Bytes, second.Bytes);
    }

    [Fact]
    public void AntialiasingCanBeDisabledForInspectionWhileDefaultRemainsUnchanged()
    {
        var scene = SceneGenerator.Generate(new("a red apple on a wooden table", Width: 256, Height: 256)).Scene!;
        var defaultRender = StillLifeRenderer.Render(scene);
        var explicitDefault = StillLifeRenderer.Render(scene, new RenderOptions());
        var aliased = StillLifeRenderer.Render(scene, new RenderOptions(Antialiasing: false));
        var repeatedAliased = StillLifeRenderer.Render(scene, new RenderOptions(Antialiasing: false));

        Assert.Equal(defaultRender.PngBytes, explicitDefault.PngBytes);
        Assert.NotEqual(defaultRender.PngBytes, aliased.PngBytes);
        Assert.Equal(aliased.PngBytes, repeatedAliased.PngBytes);
    }

    [Theory]
    [InlineData("red", 256)]
    [InlineData("green", 256)]
    [InlineData("yellow", 256)]
    [InlineData("red", 1024)]
    public void OpaqueAppleInteriorDoesNotRevealChangesToSetting(string color, int size)
    {
        var scene = SceneGenerator.Generate(new($"a {color} apple on a wooden table", Width: size, Height: size)).Scene!;
        var changedSetting = scene with
        {
            BackgroundWarmth = 0.95,
            TableHorizon = 0.40,
            GrainStrength = 0.8,
        };
        using var original = SKBitmap.Decode(StillLifeRenderer.Render(scene).PngBytes);
        using var changed = SKBitmap.Decode(StillLifeRenderer.Render(changedSetting).PngBytes);
        Assert.NotEqual(original.GetPixel(0, 0), changed.GetPixel(0, 0));

        // Samples are well inside the body, away from antialiased silhouette edges.
        for (var row = 48; row <= 70; row += 2)
        {
            for (var column = 46; column <= 54; column += 2)
            {
                var x = (size * column) / 100;
                var y = (size * row) / 100;
                Assert.Equal(original.GetPixel(x, y), changed.GetPixel(x, y));
            }
        }
    }

    [Theory]
    [InlineData(256, 0.0)]
    [InlineData(256, 1.0)]
    [InlineData(1024, 0.35)]
    public void ContactShadowDarkensTableDirectlyBelowBothLobes(int size, double variation)
    {
        var scene = SceneGenerator.Generate(new(
            "a red apple on a wooden table", Variation: variation, Width: size, Height: size)).Scene!;
        using var bitmap = SKBitmap.Decode(StillLifeRenderer.Render(scene).PngBytes);
        var apple = scene.Apple;
        var y = (int)((apple.Center.Y + (apple.Size.Y * 0.51f)) * size);
        var referenceX = (int)((apple.Center.X + (apple.Size.X * 0.85f)) * size);
        var reference = bitmap.GetPixel(referenceX, y);
        var referenceBrightness = reference.Red + reference.Green + reference.Blue;
        float[] lobeOffsets = [-0.13f, 0.13f];
        foreach (var lobe in lobeOffsets)
        {
            var x = (int)((apple.Center.X + (apple.Size.X * lobe)) * size);
            var contact = bitmap.GetPixel(x, y);
            Assert.True(contact.Red + contact.Green + contact.Blue < referenceBrightness);
        }
    }

    [Fact]
    public void WoodDetailChangesWithSeedWithoutChangingBackgroundPalette()
    {
        var scene = SceneGenerator.Generate(new("a red apple on a wooden table", Width: 256, Height: 256)).Scene!;
        using var first = SKBitmap.Decode(StillLifeRenderer.Render(scene).PngBytes);
        using var repeated = SKBitmap.Decode(StillLifeRenderer.Render(scene).PngBytes);
        using var different = SKBitmap.Decode(StillLifeRenderer.Render(scene with { Seed = 43 }).PngBytes);
        Assert.Equal(first.GetPixel(0, 0), different.GetPixel(0, 0));
        var changedPixels = 0;
        for (var y = 220; y < 250; y++)
        {
            for (var x = 10; x < 246; x += 4)
            {
                Assert.Equal(first.GetPixel(x, y), repeated.GetPixel(x, y));
                if (first.GetPixel(x, y) != different.GetPixel(x, y))
                {
                    changedPixels++;
                }
            }
        }

        Assert.True(changedPixels > 0);
    }

    [Fact]
    public void DifferentSeedChangesPixels()
    {
        var request = new GenerationRequest("a red apple on a wooden table", Width: 256, Height: 256);
        var first = StillLifeRenderer.Render(SceneGenerator.Generate(request).Scene!);
        var second = StillLifeRenderer.Render(SceneGenerator.Generate(request with { Seed = 43 }).Scene!);
        using var firstBitmap = SKBitmap.Decode(first.PngBytes);
        using var secondBitmap = SKBitmap.Decode(second.PngBytes);
        Assert.False(firstBitmap.Bytes.SequenceEqual(secondBitmap.Bytes));
    }

    [Theory]
    [InlineData("red")]
    [InlineData("green")]
    [InlineData("yellow")]
    public void AllSupportedColorsRenderAtMaximumVariation(string color)
    {
        var scene = SceneGenerator.Generate(new($"a {color} apple on a wooden table", Variation: 1, Width: 128, Height: 128)).Scene!;
        using var bitmap = SKBitmap.Decode(StillLifeRenderer.Render(scene).PngBytes);
        Assert.NotNull(bitmap);
        Assert.Equal((byte)255, bitmap.GetPixel(64, 64).Alpha);
    }
}
