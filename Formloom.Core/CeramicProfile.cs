using System.Collections.Immutable;
using System.Numerics;

namespace Formloom.Core;

// Normalized vessel geometry in the same 0..1, y-down space as the fruit contours.
// Outline is the closed exterior silhouette without the handle; the mouth opening,
// cavity depth, base width and rim thickness describe the open-vessel layers.
public sealed record CeramicProfile(
    CeramicKind Kind,
    ImmutableArray<CubicSegment> Outline,
    Vector2 MouthCenter,
    Vector2 MouthRadius,
    float CavityDepth,
    float RimThickness,
    float BaseBottom,
    float BaseWidth,
    CeramicHandle? Handle)
{
    public float OutlineTop => CeramicForms.OutlineExtent(Outline).Top;

    public float OutlineBottom => CeramicForms.OutlineExtent(Outline).Bottom;

    public float OutlineLeft => CeramicForms.OutlineExtent(Outline).Left;

    public float OutlineRight => CeramicForms.OutlineExtent(Outline).Right;

    public float MouthWidth => MouthRadius.X * 2;

    public float Height => OutlineBottom - OutlineTop;

    public float Width => OutlineRight - OutlineLeft;
}
