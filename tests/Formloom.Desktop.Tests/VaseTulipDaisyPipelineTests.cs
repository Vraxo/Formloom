using Formloom.Desktop;
using Formloom.Rendering.Skia;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Formloom.Desktop.Tests;

public sealed class VaseTulipDaisyPipelineTests
{
    [Theory]
    [InlineData("a red tulip and a white daisy in a blue ceramic vase on a wooden table", "red-tulip-and-white-daisy-in-blue-vase")]
    [InlineData("a red tulip and a white daisy in a cream ceramic vase on a wooden table", "red-tulip-and-white-daisy-in-cream-vase")]
    [InlineData("a yellow tulip and a white daisy in a blue ceramic vase on a wooden table", "yellow-tulip-and-white-daisy-in-blue-vase")]
    [InlineData("a yellow tulip and a white daisy in a cream ceramic vase on a wooden table", "yellow-tulip-and-white-daisy-in-cream-vase")]
    public async Task MixedPromptsUseTheRealPipelineAndRejectUnsupportedArrangementsWhole(string prompt, string subject)
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

        foreach (var rejected in new[]
        {
            "a white daisy and a red tulip in a blue ceramic vase on a wooden table",
            "a red tulip and a yellow tulip in a blue ceramic vase on a wooden table",
            "a red tulip and two white daisies in a blue ceramic vase on a wooden table",
            "a red tulip and a white daisy in a blue ceramic bowl on a wooden table",
            "a red tulip and a white daisy in a cream ceramic cup on a wooden table",
            "a red tulip and a white daisy in a blue ceramic vase on a wooden table and a red apple",
            "a blue ceramic vase with a red tulip and a white daisy on a wooden table",
            "a red tulip and a white daisy in a wooden table",
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
            Assert.Equal(subject, session.Image!.Subject);
        }

        await session.SaveAsync();
        Assert.NotNull(saved);
        Assert.Equal(subject, saved.Subject);
        Assert.Equal(42UL, savedSeed);
        Assert.Equal($"formloom-{subject}-seed-42.png", PngSaveService.SuggestedFileName(saved, savedSeed));
    }

    [Fact]
    public void SaveNamesWhitelistOnlyTheFourMixedTokensAndPreserveEarlierSubjects()
    {
        string[] tokens =
        [
            "red-tulip-and-white-daisy-in-blue-vase",
            "red-tulip-and-white-daisy-in-cream-vase",
            "yellow-tulip-and-white-daisy-in-blue-vase",
            "yellow-tulip-and-white-daisy-in-cream-vase",
        ];
        foreach (var token in tokens)
        {
            var image = new RenderedImage(64, 64, [], token);
            Assert.Equal($"formloom-{token}-seed-11.png", PngSaveService.SuggestedFileName(image, 11));
        }

        var hostile = new RenderedImage(64, 64, [], "red-tulip-and-white-daisy-in-blue-vase-on-a-prompt");
        Assert.Equal("formloom-fruit-seed-11.png", PngSaveService.SuggestedFileName(hostile, 11));
        Assert.Equal("formloom-red-tulip-in-blue-vase-seed-11.png", PngSaveService.SuggestedFileName(new RenderedImage(64, 64, [], "red-tulip-in-blue-vase"), 11));
        Assert.Equal("formloom-white-daisy-in-cream-vase-seed-11.png", PngSaveService.SuggestedFileName(new RenderedImage(64, 64, [], "white-daisy-in-cream-vase"), 11));
        Assert.Equal("formloom-ceramic-vase-seed-11.png", PngSaveService.SuggestedFileName(new RenderedImage(64, 64, [], "ceramic-vase"), 11));
        Assert.Equal("formloom-coffee-in-blue-cup-seed-11.png", PngSaveService.SuggestedFileName(new RenderedImage(64, 64, [], "coffee-in-blue-cup"), 11));
        Assert.Equal("formloom-apple-in-cream-bowl-seed-11.png", PngSaveService.SuggestedFileName(new RenderedImage(64, 64, [], "apple-in-cream-bowl"), 11));
    }
}
