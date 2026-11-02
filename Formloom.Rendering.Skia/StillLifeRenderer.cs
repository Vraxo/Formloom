using Formloom.Core;
using Formloom.Core.Randomness;
using SkiaSharp;

namespace Formloom.Rendering.Skia;

public static class StillLifeRenderer
{
    public static RenderedImage Render(StillLifeScene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        if (scene.Width is < 64 or > 4096 || scene.Height is < 64 or > 4096)
        {
            throw new ArgumentOutOfRangeException(nameof(scene));
        }

        var info = new SKImageInfo(scene.Width, scene.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info) ?? throw new InvalidOperationException("Could not allocate raster surface.");
        var canvas = surface.Canvas;
        canvas.Scale(scene.Width / 1024f, scene.Height / 1024f);
        DrawSetting(canvas, scene);
        DrawApple(canvas, scene);
        using var image = surface.Snapshot();
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return new(scene.Width, scene.Height, encoded.ToArray());
    }

    private static void DrawSetting(SKCanvas canvas, StillLifeScene scene)
    {
        var warmth = (float)scene.BackgroundWarmth;
        using var background = SKShader.CreateLinearGradient(
            new SKPoint(0, 0), new SKPoint(1024, 900),
            [new SKColor((byte)(70 + (warmth * 20)), 89, 82), new SKColor(29, 43, 43)],
            null, SKShaderTileMode.Clamp);
        using var paint = new SKPaint { IsAntialias = true, Shader = background };
        canvas.DrawRect(0, 0, 1024, 1024, paint);
        var horizon = (float)scene.TableHorizon * 1024;
        using var wood = SKShader.CreateLinearGradient(
            new SKPoint(0, horizon), new SKPoint(200, 1024),
            [new SKColor(161, 111, 68), new SKColor(105, 64, 38)], null, SKShaderTileMode.Clamp);
        paint.Shader = wood;
        using var tableBuilder = new SKPathBuilder();
        tableBuilder.MoveTo(0, horizon + 20);
        tableBuilder.LineTo(1024, horizon - 20);
        tableBuilder.LineTo(1024, 1024);
        tableBuilder.LineTo(0, 1024);
        tableBuilder.Close();
        using var table = tableBuilder.Detach();
        canvas.DrawPath(table, paint);
        canvas.Save();
        canvas.ClipPath(table);
        paint.Shader = null;
        paint.Style = SKPaintStyle.Stroke;
        var random = Pcg32.ForStream(scene.Seed, "table.grain");
        for (var index = 0; index < 220; index++)
        {
            var y = horizon + ((float)random.NextDouble() * (1050 - horizon));
            var bend = 6 + ((float)random.NextDouble() * 24);
            paint.Color = new SKColor(62, 33, 18, (byte)(12 + (scene.GrainStrength * 65)));
            paint.StrokeWidth = 0.5f + ((float)random.NextDouble() * 2);
            using var grainBuilder = new SKPathBuilder();
            grainBuilder.MoveTo(-20, y);
            grainBuilder.CubicTo(260, y - bend, 650, y + bend, 1050, y - 38);
            using var grain = grainBuilder.Detach();
            canvas.DrawPath(grain, paint);
        }

        paint.Color = new SKColor(61, 32, 18, 55);
        paint.StrokeWidth = 1.2f;
        for (var ring = 1; ring < 12; ring++)
        {
            canvas.DrawOval(820, 828, ring * 5, ring * 1.5f, paint);
        }

        canvas.Restore();
    }

    private static void DrawApple(SKCanvas canvas, StillLifeScene scene)
    {
        var apple = scene.Apple;
        var centerX = apple.Center.X * 1024;
        var top = (apple.Center.Y - (apple.Size.Y / 2)) * 1024;
        var width = apple.Size.X * 1024;
        var height = apple.Size.Y * 1024;
        var left = centerX - (width / 2);
        var bottom = top + height;
        using var paint = new SKPaint { IsAntialias = true };
        using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 19);
        paint.MaskFilter = blur;
        paint.Color = new SKColor(12, 20, 17, 105);
        canvas.DrawOval(centerX - (scene.LightDirection.X * 85), bottom + 8, width * 0.65f, 33, paint);
        paint.MaskFilter = null;
        using var contact = SKShader.CreateRadialGradient(
            new SKPoint(centerX, bottom - 1), width * 0.42f,
            [new SKColor(23, 18, 12, 160), new SKColor(23, 18, 12, 0)], null, SKShaderTileMode.Clamp);
        paint.Shader = contact;
        canvas.DrawOval(centerX, bottom, width * 0.42f, 16, paint);
        paint.Shader = null;
        canvas.Save();
        canvas.Translate(left, top);
        canvas.Scale(width, height);
        var asymmetry = (float)apple.Asymmetry;
        using var shapeBuilder = new SKPathBuilder();
        shapeBuilder.MoveTo(0.5f, 0.08f);
        shapeBuilder.CubicTo(0.25f, -0.06f, -0.06f, 0.12f, 0.025f, 0.47f);
        shapeBuilder.CubicTo(0.06f, 0.73f, 0.17f, 1.04f, 0.38f, 0.98f);
        shapeBuilder.CubicTo(0.47f, 0.95f, 0.51f, 1.02f, 0.63f, 0.985f);
        shapeBuilder.CubicTo(0.85f + asymmetry, 1.01f, 0.99f, 0.66f, 0.985f, 0.38f);
        shapeBuilder.CubicTo(1.01f, 0.08f, 0.75f, -0.065f, 0.5f, 0.08f);
        shapeBuilder.Close();
        using var shape = shapeBuilder.Detach();
        var baseColor = apple.Color switch
        {
            AppleColor.Green => new SKColor(112, 153, 42),
            AppleColor.Yellow => new SKColor(218, 168, 40),
            _ => new SKColor(180, 38, 37),
        };
        using var body = SKShader.CreateRadialGradient(
            new SKPoint(0.5f + (scene.LightDirection.X * 0.32f), 0.5f + (scene.LightDirection.Y * 0.36f)), 0.95f,
            [Lighten(baseColor), baseColor, new SKColor(53, 25, 20)],
            [0f, 0.48f, 1f], SKShaderTileMode.Clamp);
        paint.Shader = body;
        canvas.DrawPath(shape, paint);
        paint.Shader = null;
        canvas.Save();
        canvas.ClipPath(shape);
        var random = Pcg32.ForStream(scene.Seed, "apple.surface");
        for (var index = 0; index < 1100; index++)
        {
            var x = (float)random.NextDouble();
            var y = (float)random.NextDouble();
            paint.Color = new SKColor(255, 220, 144, (byte)(10 + (apple.SurfaceVariation * 110)));
            canvas.DrawOval(x, y, 0.001f + ((float)random.NextDouble() * 0.0015f), 0.002f, paint);
        }

        using var highlight = SKShader.CreateRadialGradient(
            new SKPoint(0.5f + (scene.LightDirection.X * 0.31f), 0.5f + (scene.LightDirection.Y * 0.38f)), 0.22f,
            [new SKColor(255, 245, 215, 125), new SKColor(255, 245, 215, 0)], null, SKShaderTileMode.Clamp);
        paint.Shader = highlight;
        canvas.DrawRect(0, 0, 1, 1, paint);
        paint.Shader = null;
        canvas.Restore();
        paint.Color = new SKColor(61, 34, 19, 160);
        canvas.DrawOval(0.5f, 0.08f, 0.095f, 0.022f, paint);
        paint.Color = new SKColor(84, 55, 29);
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 0.027f;
        paint.StrokeCap = SKStrokeCap.Round;
        var stemX = 0.5f + ((float)apple.StemLean * 0.2f);
        using var stemBuilder = new SKPathBuilder();
        stemBuilder.MoveTo(0.5f, 0.08f);
        stemBuilder.CubicTo(0.49f, 0.025f, stemX, -0.035f, stemX, -0.12f);
        using var stem = stemBuilder.Detach();
        canvas.DrawPath(stem, paint);
        paint.Style = SKPaintStyle.Fill;
        canvas.Save();
        canvas.Translate(stemX, -0.075f);
        canvas.RotateDegrees((float)(apple.LeafAngle * 180 / Math.PI));
        using var leafBuilder = new SKPathBuilder();
        leafBuilder.MoveTo(0, 0);
        leafBuilder.CubicTo(0.1f, -0.13f, 0.24f, -0.1f, 0.36f, 0.01f);
        leafBuilder.CubicTo(0.22f, 0.10f, 0.07f, 0.10f, 0, 0);
        leafBuilder.Close();
        using var leaf = leafBuilder.Detach();
        using var leafShader = SKShader.CreateLinearGradient(
            new SKPoint(0, -0.08f), new SKPoint(0, 0.08f),
            [new SKColor(112, 151, 58), new SKColor(29, 74, 34)], null, SKShaderTileMode.Clamp);
        paint.Shader = leafShader;
        canvas.DrawPath(leaf, paint);
        paint.Shader = null;
        paint.Color = new SKColor(167, 181, 91, 180);
        paint.StrokeWidth = 0.004f;
        canvas.DrawLine(0, 0, 0.33f, 0.01f, paint);
        for (var index = 1; index < 5; index++)
        {
            var x = index * 0.06f;
            canvas.DrawLine(x, 0, x + 0.035f, -0.045f, paint);
            canvas.DrawLine(x, 0, x + 0.025f, 0.04f, paint);
        }

        canvas.Restore();
        canvas.Restore();
    }

    private static SKColor Lighten(SKColor color) => new(
        (byte)Math.Min(255, color.Red + 58),
        (byte)Math.Min(255, color.Green + 60),
        (byte)Math.Min(255, color.Blue + 35));
}
