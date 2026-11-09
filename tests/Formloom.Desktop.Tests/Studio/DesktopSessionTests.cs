using Formloom.Core.Scenes;
using Formloom.Desktop.PngExport;
using Formloom.Desktop.Studio;
using Formloom.Rendering.Skia.Scenes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Formloom.Desktop.Tests.Studio;

public sealed class DesktopSessionTests
{
    [Fact]
    public async Task AntialiasingDefaultsOnAndIsPassedAsRenderOnlyOption()
    {
        RenderOptions? capturedOptions = null;
        var session = new DesktopSession(
            (request, options) =>
            {
                capturedOptions = options;
                return Task.FromResult(Success());
            },
            (_, _) => Task.FromResult(new SaveResult(false, "Saved")),
            NullLogger<DesktopSession>.Instance);

        Assert.True(session.Antialiasing);
        await session.GenerateAsync();
        Assert.True(capturedOptions!.Antialiasing);
        Assert.True(session.RenderedAntialiasing);

        session.Antialiasing = false;
        await session.GenerateAsync();
        Assert.False(capturedOptions!.Antialiasing);
        Assert.False(session.RenderedAntialiasing);
    }

    [Fact]
    public async Task FailedPromptRetainsSuccessfulImageAndMetadata()
    {
        var session = CreateSession(request => Task.FromResult(request.Prompt == "unsupported"
            ? new DesktopGenerationResult(null, null, "Unsupported prompt")
            : Success()));
        await session.GenerateAsync();
        var previousImage = session.Image;
        session.Prompt = "unsupported";
        session.SeedText = "99";
        session.Variation = 1;
        await session.GenerateAsync();
        Assert.Same(previousImage, session.Image);
        Assert.Equal("preview", session.PreviewUrl);
        Assert.Equal(42UL, session.RenderedSeed);
        Assert.Equal(0.35, session.RenderedVariation);
        Assert.Equal("a red apple on a wooden table", session.ImageDescription);
        Assert.Equal("Unsupported prompt", session.Error);
        Assert.False(session.IsBusy);
    }

    [Fact]
    public async Task SaveUsesSuccessfulSnapshotAfterControlsAreEdited()
    {
        RenderedImage? savedImage = null;
        ulong savedSeed = 0;
        var session = CreateSession(
            _ => Task.FromResult(Success()),
            (image, seed) =>
            {
                savedImage = image;
                savedSeed = seed;
                return Task.FromResult(new SaveResult(false, "Saved"));
            });
        await session.GenerateAsync();
        session.SeedText = "900";
        session.Prompt = "edited";
        session.Variation = 1;
        await session.SaveAsync();
        Assert.Same(session.Image, savedImage);
        Assert.Equal(42UL, savedSeed);
        Assert.Equal(0.35, session.RenderedVariation);
    }

    [Fact]
    public async Task PendingGenerationGuardsActionsAndCapturesVisibleInputs()
    {
        var pending = new TaskCompletionSource<DesktopGenerationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        GenerationRequest? captured = null;
        var calls = 0;
        var saves = 0;
        var session = CreateSession(
            request =>
            {
                captured = request;
                calls++;
                return pending.Task;
            },
            (_, _) =>
            {
                saves++;
                return Task.FromResult(new SaveResult(false, "Saved"));
            });
        session.Prompt = "a yellow apple on a wooden table";
        session.SeedText = "18446744073709551615";
        session.Variation = 0.8;
        var generation = session.GenerateAsync();
        Assert.True(session.Generating);
        await session.GenerateAsync();
        await session.SaveAsync();
        session.RandomizeSeed();
        Assert.Equal("18446744073709551615", session.SeedText);
        Assert.Equal(1, calls);
        Assert.Equal(0, saves);
        session.Prompt = "edited while pending";
        session.SeedText = "77";
        session.Variation = 0.1;
        pending.SetResult(Success());
        await generation;
        Assert.NotNull(captured);
        Assert.Equal("a yellow apple on a wooden table", captured.Prompt);
        Assert.Equal("a yellow apple on a wooden table", session.ImageDescription);
        Assert.Equal(ulong.MaxValue, captured.Seed);
        Assert.Equal(0.8, session.RenderedVariation);
        Assert.Equal(ulong.MaxValue, session.RenderedSeed);
        Assert.False(session.IsBusy);
    }

    [Theory]
    [InlineData("")]
    [InlineData("-1")]
    [InlineData("18446744073709551616")]
    [InlineData("1.5")]
    public async Task InvalidSeedDoesNotCallGenerator(string seed)
    {
        var calls = 0;
        var session = CreateSession(_ =>
        {
            calls++;
            return Task.FromResult(Success());
        });
        session.SeedText = seed;
        await session.GenerateAsync();
        Assert.Equal(0, calls);
        Assert.NotNull(session.Error);
        Assert.False(session.IsBusy);
    }

    [Fact]
    public async Task IncompleteResultDoesNotClearPreviousImage()
    {
        var calls = 0;
        var session = CreateSession(_ => Task.FromResult(++calls == 1 ? Success() : new DesktopGenerationResult(null, null, null)));
        await session.GenerateAsync();
        var image = session.Image;
        await session.GenerateAsync();
        Assert.Same(image, session.Image);
        Assert.NotNull(session.Error);
        Assert.False(session.IsBusy);
    }

    [Fact]
    public async Task UnexpectedGenerationFailureReleasesGuardAndAllowsRetry()
    {
        var calls = 0;
        var session = CreateSession(_ => ++calls == 1
            ? Task.FromException<DesktopGenerationResult>(new InvalidOperationException("technical detail"))
            : Task.FromResult(Success()));
        await session.GenerateAsync();
        Assert.NotNull(session.Error);
        Assert.DoesNotContain("technical detail", session.Error);
        Assert.False(session.IsBusy);
        await session.GenerateAsync();
        Assert.NotNull(session.Image);
        Assert.Null(session.Error);
    }

    [Fact]
    public async Task PendingSaveGuardsActionsAndFailurePreservesImage()
    {
        var pending = new TaskCompletionSource<SaveResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var saves = 0;
        var generations = 0;
        var session = CreateSession(
            _ =>
            {
                generations++;
                return Task.FromResult(Success());
            },
            (_, _) =>
            {
                saves++;
                return pending.Task;
            });
        await session.GenerateAsync();
        var image = session.Image;
        var saving = session.SaveAsync();
        Assert.True(session.Saving);
        await session.SaveAsync();
        await session.GenerateAsync();
        Assert.Equal(1, saves);
        Assert.Equal(1, generations);
        pending.SetException(new IOException("technical detail"));
        await saving;
        Assert.Same(image, session.Image);
        Assert.False(session.IsBusy);
        Assert.NotNull(session.Error);
        Assert.DoesNotContain("technical detail", session.Error);
    }

    [Theory]
    [InlineData(false, "Save cancelled")]
    [InlineData(true, "Save failed")]
    public async Task SaveResultPreservesImageAndAllowsAnotherGeneration(bool isError, string message)
    {
        var session = CreateSession(
            _ => Task.FromResult(Success()),
            (_, _) => Task.FromResult(new SaveResult(isError, message)));
        await session.GenerateAsync();
        var image = session.Image;
        await session.SaveAsync();
        Assert.Same(image, session.Image);
        Assert.Equal(isError ? message : null, session.Error);
        Assert.False(session.IsBusy);
        await session.GenerateAsync();
        Assert.Null(session.Error);
    }

    [Fact]
    public async Task SaveBeforeGenerationIsIgnoredAndRandomizeChangesOnlyInput()
    {
        var saves = 0;
        var session = CreateSession(
            _ => Task.FromResult(Success()),
            (_, _) =>
            {
                saves++;
                return Task.FromResult(new SaveResult(false, "Saved"));
            });
        await session.SaveAsync();
        Assert.Equal(0, saves);
        await session.GenerateAsync();
        session.RandomizeSeed();
        Assert.NotEqual("42", session.SeedText);
        Assert.Equal(42UL, session.RenderedSeed);
        Assert.NotNull(session.Image);
    }

    private static DesktopGenerationResult Success() => new(new RenderedImage(64, 64, [1, 2, 3]), "preview", null);

    private static DesktopSession CreateSession(
        Func<GenerationRequest, Task<DesktopGenerationResult>> generate,
        Func<RenderedImage, ulong, Task<SaveResult>>? save = null) => new(
            generate,
            save ?? ((_, _) => Task.FromResult(new SaveResult(false, "Saved"))),
            NullLogger<DesktopSession>.Instance);
}
