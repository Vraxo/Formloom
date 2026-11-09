using Formloom.Core;
using Formloom.Desktop;
using Formloom.Rendering.Skia;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Formloom.Desktop.Tests;

public sealed class VaseRosePipelineTests
{
    [Theory]
    [InlineData("a red rose in a blue ceramic vase on a wooden table", "red-rose-in-blue-vase")]
    [InlineData("a red rose in a cream ceramic vase on a wooden table", "red-rose-in-cream-vase")]
    [InlineData("a pink rose in a blue ceramic vase on a wooden table", "pink-rose-in-blue-vase")]
    [InlineData("a pink rose in a cream ceramic vase on a wooden table", "pink-rose-in-cream-vase")]
    public async Task RosePromptsUseTheRealPipelineAndKeepTheLastSuccessfulPreview(
        string prompt,
        string subject)
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
        Assert.Equal(prompt, session.ImageDescription);
        Assert.Equal(42UL, session.RenderedSeed);
        Assert.Equal(0.35, session.RenderedVariation);
        var preview = session.PreviewUrl;

        foreach (var rejected in new[]
        {
            "a rose in a blue ceramic vase on a wooden table",
            "a white rose in a cream ceramic vase on a wooden table",
            "a yellow rose in a blue ceramic vase on a wooden table",
            "a purple rose in a cream ceramic vase on a wooden table",
            "two red roses in a blue ceramic vase on a wooden table",
            "a bouquet of pink roses in a cream ceramic vase on a wooden table",
            "a red rose and a white daisy in a blue ceramic vase on a wooden table",
            "a pink rose in a blue ceramic bowl on a wooden table",
            "a red rose in a cream ceramic cup on a wooden table",
            "a blue ceramic vase with a pink rose on a wooden table",
            "a pink rose in a blue ceramic vase on a wooden table and a red apple",
            "a red rose in a blue ceramic vase",
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
            Assert.Equal(prompt, session.ImageDescription);
            Assert.Equal(subject, session.Image!.Subject);
        }

        await session.SaveAsync();
        Assert.NotNull(saved);
        Assert.Equal(subject, saved.Subject);
        Assert.Equal(42UL, savedSeed);
        Assert.Equal($"formloom-{subject}-seed-42.png", PngSaveService.SuggestedFileName(saved, savedSeed));
    }

    [Fact]
    public void SaveWhitelistAddsExactlyTheFourRoseTokensAndPreservesExistingNames()
    {
        string[] roseTokens =
        [
            "red-rose-in-blue-vase",
            "red-rose-in-cream-vase",
            "pink-rose-in-blue-vase",
            "pink-rose-in-cream-vase",
        ];
        foreach (var token in roseTokens)
        {
            var image = new RenderedImage(64, 64, [], token);
            Assert.Equal($"formloom-{token}-seed-42.png", PngSaveService.SuggestedFileName(image, 42));
        }

        var hostile = new RenderedImage(64, 64, [], "pink-rose-in-blue-vase-on-a-wooden-table");
        Assert.Equal("formloom-fruit-seed-42.png", PngSaveService.SuggestedFileName(hostile, 42));
        Assert.Equal("formloom-red-tulip-in-blue-vase-seed-42.png", PngSaveService.SuggestedFileName(new RenderedImage(64, 64, [], "red-tulip-in-blue-vase"), 42));
        Assert.Equal("formloom-white-daisy-in-cream-vase-seed-42.png", PngSaveService.SuggestedFileName(new RenderedImage(64, 64, [], "white-daisy-in-cream-vase"), 42));
        Assert.Equal("formloom-red-tulip-and-white-daisy-in-blue-vase-seed-42.png", PngSaveService.SuggestedFileName(new RenderedImage(64, 64, [], "red-tulip-and-white-daisy-in-blue-vase"), 42));
        Assert.Equal("formloom-ceramic-vase-seed-42.png", PngSaveService.SuggestedFileName(new RenderedImage(64, 64, [], "ceramic-vase"), 42));
        Assert.Equal("formloom-fruit-seed-42.png", PngSaveService.SuggestedFileName(new RenderedImage(64, 64, [], "red-rose-in-purple-vase"), 42));
    }
}
