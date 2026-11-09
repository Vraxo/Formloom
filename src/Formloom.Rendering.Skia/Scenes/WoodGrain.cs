using Formloom.Core.Core;
using Formloom.Core.Scenes;
using SkiaSharp;

namespace Formloom.Rendering.Skia.Scenes;

internal static class WoodGrain
{
    public static void Draw(SKCanvas canvas, StillLifeScene scene, float horizon, bool antialias)
    {
        var random = Pcg32.ForStream(scene.Seed, "table.grain");
        var knotX = 790 + ((float)random.NextDouble() * 90);
        var knotY = horizon + ((1024 - horizon) * (0.45f + ((float)random.NextDouble() * 0.12f)));
        var strength = (float)scene.GrainStrength;
        using var paint = new SKPaint { IsAntialias = antialias, Style = SKPaintStyle.Stroke };
        var bandY = horizon - 40;
        while (bandY < 1080)
        {
            var depth = Math.Clamp((bandY - horizon) / (1024 - horizon), 0, 1);
            paint.StrokeWidth = 7 + (depth * 10) + ((float)random.NextDouble() * 6);
            var alpha = (byte)(5 + (strength * 30));
            paint.Color = random.Chance(0.45)
                ? new SKColor(214, 165, 103, alpha)
                : new SKColor(78, 43, 23, alpha);
            using var band = CreateFlow(bandY, knotX, knotY);
            canvas.DrawPath(band, paint);
            bandY += 15 + (depth * 12) + ((float)random.NextDouble() * 12);
        }

        var grainY = horizon - 35;
        while (grainY < 1080)
        {
            var depth = Math.Clamp((grainY - horizon) / (1024 - horizon), 0, 1);
            paint.StrokeWidth = 0.45f + (depth * 0.65f) + ((float)random.NextDouble() * 0.45f);
            paint.Color = new SKColor(66, 37, 22, (byte)(8 + (strength * 40) + (random.NextDouble() * 10)));
            using var grain = CreateFlow(grainY, knotX, knotY);
            canvas.DrawPath(grain, paint);
            grainY += 4 + (depth * 3) + ((float)random.NextDouble() * 7);
        }

        using var knotBuilder = new SKPathBuilder();
        knotBuilder.MoveTo(knotX - 24, knotY);
        knotBuilder.CubicTo(knotX - 7, knotY - 8, knotX + 12, knotY - 5, knotX + 26, knotY);
        knotBuilder.CubicTo(knotX + 8, knotY + 6, knotX - 10, knotY + 7, knotX - 24, knotY);
        knotBuilder.Close();
        using var knot = knotBuilder.Detach();
        using var shading = SKShader.CreateLinearGradient(
            new SKPoint(knotX, knotY - 7), new SKPoint(knotX, knotY + 7),
            [new SKColor(71, 40, 24, 80), new SKColor(49, 28, 18, 140), new SKColor(153, 99, 54, 55)],
            [0f, 0.55f, 1f], SKShaderTileMode.Clamp);
        paint.Style = SKPaintStyle.Fill;
        paint.Color = SKColors.White;
        paint.Shader = shading;
        canvas.DrawPath(knot, paint);
    }

    private static SKPath CreateFlow(float baseline, float knotX, float knotY)
    {
        using var builder = new SKPathBuilder();
        for (var index = 0; index <= 68; index++)
        {
            var x = -32 + (index * 16);
            var dx = x - knotX;
            var dy = baseline - knotY;
            var wave = (MathF.Sin((dx * 0.0045f) + (baseline * 0.012f)) - MathF.Sin(baseline * 0.012f)) * 5;

            // Every band follows one shared flow field, splitting around the knot instead of crossing it.
            var envelope = MathF.Exp(-((dx * dx) / 10000) - (MathF.Abs(dy) / 24));
            var deflection = (dy < 0 ? -1 : 1) * 13 * envelope;
            var y = baseline - (dx * 0.037f) + wave + deflection;
            if (index == 0)
            {
                builder.MoveTo(x, y);
            }
            else
            {
                builder.LineTo(x, y);
            }
        }

        return builder.Detach();
    }
}
