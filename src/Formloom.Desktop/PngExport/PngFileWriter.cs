using Formloom.Rendering.Skia.Scenes;
using Microsoft.Extensions.Logging;

namespace Formloom.Desktop.PngExport;

internal sealed class PngFileWriter(ILogger<PngFileWriter> logger)
{
    public async Task<SaveResult> WriteAsync(string? path, RenderedImage image)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return new(false, "Save cancelled. Your image is still available.");
        }

        if (!string.Equals(Path.GetExtension(path), ".png", StringComparison.OrdinalIgnoreCase))
        {
            return new(true, "Choose a filename ending in .png and save again.");
        }

        string? temporaryPath = null;
        try
        {
            var destination = Path.GetFullPath(path);
            temporaryPath = Path.Combine(Path.GetDirectoryName(destination)!, $".formloom-{Guid.NewGuid():N}.tmp");
            await File.WriteAllBytesAsync(temporaryPath, image.PngBytes);

            // Stage in the same directory so a failed write does not truncate an existing image.
            File.Move(temporaryPath, destination, overwrite: true);
            return new(false, $"Saved {Path.GetFileName(destination)}.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Writing the generated PNG failed");
            return new(true, "Could not save the PNG. Check the folder permissions and available disk space, then try again.");
        }
        finally
        {
            if (temporaryPath is not null)
            {
                try
                {
                    File.Delete(temporaryPath);
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Could not remove temporary PNG file");
                }
            }
        }
    }
}
