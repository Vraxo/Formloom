using Formloom.Rendering.Skia;

namespace Formloom.Desktop;

internal sealed record DesktopGenerationResult(RenderedImage? Image, string? PreviewUrl, string? Error);
