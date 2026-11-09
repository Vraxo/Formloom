using System.Numerics;
using Formloom.Core.Ceramics;

namespace Formloom.Core.CupLiquids;

// Normalized geometry of a still liquid surface inside a cup's opening. Everything is
// derived from the cup profile, so no canvas pixel offset is hard-coded:
//
// * Center/Radius describe the drink's own ellipse. It is deliberately wider than the
//   inner opening, so the left and right extremes are governed by Clip, which makes the
//   drink meet the inner wall instead of floating inside a visible ceramic ring.
// * Clip is the inner opening ellipse, inset by the rim thickness, so the lip and the rim
//   stroke always stay in front of the boundary.
// * Center is sunk slightly below the mouth center, so a crescent of the far inner wall
//   stays visible above the drink and the cup reads as filled, not overflowing.
public readonly record struct CupLiquidSurface(
    Vector2 Center,
    Vector2 Radius,
    Vector2 ClipCenter,
    Vector2 ClipRadius)
{
    public float Top => Center.Y - Radius.Y;

    public float Bottom => Center.Y + Radius.Y;

    public float Left => Center.X - Radius.X;

    public float Right => Center.X + Radius.X;

    public static CupLiquidSurface For(CeramicProfile profile)
    {
        var rim = profile.RimThickness;
        var mouth = profile.MouthCenter;
        var outer = profile.MouthRadius;
        return new(
            new Vector2(mouth.X, mouth.Y + (rim * 0.6f)),
            new Vector2(outer.X + (rim * 3f), outer.Y - (rim * 1.15f)),
            mouth,
            new Vector2(outer.X - (rim * 1.1f), outer.Y - (rim * 1.1f)));
    }
}
