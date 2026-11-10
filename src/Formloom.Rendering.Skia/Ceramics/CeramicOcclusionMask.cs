using Formloom.Core.Ceramics;
using SkiaSharp;

namespace Formloom.Rendering.Skia.Ceramics;

/// <summary>Builds host-authored occlusion regions from ceramic vessel geometry.</summary>
public static class CeramicOcclusionMask
{
    private const float BezierCircle = 0.55228475f;
    private const float RimExpansion = 1.1f;

    /// <summary>
    /// Creates a scene-normalized region below the front lip of a vase.
    /// Pass the returned path to <see cref="Models.ModelGeometryRenderOptions.OcclusionMask"/>
    /// when rendering a model mounted in that vase.
    /// </summary>
    /// <param name="vase">The vase whose mouth defines the occlusion boundary.</param>
    /// <returns>A caller-owned path covering the scene below the near rim.</returns>
    public static SKPath BelowVaseNearRim(CeramicInstance vase)
    {
        ArgumentNullException.ThrowIfNull(vase);
        if (vase.Kind != CeramicKind.Vase)
        {
            throw new ArgumentException("Near-rim occlusion requires a vase instance.", nameof(vase));
        }

        var profile = CeramicForms.For(vase.Kind);
        var body = vase.Body;
        var centerX = body.Center.X - (body.Size.X / 2) + (profile.MouthCenter.X * body.Size.X);
        var centerY = body.Center.Y - (body.Size.Y / 2) + (profile.MouthCenter.Y * body.Size.Y);
        var radiusX = (profile.MouthRadius.X + (profile.RimThickness * RimExpansion)) * body.Size.X;
        var radiusY = (profile.MouthRadius.Y + (profile.RimThickness * RimExpansion)) * body.Size.Y;
        var controlY = radiusY * BezierCircle;

        using var builder = new SKPathBuilder();
        builder.MoveTo(centerX - radiusX, centerY);
        builder.CubicTo(
            centerX - radiusX,
            centerY + controlY,
            centerX + radiusX,
            centerY + controlY,
            centerX + radiusX,
            centerY);
        builder.LineTo(1.5f, 1.5f);
        builder.LineTo(-0.5f, 1.5f);
        builder.Close();
        return builder.Detach();
    }
}
