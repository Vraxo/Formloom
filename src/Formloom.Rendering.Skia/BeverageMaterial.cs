using Formloom.Core;
using SkiaSharp;

namespace Formloom.Rendering.Skia;

// Palette for the two supported still beverages. Core carries only the beverage identity
// and the normalized surface geometry; every colour, gradient and reflection lives here.
internal sealed record BeverageMaterial(
    SKColor Base,
    SKColor Light,
    SKColor Edge,
    SKColor Sheen,
    byte SheenAlpha,
    byte GlintAlpha)
{
    public static BeverageMaterial For(BeverageKind beverage) => beverage == BeverageKind.Coffee
        ? new(
            new SKColor(46, 26, 17),
            new SKColor(88, 51, 30),
            new SKColor(22, 12, 8),
            new SKColor(255, 216, 174),
            44,
            20)
        : new(
            new SKColor(142, 71, 18),
            new SKColor(198, 122, 38),
            new SKColor(84, 39, 9),
            new SKColor(255, 228, 170),
            52,
            24);
}
