using Formloom.Core.Fruits;
using Formloom.Core.Scenes;
using SkiaSharp;

namespace Formloom.Rendering.Skia.Fruits;

internal sealed record FruitMaterial(SKColor BaseColor, byte HighlightAlpha, float HighlightRadius, int DetailCount)
{
    public static FruitMaterial For(StillLifeScene scene)
    {
        if (scene.Subject == FruitKind.Orange)
        {
            return new(new SKColor(225, 113, 20), 85, 0.28f, 2200);
        }

        if (scene.Subject == FruitKind.Pear)
        {
            return new(scene.Color == FruitColor.Green ? new SKColor(136, 157, 57) : new SKColor(207, 174, 59), 60, 0.3f, 1500);
        }

        var color = scene.Color switch
        {
            FruitColor.Green => new SKColor(112, 153, 42),
            FruitColor.Yellow => new SKColor(218, 168, 40),
            _ => new SKColor(180, 38, 37),
        };
        return new(color, 125, 0.22f, 1100);
    }
}
