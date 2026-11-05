using Bunit;
using Formloom.Core;
using Formloom.Desktop;
using Formloom.Rendering.Skia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Formloom.Desktop.Tests;

public sealed class AppComponentTests
{
    [Fact]
    public async Task InitialMarkupHasBoundDefaultsLabelsAndDisabledSave()
    {
        await using var context = new BunitContext();
        context.Services.AddSingleton(CreateSession(_ => Task.FromResult(Success())));
        var component = context.Render<App>();
        Assert.Equal("a red apple on a wooden table", component.Find("#prompt").GetAttribute("value"));
        Assert.Equal("42", component.Find("#seed").GetAttribute("value"));
        Assert.Equal("0.35", component.Find("#variation").GetAttribute("value"));
        Assert.True(component.Find(".secondary").HasAttribute("disabled"));
        Assert.Empty(component.FindAll(".preview img"));
        Assert.Single(component.FindAll("label[for='prompt']"));
        Assert.Single(component.FindAll("label[for='seed']"));
        Assert.Single(component.FindAll("label[for='variation']"));
        Assert.Equal("polite", component.Find("[role='status']").GetAttribute("aria-live"));
    }

    [Theory]
    [InlineData("a green apple on a wooden table")]
    [InlineData("a green pear on a wooden table")]
    [InlineData("a yellow pear on a wooden table")]
    [InlineData("an orange on a wooden table")]
    [InlineData("a red apple and a green pear on a wooden table")]
    [InlineData("a red apple, a green pear, and an orange on a wooden table")]
    [InlineData("a blue ceramic bowl on a wooden table")]
    [InlineData("a cream ceramic cup, a green pear, and a blue ceramic vase on a wooden table")]
    [InlineData("a red apple in a blue ceramic bowl on a wooden table")]
    [InlineData("a green pear in a cream ceramic bowl on a wooden table")]
    [InlineData("an orange in a blue ceramic bowl on a wooden table")]
    public async Task InputEventsAndSubmitUseEditedValuesAndRenderPreview(string prompt)
    {
        await using var context = new BunitContext();
        GenerationRequest? captured = null;
        context.Services.AddSingleton(CreateSession(request =>
        {
            captured = request;
            return Task.FromResult(Success());
        }));
        var component = context.Render<App>();
        await component.Find("#prompt").InputAsync(prompt);
        await component.Find("#seed").InputAsync("123");
        await component.Find("#variation").InputAsync("0.8");
        await component.Find("form").TriggerEventAsync("onsubmit", EventArgs.Empty);
        Assert.NotNull(captured);
        Assert.Equal(prompt, captured.Prompt);
        Assert.Equal(123UL, captured.Seed);
        Assert.Equal(0.8, captured.Variation);
        Assert.Equal(Success().PreviewUrl, component.Find(".preview img").GetAttribute("src"));
        Assert.Equal(captured.Prompt, component.Find(".preview img").GetAttribute("alt"));
        Assert.Contains("Seed 123", component.Find(".preview-footer").TextContent);
        Assert.False(component.Find(".secondary").HasAttribute("disabled"));
    }

    [Fact]
    public async Task PendingGenerationDisablesControlsAndFailureRetainsPreview()
    {
        await using var context = new BunitContext();
        var pending = new TaskCompletionSource<DesktopGenerationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        context.Services.AddSingleton(CreateSession(_ => ++calls == 1 ? Task.FromResult(Success()) : pending.Task));
        var component = context.Render<App>();
        await component.Find("form").TriggerEventAsync("onsubmit", EventArgs.Empty);
        var preview = component.Find(".preview img").GetAttribute("src");
        await component.Find("#prompt").InputAsync("unsupported");
        var submission = component.Find("form").TriggerEventAsync("onsubmit", EventArgs.Empty);
        try
        {
            component.WaitForAssertion(() =>
            {
                Assert.True(component.Find("fieldset").HasAttribute("disabled"));
                Assert.True(component.Find(".secondary").HasAttribute("disabled"));
                Assert.Contains("Generating", component.Find(".primary").TextContent);
            });
        }
        finally
        {
            pending.TrySetResult(new(null, null, "Unsupported prompt. Use an apple on a wooden table."));
        }

        await submission;
        Assert.Contains("Unsupported prompt", component.Find("[role='alert']").TextContent);
        Assert.Equal(preview, component.Find(".preview img").GetAttribute("src"));
        Assert.Contains("Seed 42", component.Find(".preview-footer").TextContent);
        Assert.False(component.Find("fieldset").HasAttribute("disabled"));
        Assert.False(component.Find(".secondary").HasAttribute("disabled"));
    }

    [Fact]
    public async Task InvalidSeedRendersAlertWithoutCallingGenerator()
    {
        await using var context = new BunitContext();
        var calls = 0;
        context.Services.AddSingleton(CreateSession(_ =>
        {
            calls++;
            return Task.FromResult(Success());
        }));
        var component = context.Render<App>();
        await component.Find("#seed").InputAsync("not a seed");
        await component.Find("form").TriggerEventAsync("onsubmit", EventArgs.Empty);
        Assert.Equal(0, calls);
        Assert.Contains("whole-number seed", component.Find("[role='alert']").TextContent);
        Assert.True(component.Find(".secondary").HasAttribute("disabled"));
    }

    [Fact]
    public async Task RandomizeClickUpdatesInputWithoutGeneratingAndSaveUsesImageSnapshot()
    {
        await using var context = new BunitContext();
        var calls = 0;
        ulong savedSeed = 0;
        context.Services.AddSingleton(CreateSession(
            _ =>
            {
                calls++;
                return Task.FromResult(Success());
            },
            (_, seed) =>
            {
                savedSeed = seed;
                return Task.FromResult(new SaveResult(false, "Save cancelled. Your image is still available."));
            }));
        var component = context.Render<App>();
        await component.Find("form").TriggerEventAsync("onsubmit", EventArgs.Empty);
        await component.Find(".text-button").TriggerEventAsync("onclick", new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        Assert.NotEqual("42", component.Find("#seed").GetAttribute("value"));
        Assert.Equal(1, calls);
        await component.Find("#seed").InputAsync("900");
        await component.Find("#variation").InputAsync("1");
        await component.Find(".secondary").TriggerEventAsync("onclick", new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        Assert.Equal(42UL, savedSeed);
        Assert.Contains("Seed 42", component.Find(".preview-footer").TextContent);
        Assert.Contains("cancelled", component.Find("[role='status']").TextContent);
        Assert.Single(component.FindAll(".preview img"));
        Assert.False(component.Find(".secondary").HasAttribute("disabled"));
    }

    [Fact]
    public async Task PendingSaveDisablesActionsAndShowsRecoverableFailure()
    {
        await using var context = new BunitContext();
        var pending = new TaskCompletionSource<SaveResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        context.Services.AddSingleton(CreateSession(_ => Task.FromResult(Success()), (_, _) => pending.Task));
        var component = context.Render<App>();
        await component.Find("form").TriggerEventAsync("onsubmit", EventArgs.Empty);
        var preview = component.Find(".preview img").GetAttribute("src");
        var saving = component.Find(".secondary").TriggerEventAsync("onclick", new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        try
        {
            component.WaitForAssertion(() =>
            {
                Assert.True(component.Find("fieldset").HasAttribute("disabled"));
                Assert.True(component.Find(".secondary").HasAttribute("disabled"));
                Assert.Contains("Saving", component.Find(".secondary").TextContent);
            });
        }
        finally
        {
            pending.TrySetResult(new(true, "Could not save the PNG. Choose a writable folder."));
        }

        await saving;
        Assert.Contains("writable folder", component.Find("[role='alert']").TextContent);
        Assert.Equal(preview, component.Find(".preview img").GetAttribute("src"));
        Assert.False(component.Find("fieldset").HasAttribute("disabled"));
        Assert.False(component.Find(".secondary").HasAttribute("disabled"));
    }

    private static DesktopGenerationResult Success() => new(new RenderedImage(64, 64, [1, 2, 3]), "data:image/png;base64,AQID", null);

    private static DesktopSession CreateSession(
        Func<GenerationRequest, Task<DesktopGenerationResult>> generate,
        Func<RenderedImage, ulong, Task<SaveResult>>? save = null) => new(
            generate,
            save ?? ((_, _) => Task.FromResult(new SaveResult(false, "Saved"))),
            NullLogger<DesktopSession>.Instance);
}
