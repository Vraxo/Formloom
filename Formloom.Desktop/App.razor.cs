using System.Globalization;
using System.Security.Cryptography;
using Formloom.Core;
using Formloom.Rendering.Skia;

namespace Formloom.Desktop;

public partial class App
{
    private string prompt = "a red apple on a wooden table";
    private string seedText = "42";
    private double variation = 0.35;
    private bool generating;
    private bool saving;
    private string? error;
    private string status = "Enter a prompt, then generate a still life.";
    private RenderedImage? image;
    private string? previewUrl;
    private string? imageDescription;
    private ulong renderedSeed;
    private double renderedVariation;

    private bool IsBusy => generating || saving;

    private async Task GenerateAsync()
    {
        if (IsBusy)
        {
            return;
        }

        error = null;
        if (!ulong.TryParse(seedText.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var seed))
        {
            error = "Enter a whole-number seed from 0 to 18446744073709551615.";
            status = "Generation did not start. Check the seed.";
            return;
        }

        var request = new GenerationRequest(prompt, seed, variation);
        generating = true;
        status = "Constructing your image…";
        try
        {
            var result = await Generator.GenerateAsync(request);
            if (result.Error is not null)
            {
                error = result.Error;
                status = image is null ? "No image generated yet." : "Previous image retained.";
                return;
            }

            image = result.Image;
            previewUrl = result.PreviewUrl;
            imageDescription = request.Prompt;
            renderedSeed = request.Seed;
            renderedVariation = request.Variation;
            status = "Image ready. Save PNG to keep this study.";
        }
        finally
        {
            generating = false;
        }
    }

    private void RandomizeSeed()
    {
        if (IsBusy)
        {
            return;
        }

        var hasPrevious = ulong.TryParse(seedText.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var previous);
        ulong next;
        do
        {
            next = BitConverter.ToUInt64(RandomNumberGenerator.GetBytes(sizeof(ulong)));
        }
        while (hasPrevious && next == previous);

        seedText = next.ToString(CultureInfo.InvariantCulture);

        status = "New seed selected. Generate to see the variation.";
    }

    private async Task SaveAsync()
    {
        if (IsBusy || image is null)
        {
            return;
        }

        saving = true;
        error = null;
        try
        {
            var result = await Saver.SaveAsync(image, renderedSeed);
            if (result.IsError)
            {
                error = result.Message;
            }

            status = result.IsError ? "Image retained. You can try saving again." : result.Message;
        }
        finally
        {
            saving = false;
        }
    }
}
