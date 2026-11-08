namespace Formloom.Rendering.Skia;

/// <summary>
/// Rasterization options for a single render. Antialiasing is enabled by default; disabling it is
/// intended for pixel inspection and does not affect scene generation or geometry.
/// </summary>
public sealed record RenderOptions(bool Antialiasing = true);
