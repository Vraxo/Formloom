using System.Globalization;
using Formloom.Desktop;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Formloom.Desktop.Tests;

public sealed class DesktopPipelineTests
{
    [Fact]
    public async Task DefaultSessionGeneratesRepeatableFullResolutionOutput()
    {
        var session = CreateSession();
        await session.GenerateAsync();
        Assert.Null(session.Error);
        Assert.NotNull(session.Image);
        Assert.Equal(1024, session.Image.Width);
        Assert.Equal(1024, session.Image.Height);
        Assert.Equal(42UL, session.RenderedSeed);
        Assert.Equal(0.35, session.RenderedVariation);
        var bytes = session.Image.PngBytes;
        var preview = session.PreviewUrl;
        await session.GenerateAsync();
        Assert.Null(session.Error);
        Assert.NotNull(session.Image);
        Assert.Equal(bytes, session.Image.PngBytes);
        Assert.Equal(preview, session.PreviewUrl);
    }

    [Fact]
    public async Task RealUnsupportedPromptPreservesLastImageAndMetadata()
    {
        var session = CreateSession();
        await session.GenerateAsync();
        Assert.NotNull(session.Image);
        var image = session.Image;
        var preview = session.PreviewUrl;
        session.Prompt = "a pear on a glass table";
        session.SeedText = "99";
        session.Variation = 1;
        await session.GenerateAsync();
        Assert.NotNull(session.Error);
        Assert.Contains("not supported", session.Error);
        Assert.Same(image, session.Image);
        Assert.Equal(preview, session.PreviewUrl);
        Assert.Equal(42UL, session.RenderedSeed);
        Assert.Equal(0.35, session.RenderedVariation);
        Assert.False(session.IsBusy);
    }

    [Fact]
    public async Task RandomizeSelectsInputWithoutRenderingUntilRequested()
    {
        var session = CreateSession();
        await session.GenerateAsync();
        Assert.NotNull(session.Image);
        var image = session.Image;
        session.RandomizeSeed();
        var selectedSeed = ulong.Parse(session.SeedText, CultureInfo.InvariantCulture);
        Assert.NotEqual(42UL, selectedSeed);
        Assert.Same(image, session.Image);
        Assert.Equal(42UL, session.RenderedSeed);
        await session.GenerateAsync();
        Assert.Null(session.Error);
        Assert.NotNull(session.Image);
        Assert.Equal(selectedSeed, session.RenderedSeed);
        Assert.False(image.PngBytes.SequenceEqual(session.Image.PngBytes));
    }

    private static DesktopSession CreateSession()
    {
        var generator = new GenerationService(NullLogger<GenerationService>.Instance);
        return new DesktopSession(
            (request, options) => generator.GenerateAsync(request, options),
            (_, _) => Task.FromResult(new SaveResult(false, "No dialog is opened by this test")),
            NullLogger<DesktopSession>.Instance);
    }
}
