using Formloom.Core;
using SkiaSharp;

namespace Formloom.Rendering.Skia;

/// <summary>Rose-only petal palettes with the renderer's shared illustrated greens.</summary>
internal sealed record RoseMaterial(
    SKColor PetalBase,
    SKColor PetalLight,
    SKColor PetalShadow,
    SKColor PetalEdge,
    SKColor PetalFold,
    SKColor StemBase,
    SKColor StemLight,
    SKColor StemEdge,
    SKColor LeafBase,
    SKColor LeafLight,
    SKColor LeafEdge)
{
    private static readonly SKColor StemGreen = new(98, 124, 50);
    private static readonly SKColor StemHighlight = new(172, 190, 92);
    private static readonly SKColor StemShadow = new(54, 80, 32);
    private static readonly SKColor LeafGreen = new(94, 128, 52);
    private static readonly SKColor LeafHighlight = new(162, 186, 96);
    private static readonly SKColor LeafShadow = new(48, 80, 34);

    private static readonly RoseMaterial Red = new(
        new SKColor(176, 34, 50),
        new SKColor(237, 93, 103),
        new SKColor(91, 17, 31),
        new SKColor(103, 21, 38),
        new SKColor(255, 155, 153),
        StemGreen,
        StemHighlight,
        StemShadow,
        LeafGreen,
        LeafHighlight,
        LeafShadow);

    private static readonly RoseMaterial Pink = new(
        new SKColor(208, 58, 116),
        new SKColor(255, 145, 190),
        new SKColor(111, 28, 69),
        new SKColor(132, 39, 80),
        new SKColor(255, 196, 218),
        StemGreen,
        StemHighlight,
        StemShadow,
        LeafGreen,
        LeafHighlight,
        LeafShadow);

    public static RoseMaterial For(RoseColor color) => color switch
    {
        RoseColor.Red => Red,
        RoseColor.Pink => Pink,
        _ => throw new ArgumentOutOfRangeException(nameof(color), color, "Unsupported rose colour."),
    };
}
