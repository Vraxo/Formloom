using Formloom.Core;
using Formloom.Desktop;
using Formloom.Rendering.Skia;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Formloom.Desktop.Tests;

public sealed class ContainmentPipelineTests
{
    [Theory]
    [InlineData("a red apple in a blue ceramic bowl on a wooden table", "apple-in-blue-bowl")]
    [InlineData("a green apple in a cream ceramic bowl on a wooden table", "apple-in-cream-bowl")]
    [InlineData("a yellow apple in a blue ceramic bowl on a wooden table", "apple-in-blue-bowl")]
    [InlineData("a green pear in a cream ceramic bowl on a wooden table", "pear-in-cream-bowl")]
    [InlineData("a yellow pear in a blue ceramic bowl on a wooden table", "pear-in-blue-bowl")]
    [InlineData("an orange in a cream ceramic bowl on a wooden table", "orange-in-cream-bowl")]
    public async Task ContainedPromptsUseTheRealPipelineAndKeepThePreviewOnRejection(string prompt, string subject)
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

        // Unsupported containment phrasings are rejected and must not disturb the preview.
        foreach (var rejected in new[]
        {
            "a red apple in a blue ceramic cup on a wooden table",
            "a blue ceramic bowl with a red apple on a wooden table",
            "two red apples in a blue ceramic bowl on a wooden table",
            "a red apple in a blue ceramic bowl on a wooden table and a green pear",
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
    public void SaveNamesWhitelistContainmentTokens()
    {
        string[] tokens =
        [
            "apple-in-blue-bowl",
            "apple-in-cream-bowl",
            "pear-in-blue-bowl",
            "pear-in-cream-bowl",
            "orange-in-blue-bowl",
            "orange-in-cream-bowl",
        ];
        foreach (var token in tokens)
        {
            var image = new RenderedImage(64, 64, [], token);
            Assert.Equal($"formloom-{token}-seed-3.png", PngSaveService.SuggestedFileName(image, 3));
        }

        var hostile = new RenderedImage(64, 64, [], "apple-in-blue-bowl-on-a-prompt");
        Assert.Equal("formloom-fruit-seed-3.png", PngSaveService.SuggestedFileName(hostile, 3));
    }
}
