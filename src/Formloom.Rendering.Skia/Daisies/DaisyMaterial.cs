using Formloom.Core.Daisies;
using SkiaSharp;

namespace Formloom.Rendering.Skia.Daisies;

// Palette for the daisy. Core carries only the species, the fixed appearance value and the
// normalized contours; every petal, disk, stem and leaf colour lives here. The petals are white
// with a warm shade at the root and a cool edge, so they never read as flat white cut-outs, and
// the disk is golden with a lit dome. None of it depends on the vase glaze, so a blue and a
// cream vase never recolour the flower.
internal sealed record DaisyMaterial(
    SKColor PetalRoot,
    SKColor PetalBase,
    SKColor PetalLight,
    SKColor PetalEdge,
    SKColor DiskLight,
    SKColor DiskBase,
    SKColor DiskEdge,
    SKColor DiskRing,
    SKColor StemBase,
    SKColor StemLight,
    SKColor StemEdge,
    SKColor LeafBase,
    SKColor LeafLight,
    SKColor LeafEdge)
{
    private static readonly DaisyMaterial WhiteWithGoldenDisk = new(
        new SKColor(206, 206, 198),
        new SKColor(244, 243, 236),
        new SKColor(255, 254, 249),
        new SKColor(176, 180, 182),
        new SKColor(249, 208, 92),
        new SKColor(226, 174, 48),
        new SKColor(150, 104, 20),
        new SKColor(120, 82, 16),
        new SKColor(98, 124, 50),
        new SKColor(172, 190, 92),
        new SKColor(54, 80, 32),
        new SKColor(94, 128, 52),
        new SKColor(162, 186, 96),
        new SKColor(48, 80, 34));

    public static DaisyMaterial For(DaisyAppearance appearance) => appearance == DaisyAppearance.WhiteWithGoldenDisk
        ? WhiteWithGoldenDisk
        : throw new ArgumentOutOfRangeException(nameof(appearance), appearance, "Unsupported daisy appearance.");
}
