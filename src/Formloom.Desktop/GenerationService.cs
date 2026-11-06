using Formloom.Core;
using Formloom.Rendering.Skia;
using Microsoft.Extensions.Logging;

namespace Formloom.Desktop;

internal sealed class GenerationService(ILogger<GenerationService> logger)
{
    public async Task<DesktopGenerationResult> GenerateAsync(GenerationRequest request)
    {
        try
        {
            return await Task.Run(() =>
            {
                var generated = SceneGenerator.Generate(request);
                if (!generated.IsSuccess)
                {
                    return new DesktopGenerationResult(null, null, generated.Error);
                }

                var rendered = StillLifeRenderer.Render(generated.Scene!);
                var preview = "data:image/png;base64," + Convert.ToBase64String(rendered.PngBytes);
                return new DesktopGenerationResult(rendered, preview, null);
            });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Image generation failed for seed {Seed}", request.Seed);
            return new(null, null, "Could not generate the image. Try again or use a smaller canvas.");
        }
    }
}
