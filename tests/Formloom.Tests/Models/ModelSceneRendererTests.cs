using Formloom.Core.Models.Generation;
using Formloom.Core.Scenes;
using Formloom.Rendering.Skia.Models;
using Formloom.Rendering.Skia.Scenes;
using SkiaSharp;
using Xunit;

namespace Formloom.Tests.Models;

public sealed class ModelSceneRendererTests
{
    [Theory]
    [InlineData("a red rose in a blue ceramic vase on a wooden table", "model-flowers-rose-probe-in-blue-vase")]
    [InlineData("a pink rose in a cream ceramic vase on a wooden table", "model-flowers-rose-probe-in-cream-vase")]
    public void RendersCatalogModelWithItsGeneratedHostSceneAndExportsReviewPng(
        string prompt,
        string expectedSubject)
    {
        var generated = ModelSceneGenerator.Generate(
            new GenerationRequest(prompt, Seed: 42, Variation: 0.35, Width: 512, Height: 512),
            Path.Combine(AppContext.BaseDirectory, "Fixtures"));
        Assert.True(generated.IsSuccess, generated.Error);

        var rendered = ModelSceneRenderer.Render(
            generated.HostScene!,
            generated.Geometry!,
            generated.SubjectToken!);

        Assert.Equal(512, rendered.Width);
        Assert.Equal(512, rendered.Height);
        Assert.Equal(expectedSubject, rendered.Subject);
        using var bitmap = SKBitmap.Decode(rendered.PngBytes)
            ?? throw new InvalidOperationException("The renderer returned an invalid PNG.");
        Assert.Equal(512, bitmap.Width);
        Assert.Equal(512, bitmap.Height);
        Assert.Equal((byte)255, bitmap.GetPixel(256, 256).Alpha);

        var destination = Path.Combine(AppContext.BaseDirectory, expectedSubject + ".png");
        File.WriteAllBytes(destination, rendered.PngBytes);
    }
}
