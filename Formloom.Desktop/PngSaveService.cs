using Formloom.Rendering.Skia;
using Microsoft.Extensions.Logging;
using Photino.NET;

namespace Formloom.Desktop;

internal sealed class PngSaveService(ILogger<PngSaveService> logger)
{
    private PhotinoWindow? window;

    public void Initialize(PhotinoWindow mainWindow) => window = mainWindow;

    public async Task<SaveResult> SaveAsync(RenderedImage image, ulong seed)
    {
        try
        {
            var host = window ?? throw new InvalidOperationException("The desktop window is not initialized.");
            var path = await host.ShowSaveFileAsync(
                title: "Save Formloom PNG",
                filters: [("PNG image", ["png"])],
                defaultFileName: $"formloom-apple-seed-{seed}.png");
            if (string.IsNullOrWhiteSpace(path))
            {
                return new(false, "Save cancelled. Your image is still available.");
            }

            if (!string.Equals(Path.GetExtension(path), ".png", StringComparison.OrdinalIgnoreCase))
            {
                return new(true, "Choose a filename ending in .png and save again.");
            }

            await File.WriteAllBytesAsync(path, image.PngBytes);
            return new(false, $"Saved {Path.GetFileName(path)}.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Saving the generated PNG failed");
            return new(true, "Could not save the PNG. Check the folder permissions and available disk space, then try again.");
        }
    }
}
