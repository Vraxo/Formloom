using Formloom.Core;
using Formloom.Desktop;
using Formloom.Rendering.Skia;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Formloom.Desktop.Tests;

public sealed class GenerationServiceTests
{
    [Fact]
    public async Task RealPipelineProducesPreviewFromTheSamePngBytes()
    {
        var service = new GenerationService(NullLogger<GenerationService>.Instance);
        var result = await service.GenerateAsync(new GenerationRequest("a green apple on a wooden table", Width: 64, Height: 64));
        Assert.Null(result.Error);
        Assert.NotNull(result.Image);
        Assert.Equal(64, result.Image.Width);
        Assert.Equal("data:image/png;base64," + Convert.ToBase64String(result.Image.PngBytes), result.PreviewUrl);
    }

    [Fact]
    public async Task OptionalAntialiasingSettingChangesOnlyRenderedPixels()
    {
        var service = new GenerationService(NullLogger<GenerationService>.Instance);
        var request = new GenerationRequest("a green apple on a wooden table", Width: 64, Height: 64);
        var smooth = await service.GenerateAsync(request);
        var hardEdged = await service.GenerateAsync(request, new RenderOptions(Antialiasing: false));

        Assert.Null(smooth.Error);
        Assert.Null(hardEdged.Error);
        var smoothImage = Assert.IsType<RenderedImage>(smooth.Image);
        var hardEdgedImage = Assert.IsType<RenderedImage>(hardEdged.Image);
        Assert.Equal(smoothImage.Subject, hardEdgedImage.Subject);
        Assert.Equal(smoothImage.Width, hardEdgedImage.Width);
        Assert.Equal(smoothImage.Height, hardEdgedImage.Height);
        Assert.NotEqual(smoothImage.PngBytes, hardEdgedImage.PngBytes);
        Assert.Equal($"data:image/png;base64,{Convert.ToBase64String(hardEdgedImage.PngBytes)}", hardEdged.PreviewUrl);
    }

    [Theory]
    [InlineData("a green pear on a wooden table", "pear")]
    [InlineData("a yellow pear on a wooden table", "pear")]
    [InlineData("an orange on a wooden table", "orange")]
    [InlineData("a red apple and a green pear on a wooden table", "fruit-group")]
    [InlineData("a red apple, a green pear, and an orange on a wooden table", "fruit-group")]
    public async Task NewFruitUsesRealPipelineAndSaveSnapshot(string prompt, string subject)
    {
        var generator = new GenerationService(NullLogger<GenerationService>.Instance);
        Formloom.Rendering.Skia.RenderedImage? saved = null;
        ulong savedSeed = 0;
        var session = new DesktopSession(
            (request, options) => generator.GenerateAsync(request, options),
            (image, seed) =>
            {
                saved = image;
                savedSeed = seed;
                return Task.FromResult(new SaveResult(false, "Saved"));
            },
            NullLogger<DesktopSession>.Instance);
        session.Prompt = prompt;
        await session.GenerateAsync();
        Assert.Null(session.Error);
        Assert.NotNull(session.Image);
        Assert.Equal(subject, session.Image.Subject);
        var preview = session.PreviewUrl;
        session.Prompt = subject == "fruit-group"
            ? "a red apple and a blue pear on a wooden table"
            : "a red pear on a wooden table";
        session.SeedText = "99";
        await session.GenerateAsync();
        Assert.NotNull(session.Error);
        Assert.Equal(preview, session.PreviewUrl);
        await session.SaveAsync();
        Assert.NotNull(saved);
        Assert.Equal(subject, saved.Subject);
        Assert.Equal(42UL, savedSeed);
        Assert.Equal($"formloom-{subject}-seed-42.png", PngSaveService.SuggestedFileName(saved, savedSeed));
    }

    [Fact]
    public async Task RealParserFailureReturnsErrorWithoutPreview()
    {
        var service = new GenerationService(NullLogger<GenerationService>.Instance);
        var result = await service.GenerateAsync(new GenerationRequest("a pear on a glass table"));
        Assert.NotNull(result.Error);
        Assert.Null(result.Image);
        Assert.Null(result.PreviewUrl);
    }
}
