using Formloom.Rendering.Skia.Scenes;
using Microsoft.Extensions.Logging;
using Photino.NET;

namespace Formloom.Desktop.PngExport;

internal sealed class PngSaveService(PngFileWriter writer, ILogger<PngSaveService> logger)
{
    private static readonly string[] KnownSubjects =
    [
        "apple", "pear", "orange", "fruit-group",
        "ceramic-group", "still-life-group", "ceramic-bowl", "ceramic-cup", "ceramic-vase",
        "apple-in-blue-bowl", "apple-in-cream-bowl",
        "pear-in-blue-bowl", "pear-in-cream-bowl",
        "orange-in-blue-bowl", "orange-in-cream-bowl",
        "coffee-in-blue-cup", "coffee-in-cream-cup", "tea-in-blue-cup", "tea-in-cream-cup",
        "red-tulip-in-blue-vase", "red-tulip-in-cream-vase",
        "yellow-tulip-in-blue-vase", "yellow-tulip-in-cream-vase",
        "white-daisy-in-blue-vase", "white-daisy-in-cream-vase",
        "red-tulip-and-white-daisy-in-blue-vase", "red-tulip-and-white-daisy-in-cream-vase",
        "yellow-tulip-and-white-daisy-in-blue-vase", "yellow-tulip-and-white-daisy-in-cream-vase",
        "red-rose-in-blue-vase", "red-rose-in-cream-vase",
        "pink-rose-in-blue-vase", "pink-rose-in-cream-vase",
    ];

    private PhotinoWindow? window;

    public void Initialize(PhotinoWindow mainWindow) => window = mainWindow;

    public static string SuggestedFileName(RenderedImage image, ulong seed)
    {
        var subject = IsKnownSubject(image.Subject) ? image.Subject : "fruit";
        return FormattableString.Invariant($"formloom-{subject}-seed-{seed}.png");
    }

    public async Task<SaveResult> SaveAsync(RenderedImage image, ulong seed)
    {
        try
        {
            var host = window ?? throw new InvalidOperationException("The desktop window is not initialized.");
            var path = await host.ShowSaveFileAsync(
                title: "Save Formloom PNG",
                filters: [("PNG image", ["png"])],
                defaultFileName: SuggestedFileName(image, seed));
            return await writer.WriteAsync(path, image);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Saving the generated PNG failed");
            return new(true, "Could not save the PNG. Check the folder permissions and available disk space, then try again.");
        }
    }

    private static bool IsKnownSubject(string subject)
    {
        if (Array.IndexOf(KnownSubjects, subject) >= 0)
        {
            return true;
        }

        if (!subject.StartsWith("model-", StringComparison.Ordinal))
        {
            return false;
        }

        var suffix = subject.EndsWith("-in-blue-vase", StringComparison.Ordinal)
            ? "-in-blue-vase"
            : subject.EndsWith("-in-cream-vase", StringComparison.Ordinal)
                ? "-in-cream-vase"
                : string.Empty;
        if (suffix.Length == 0)
        {
            return false;
        }

        var modelId = subject["model-".Length..^suffix.Length];
        return modelId.Length > 0 &&
            modelId.Split('-').All(part =>
                part.Length > 0 && part.All(character => char.IsAsciiLetterOrDigit(character)));
    }
}
