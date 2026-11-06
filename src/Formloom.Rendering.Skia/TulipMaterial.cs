using Formloom.Core;
using SkiaSharp;

namespace Formloom.Rendering.Skia;

// Palette for the one supported flower. Core carries only the flower identity and the
// normalized contours; every petal, stem and leaf colour lives here, and none of it
// depends on the vase glaze, so blue and cream vases never recolour the flower. The stem
// and leaf greens are shared by both petal colours, so red and yellow differ only in the
// bloom.
internal sealed record TulipMaterial(
    SKColor PetalBase,
    SKColor PetalLight,
    SKColor PetalEdge,
    SKColor StemBase,
    SKColor StemLight,
    SKColor StemEdge,
    SKColor LeafBase,
    SKColor LeafLight,
    SKColor LeafEdge)
{
    private static readonly SKColor GreenBase = new(98, 124, 50);
    private static readonly SKColor GreenLight = new(172, 190, 92);
    private static readonly SKColor GreenEdge = new(54, 80, 32);
    private static readonly SKColor BladeBase = new(94, 128, 52);
    private static readonly SKColor BladeLight = new(162, 186, 96);
    private static readonly SKColor BladeEdge = new(48, 80, 34);

    public static TulipMaterial For(TulipColor color) => color == TulipColor.Yellow
        ? new(new SKColor(226, 174, 38), new SKColor(247, 214, 104), new SKColor(140, 94, 16), GreenBase, GreenLight, GreenEdge, BladeBase, BladeLight, BladeEdge)
        : new(new SKColor(178, 36, 34), new SKColor(226, 92, 68), new SKColor(92, 14, 20), GreenBase, GreenLight, GreenEdge, BladeBase, BladeLight, BladeEdge);
}
