using Formloom.Core;
using Formloom.Desktop;
using Formloom.Rendering.Skia;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Formloom.Desktop.Tests;

public sealed class CupLiquidPipelineTests
{
    [Theory]
    [InlineData("a blue ceramic cup filled with coffee on a wooden table", "coffee-in-blue-cup")]
    [InlineData("a cream ceramic cup filled with coffee on a wooden table", "coffee-in-cream-cup")]
    [InlineData("a blue ceramic cup filled with tea on a wooden table", "tea-in-blue-cup")]
    [InlineData("a cream ceramic cup filled with tea on a wooden table", "tea-in-cream-cup")]
    public async Task FilledCupPromptsUseTheRealPipelineAndKeepThePreviewOnRejection(string prompt, string subject)
    {
        var generator = new GenerationService(NullLogger<GenerationService>.Instance);
        RenderedImage? saved = null;
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
        Assert.Equal(1024, session.Image.Width);
        Assert.Equal(1024, session.Image.Height);
        Assert.StartsWith("data:image/png;base64,", session.PreviewUrl);
        var preview = session.PreviewUrl;

        // Unsupported beverage phrasings are rejected whole and must not disturb the preview.
        foreach (var rejected in new[]
        {
            "a blue ceramic bowl filled with coffee on a wooden table",
            "a blue ceramic cup containing coffee on a wooden table",
            "coffee in a blue ceramic cup on a wooden table",
            "a blue ceramic cup filled with milk on a wooden table",
            "a blue ceramic cup filled with coffee on a wooden table and a red apple",
        })
        {
            session.Prompt = rejected;
            session.SeedText = "99";
            session.Variation = 1;
            await session.GenerateAsync();
            Assert.NotNull(session.Error);
            Assert.Equal(preview, session.PreviewUrl);
            Assert.Equal(42UL, session.RenderedSeed);
            Assert.Equal(0.35, session.RenderedVariation);
        }

        await session.SaveAsync();
        Assert.NotNull(saved);
        Assert.Equal(subject, saved.Subject);
        Assert.Equal(42UL, savedSeed);
        Assert.Equal($"formloom-{subject}-seed-42.png", PngSaveService.SuggestedFileName(saved, savedSeed));
    }

    [Fact]
    public async Task EmptyCupsAndFilledCupsKeepDistinctMetadata()
    {
        var generator = new GenerationService(NullLogger<GenerationService>.Instance);
        var session = new DesktopSession(
            (request, options) => generator.GenerateAsync(request, options),
            (_, _) => Task.FromResult(new SaveResult(false, "Saved")),
            NullLogger<DesktopSession>.Instance);

        session.Prompt = "a blue ceramic cup on a wooden table";
        await session.GenerateAsync();
        Assert.Null(session.Error);
        Assert.Equal("ceramic-cup", session.Image!.Subject);

        session.Prompt = "a blue ceramic cup filled with coffee on a wooden table";
        await session.GenerateAsync();
        Assert.Null(session.Error);
        Assert.Equal("coffee-in-blue-cup", session.Image!.Subject);
        Assert.NotEqual(
            PngSaveService.SuggestedFileName(new RenderedImage(64, 64, [], "ceramic-cup"), 42),
            PngSaveService.SuggestedFileName(session.Image, 42));
    }

    [Fact]
    public void SaveNamesWhitelistCupLiquidTokens()
    {
        string[] tokens =
        [
            "coffee-in-blue-cup",
            "coffee-in-cream-cup",
            "tea-in-blue-cup",
            "tea-in-cream-cup",
        ];
        foreach (var token in tokens)
        {
            var image = new RenderedImage(64, 64, [], token);
            Assert.Equal($"formloom-{token}-seed-11.png", PngSaveService.SuggestedFileName(image, 11));
        }

        var hostile = new RenderedImage(64, 64, [], "coffee-in-blue-cup-on-a-prompt");
        Assert.Equal("formloom-fruit-seed-11.png", PngSaveService.SuggestedFileName(hostile, 11));

        // Existing tokens are untouched.
        Assert.Equal("formloom-ceramic-cup-seed-11.png", PngSaveService.SuggestedFileName(new RenderedImage(64, 64, [], "ceramic-cup"), 11));
        Assert.Equal("formloom-apple-in-blue-bowl-seed-11.png", PngSaveService.SuggestedFileName(new RenderedImage(64, 64, [], "apple-in-blue-bowl"), 11));
    }
}
