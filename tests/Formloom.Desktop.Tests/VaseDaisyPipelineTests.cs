using Formloom.Desktop;
using Formloom.Rendering.Skia;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Formloom.Desktop.Tests;

public sealed class VaseDaisyPipelineTests
{
    [Theory]
    [InlineData("a white daisy in a blue ceramic vase on a wooden table", "white-daisy-in-blue-vase")]
    [InlineData("a white daisy in a cream ceramic vase on a wooden table", "white-daisy-in-cream-vase")]
    public async Task DaisyPromptsUseTheRealPipelineAndKeepThePreviewOnRejection(string prompt, string subject)
    {
        var generator = new GenerationService(NullLogger<GenerationService>.Instance);
        RenderedImage? saved = null;
        ulong savedSeed = 0;
        var session = new DesktopSession(
            generator.GenerateAsync,
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

        // Unsupported daisy phrasings are rejected whole and must not disturb the preview or the
        // metadata that names it. The list covers colour, species, container, relationship
        // wording, quantity and a second object.
        foreach (var rejected in new[]
        {
            "a pink daisy in a blue ceramic vase on a wooden table",
            "a red daisy in a blue ceramic vase on a wooden table",
            "a white daisy in a blue ceramic bowl on a wooden table",
            "a white daisy in a cream ceramic cup on a wooden table",
            "a blue ceramic vase with a white daisy on a wooden table",
            "two white daisies in a blue ceramic vase on a wooden table",
            "a white daisy in a blue ceramic vase on a wooden table and a red apple",
            "a white daisy in a wooden table",
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
    public async Task EmptyVasesTulipsAndDaisiesKeepDistinctMetadata()
    {
        var generator = new GenerationService(NullLogger<GenerationService>.Instance);
        var session = new DesktopSession(
            generator.GenerateAsync,
            (_, _) => Task.FromResult(new SaveResult(false, "Saved")),
            NullLogger<DesktopSession>.Instance);

        session.Prompt = "a blue ceramic vase on a wooden table";
        await session.GenerateAsync();
        Assert.Null(session.Error);
        var empty = session.Image!;
        Assert.Equal("ceramic-vase", empty.Subject);

        session.Prompt = "a red tulip in a blue ceramic vase on a wooden table";
        await session.GenerateAsync();
        Assert.Null(session.Error);
        var tulip = session.Image!;
        Assert.Equal("red-tulip-in-blue-vase", tulip.Subject);

        session.Prompt = "a white daisy in a blue ceramic vase on a wooden table";
        await session.GenerateAsync();
        Assert.Null(session.Error);
        var daisy = session.Image!;
        Assert.Equal("white-daisy-in-blue-vase", daisy.Subject);

        // Three genuinely different images and three different file names for the same vase.
        Assert.NotEqual(empty.PngBytes, daisy.PngBytes);
        Assert.NotEqual(tulip.PngBytes, daisy.PngBytes);
        Assert.NotEqual(
            PngSaveService.SuggestedFileName(tulip, 42),
            PngSaveService.SuggestedFileName(daisy, 42));
        Assert.NotEqual(
            PngSaveService.SuggestedFileName(empty, 42),
            PngSaveService.SuggestedFileName(daisy, 42));
    }

    [Fact]
    public void SaveNamesWhitelistDaisyTokens()
    {
        string[] tokens =
        [
            "white-daisy-in-blue-vase",
            "white-daisy-in-cream-vase",
        ];
        foreach (var token in tokens)
        {
            var image = new RenderedImage(64, 64, [], token);
            Assert.Equal($"formloom-{token}-seed-11.png", PngSaveService.SuggestedFileName(image, 11));
        }

        var hostile = new RenderedImage(64, 64, [], "white-daisy-in-blue-vase-on-a-prompt");
        Assert.Equal("formloom-fruit-seed-11.png", PngSaveService.SuggestedFileName(hostile, 11));

        // Every earlier token is untouched, the tulip ones included.
        Assert.Equal("formloom-red-tulip-in-blue-vase-seed-11.png", PngSaveService.SuggestedFileName(new RenderedImage(64, 64, [], "red-tulip-in-blue-vase"), 11));
        Assert.Equal("formloom-yellow-tulip-in-cream-vase-seed-11.png", PngSaveService.SuggestedFileName(new RenderedImage(64, 64, [], "yellow-tulip-in-cream-vase"), 11));
        Assert.Equal("formloom-ceramic-vase-seed-11.png", PngSaveService.SuggestedFileName(new RenderedImage(64, 64, [], "ceramic-vase"), 11));
        Assert.Equal("formloom-coffee-in-blue-cup-seed-11.png", PngSaveService.SuggestedFileName(new RenderedImage(64, 64, [], "coffee-in-blue-cup"), 11));
        Assert.Equal("formloom-apple-in-cream-bowl-seed-11.png", PngSaveService.SuggestedFileName(new RenderedImage(64, 64, [], "apple-in-cream-bowl"), 11));
    }
}
