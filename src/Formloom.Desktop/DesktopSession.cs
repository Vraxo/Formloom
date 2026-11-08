using System.Globalization;
using System.Security.Cryptography;
using Formloom.Core;
using Formloom.Rendering.Skia;
using Microsoft.Extensions.Logging;

namespace Formloom.Desktop;

internal sealed class DesktopSession(
    Func<GenerationRequest, RenderOptions?, Task<DesktopGenerationResult>> generate,
    Func<RenderedImage, ulong, Task<SaveResult>> save,
    ILogger<DesktopSession> logger)
{
    public DesktopSession(
        Func<GenerationRequest, Task<DesktopGenerationResult>> generate,
        Func<RenderedImage, ulong, Task<SaveResult>> save,
        ILogger<DesktopSession> logger)
        : this((request, _) => generate(request), save, logger)
    {
    }

    public string Prompt { get; set; } = "a red apple on a wooden table";

    public string SeedText { get; set; } = "42";

    public double Variation { get; set; } = 0.35;

    public bool Antialiasing { get; set; } = true;

    public bool Generating { get; private set; }

    public bool Saving { get; private set; }

    public string? Error { get; private set; }

    public string Status { get; private set; } = "Enter a prompt, then generate a still life.";

    public RenderedImage? Image { get; private set; }

    public string? PreviewUrl { get; private set; }

    public string? ImageDescription { get; private set; }

    public ulong RenderedSeed { get; private set; }

    public double RenderedVariation { get; private set; }

    public bool RenderedAntialiasing { get; private set; } = true;

    public string RenderedAntialiasingStatus => RenderedAntialiasing ? "on" : "off";

    public bool IsBusy => Generating || Saving;

    public async Task GenerateAsync()
    {
        if (IsBusy)
        {
            return;
        }

        Error = null;
        if (!ulong.TryParse(SeedText.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var seed))
        {
            Error = "Enter a whole-number seed from 0 to 18446744073709551615.";
            Status = "Generation did not start. Check the seed.";
            return;
        }

        var request = new GenerationRequest(Prompt, seed, Variation);
        var renderOptions = new RenderOptions(Antialiasing);
        Generating = true;
        Status = "Constructing your image…";
        try
        {
            var result = await generate(request, renderOptions);
            if (result.Error is not null || result.Image is null || result.PreviewUrl is null)
            {
                Error = result.Error ?? "Could not generate a complete image. Please try again.";
                Status = Image is null ? "No image generated yet." : "Previous image retained.";
                return;
            }

            Image = result.Image;
            PreviewUrl = result.PreviewUrl;
            ImageDescription = request.Prompt;
            RenderedSeed = request.Seed;
            RenderedVariation = request.Variation;
            RenderedAntialiasing = renderOptions.Antialiasing;
            Status = "Image ready. Save PNG to keep this study.";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Desktop generation interaction failed");
            Error = "Could not generate the image. Please try again.";
            Status = Image is null ? "No image generated yet." : "Previous image retained.";
        }
        finally
        {
            Generating = false;
        }
    }

    public void RandomizeSeed()
    {
        if (IsBusy)
        {
            return;
        }

        var hasPrevious = ulong.TryParse(SeedText.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var previous);
        ulong next;
        do
        {
            next = BitConverter.ToUInt64(RandomNumberGenerator.GetBytes(sizeof(ulong)));
        }
        while (hasPrevious && next == previous);

        SeedText = next.ToString(CultureInfo.InvariantCulture);

        Status = "New seed selected. Generate to see the variation.";
    }

    public async Task SaveAsync()
    {
        if (IsBusy || Image is null)
        {
            return;
        }

        Saving = true;
        Error = null;
        try
        {
            var result = await save(Image, RenderedSeed);
            if (result.IsError)
            {
                Error = result.Message;
            }

            Status = result.IsError ? "Image retained. You can try saving again." : result.Message;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Desktop save interaction failed");
            Error = "Could not save the PNG. Please try again.";
            Status = "Image retained. You can try saving again.";
        }
        finally
        {
            Saving = false;
        }
    }
}
