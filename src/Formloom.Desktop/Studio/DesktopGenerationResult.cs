using Formloom.Rendering.Skia.Scenes;

namespace Formloom.Desktop.Studio;

internal sealed record DesktopGenerationResult(RenderedImage? Image, string? PreviewUrl, string? Error);
