using Formloom.Core.Models.Geometry;
using Formloom.Core.Scenes;
using Formloom.Rendering.Skia.Ceramics;
using Formloom.Rendering.Skia.Scenes;
using SkiaSharp;

namespace Formloom.Rendering.Skia.Models;

/// <summary>Composes a generic evaluated model over its generated host still-life scene.</summary>
public static class ModelSceneRenderer
{
    /// <summary>Renders a host scene and catalog-evaluated model geometry into one PNG.</summary>
    public static RenderedImage Render(
        StillLifeScene hostScene,
        ModelGeometry geometry,
        string subjectToken,
        RenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(hostScene);
        ArgumentNullException.ThrowIfNull(geometry);
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectToken);

        var renderOptions = options ?? new RenderOptions();
        var hostImage = StillLifeRenderer.Render(hostScene, renderOptions);
        var vase = hostScene.Ceramics.Single();
        using var nearRimMask = CeramicOcclusionMask.BelowVaseNearRim(vase);
        var modelImage = ModelGeometryRenderer.RenderWithOptions(
            geometry,
            hostScene.Width,
            hostScene.Height,
            new ModelGeometryRenderOptions(Antialiasing: renderOptions.Antialiasing)
            {
                OcclusionMask = nearRimMask,
            });

        using var hostBitmap = SKBitmap.Decode(hostImage.PngBytes)
            ?? throw new InvalidOperationException("The host still-life PNG could not be decoded.");
        using var modelBitmap = SKBitmap.Decode(modelImage.PngBytes)
            ?? throw new InvalidOperationException("The generic model PNG could not be decoded.");
        var info = new SKImageInfo(hostScene.Width, hostScene.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info)
            ?? throw new InvalidOperationException("Could not allocate the composed raster surface.");
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        canvas.DrawBitmap(hostBitmap, 0, 0, new SKSamplingOptions(SKFilterMode.Linear));
        canvas.DrawBitmap(modelBitmap, 0, 0, new SKSamplingOptions(SKFilterMode.Linear));

        using var image = surface.Snapshot();
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return new(hostScene.Width, hostScene.Height, encoded.ToArray(), subjectToken);
    }
}
