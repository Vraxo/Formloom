using Formloom.Desktop.PngExport;
using Formloom.Desktop.Studio;
using Formloom.Rendering.Skia.Scenes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Formloom.Desktop.Tests.Tulips;

public sealed class VaseFlowerPipelineTests
{
    [Theory]
    [InlineData("a red tulip in a blue ceramic vase on a wooden table", "red-tulip-in-blue-vase")]
    [InlineData("a red tulip in a cream ceramic vase on a wooden table", "red-tulip-in-cream-vase")]
    [InlineData("a yellow tulip in a blue ceramic vase on a wooden table", "yellow-tulip-in-blue-vase")]
    [InlineData("a yellow tulip in a cream ceramic vase on a wooden table", "yellow-tulip-in-cream-vase")]
    public async Task FlowerPromptsUseTheRealPipelineAndKeepThePreviewOnRejection(string prompt, string subject)
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

        // Unsupported flower phrasings are rejected whole and must not disturb the preview.
        foreach (var rejected in new[]
        {
            "a pink tulip in a blue ceramic vase on a wooden table",
            "a red tulip in a blue ceramic bowl on a wooden table",
            "a blue ceramic vase with a red tulip on a wooden table",
            "two red tulips in a blue ceramic vase on a wooden table",
            "a red tulip in a blue ceramic vase on a wooden table and a red apple",
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
    public async Task EmptyVasesAndPlantedVasesKeepDistinctMetadata()
    {
        var generator = new GenerationService(NullLogger<GenerationService>.Instance);
        var session = new DesktopSession(
            (request, options) => generator.GenerateAsync(request, options),
            (_, _) => Task.FromResult(new SaveResult(false, "Saved")),
            NullLogger<DesktopSession>.Instance);

        session.Prompt = "a blue ceramic vase on a wooden table";
        await session.GenerateAsync();
        Assert.Null(session.Error);
        Assert.Equal("ceramic-vase", session.Image!.Subject);

        session.Prompt = "a yellow tulip in a blue ceramic vase on a wooden table";
        await session.GenerateAsync();
        Assert.Null(session.Error);
        Assert.Equal("yellow-tulip-in-blue-vase", session.Image!.Subject);
        Assert.NotEqual(
            PngSaveService.SuggestedFileName(new RenderedImage(64, 64, [], "ceramic-vase"), 42),
            PngSaveService.SuggestedFileName(session.Image, 42));
    }

    [Fact]
    public void SaveNamesWhitelistFlowerTokens()
    {
        string[] tokens =
        [
            "red-tulip-in-blue-vase",
            "red-tulip-in-cream-vase",
            "yellow-tulip-in-blue-vase",
            "yellow-tulip-in-cream-vase",
        ];
        foreach (var token in tokens)
        {
            var image = new RenderedImage(64, 64, [], token);
            Assert.Equal($"formloom-{token}-seed-11.png", PngSaveService.SuggestedFileName(image, 11));
        }

        var hostile = new RenderedImage(64, 64, [], "red-tulip-in-blue-vase-on-a-prompt");
        Assert.Equal("formloom-fruit-seed-11.png", PngSaveService.SuggestedFileName(hostile, 11));

        // Existing tokens are untouched.
        Assert.Equal("formloom-ceramic-vase-seed-11.png", PngSaveService.SuggestedFileName(new RenderedImage(64, 64, [], "ceramic-vase"), 11));
        Assert.Equal("formloom-coffee-in-blue-cup-seed-11.png", PngSaveService.SuggestedFileName(new RenderedImage(64, 64, [], "coffee-in-blue-cup"), 11));
        Assert.Equal("formloom-apple-in-cream-bowl-seed-11.png", PngSaveService.SuggestedFileName(new RenderedImage(64, 64, [], "apple-in-cream-bowl"), 11));
    }
}
