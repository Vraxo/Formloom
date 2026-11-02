using Formloom.Rendering.Skia;
using Microsoft.Extensions.Logging;
using Photino.NET;

namespace Formloom.Desktop;

internal sealed class PngSaveService(PngFileWriter writer, ILogger<PngSaveService> logger)
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
            return await writer.WriteAsync(path, image);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Saving the generated PNG failed");
            return new(true, "Could not save the PNG. Check the folder permissions and available disk space, then try again.");
        }
    }
}
