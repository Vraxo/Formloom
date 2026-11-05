using Formloom.Rendering.Skia;
using Microsoft.Extensions.Logging;
using Photino.NET;

namespace Formloom.Desktop;

internal sealed class PngSaveService(PngFileWriter writer, ILogger<PngSaveService> logger)
{
    // Subject tokens the renderer can emit. Anything else falls back to "fruit", so a new
    // subject token has to be whitelisted here before it can reach a file name.
    private static readonly string[] KnownSubjects =
    [
        "apple", "pear", "orange", "fruit-group",
        "ceramic-group", "still-life-group", "ceramic-bowl", "ceramic-cup", "ceramic-vase",
        "apple-in-blue-bowl", "apple-in-cream-bowl",
        "pear-in-blue-bowl", "pear-in-cream-bowl",
        "orange-in-blue-bowl", "orange-in-cream-bowl",
    ];

    private PhotinoWindow? window;

    public void Initialize(PhotinoWindow mainWindow) => window = mainWindow;

    public static string SuggestedFileName(RenderedImage image, ulong seed)
    {
        var subject = Array.IndexOf(KnownSubjects, image.Subject) >= 0 ? image.Subject : "fruit";
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
}
