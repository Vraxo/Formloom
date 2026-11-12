using Formloom.Core.Scenes;
using Formloom.Desktop.PngExport;
using Formloom.Desktop.Studio;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Formloom.Desktop.Tests.Studio;

public sealed class CatalogModelGenerationTests
{
    [Theory]
    [InlineData("a red rose in a blue ceramic vase on a wooden table", "model-flowers-rose-in-blue-vase")]
    [InlineData("a pink rose in a cream ceramic vase on a wooden table", "model-flowers-rose-in-cream-vase")]
    public async Task DesktopPromptPipelineUsesTheDiscoveredModelDefinition(
        string prompt,
        string expectedSubject)
    {
        var service = new GenerationService(NullLogger<GenerationService>.Instance);
        var result = await service.GenerateAsync(
            new GenerationRequest(prompt, Seed: 42, Variation: 0.35, Width: 256, Height: 256));

        Assert.Null(result.Error);
        var image = Assert.IsType<Formloom.Rendering.Skia.Scenes.RenderedImage>(result.Image);
        Assert.Equal(expectedSubject, image.Subject);
        Assert.Equal(256, image.Width);
        Assert.Equal(256, image.Height);
        Assert.Equal(
            $"formloom-{expectedSubject}-seed-42.png",
            PngSaveService.SuggestedFileName(image, 42));
        Assert.Equal("data:image/png;base64," + Convert.ToBase64String(image.PngBytes), result.PreviewUrl);

        var artifactPath = Path.Combine(AppContext.BaseDirectory, expectedSubject + ".png");
        await File.WriteAllBytesAsync(artifactPath, image.PngBytes, TestContext.Current.CancellationToken);
    }
}
