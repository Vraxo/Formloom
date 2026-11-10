namespace Formloom.Rendering.Skia.Models;

/// <summary>Controls generic model-geometry detail and silhouette rendering.</summary>
/// <param name="Antialiasing">Whether edges are antialiased.</param>
/// <param name="IncludeDetails">Whether paths classified as detail are drawn.</param>
/// <param name="Silhouette">Whether primary geometry is rendered as a solid black silhouette.</param>
public sealed record ModelGeometryRenderOptions(
    bool Antialiasing = true,
    bool IncludeDetails = true,
    bool Silhouette = false);
