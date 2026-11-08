using Formloom.Core;
using Formloom.Desktop;
using Formloom.Rendering.Skia;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Formloom.Desktop.Tests;

public sealed class CeramicPipelineTests
{
    [Theory]
    [InlineData("a blue ceramic bowl on a wooden table", "ceramic-bowl")]
    [InlineData("a cream ceramic cup on a wooden table", "ceramic-cup")]
    [InlineData("a blue ceramic vase on a wooden table", "ceramic-vase")]
    [InlineData("a red apple and a blue ceramic bowl on a wooden table", "still-life-group")]
    [InlineData("a green pear and a cream ceramic cup on a wooden table", "still-life-group")]
    [InlineData("a blue ceramic bowl and a cream ceramic cup on a wooden table", "ceramic-group")]
    [InlineData("a cream ceramic cup, a green pear, and a blue ceramic vase on a wooden table", "still-life-group")]
    public async Task VesselAndMixedPromptsUseTheRealPipelineAndSaveSnapshot(string prompt, string subject)
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

        session.Prompt = "a blue ceramic plate on a wooden table";
        session.SeedText = "99";
        session.Variation = 1;
        await session.GenerateAsync();
        Assert.NotNull(session.Error);
        Assert.Equal(preview, session.PreviewUrl);
        Assert.Equal(42UL, session.RenderedSeed);
        Assert.Equal(0.35, session.RenderedVariation);

        await session.SaveAsync();
        Assert.NotNull(saved);
        Assert.Equal(subject, saved.Subject);
        Assert.Equal(42UL, savedSeed);
        Assert.Equal($"formloom-{subject}-seed-42.png", PngSaveService.SuggestedFileName(saved, savedSeed));
    }

    [Fact]
    public void SaveNamesUseWhitelistedTokensInsteadOfPromptText()
    {
        string[] tokens =
        [
            "apple",
            "pear",
            "orange",
            "fruit-group",
            "ceramic-bowl",
            "ceramic-cup",
            "ceramic-vase",
            "ceramic-group",
            "still-life-group",
        ];
        foreach (var token in tokens)
        {
            var image = new RenderedImage(64, 64, [], token);
            Assert.Equal($"formloom-{token}-seed-7.png", PngSaveService.SuggestedFileName(image, 7));
        }

        var hostile = new RenderedImage(64, 64, [], "a blue ceramic bowl on a wooden table");
        Assert.Equal("formloom-fruit-seed-7.png", PngSaveService.SuggestedFileName(hostile, 7));
    }
}
