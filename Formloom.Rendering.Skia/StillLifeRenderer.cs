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
        WoodGrain.Draw(canvas, scene, horizon);
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
        using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 14);
        paint.MaskFilter = blur;
        paint.Color = new SKColor(12, 20, 17, 80);
        canvas.DrawOval(centerX - (scene.LightDirection.X * 65), bottom + 3, width * 0.55f, 24, paint);
        paint.MaskFilter = null;
        DrawContactShadow(canvas, centerX - (width * 0.13f), bottom - (height * 0.009f), width * 0.22f, height * 0.035f);
        DrawContactShadow(canvas, centerX + (width * 0.13f), bottom - (height * 0.005f), width * 0.23f, height * 0.035f);
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

        // Shader colors are multiplied by paint alpha; do not inherit shadow opacity.
        paint.Color = SKColors.White;
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
        paint.Color = SKColors.White;
        paint.Shader = highlight;
        canvas.DrawRect(0, 0, 1, 1, paint);
        paint.Shader = null;
        canvas.Restore();
        var stemX = 0.5f + ((float)apple.StemLean * 0.2f);
        DrawStemAttachment(canvas, shape, stemX);
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

    private static void DrawContactShadow(SKCanvas canvas, float x, float y, float radiusX, float radiusY)
    {
        canvas.Save();
        canvas.Translate(x, y);
        canvas.Scale(radiusX, radiusY);
        using var gradient = SKShader.CreateRadialGradient(
            SKPoint.Empty, 1,
            [new SKColor(20, 15, 10, 205), new SKColor(20, 15, 10, 85), new SKColor(20, 15, 10, 0)],
            [0f, 0.38f, 1f], SKShaderTileMode.Clamp);
        using var paint = new SKPaint { IsAntialias = true, Color = SKColors.White, Shader = gradient };
        canvas.DrawOval(0, 0, 1, 1, paint);
        canvas.Restore();
    }

    private static void DrawStemAttachment(SKCanvas canvas, SKPath body, float stemX)
    {
        using var paint = new SKPaint { IsAntialias = true, Color = SKColors.White };
        canvas.Save();
        canvas.ClipPath(body);
        canvas.Translate(0.5f, 0.08f);
        canvas.Scale(0.115f, 0.055f);
        using var indentation = SKShader.CreateRadialGradient(
            SKPoint.Empty, 1,
            [new SKColor(48, 23, 14, 195), new SKColor(69, 26, 17, 95), new SKColor(69, 26, 17, 0)],
            [0f, 0.35f, 1f], SKShaderTileMode.Clamp);
        paint.Shader = indentation;
        canvas.DrawOval(0, 0, 1, 1, paint);
        canvas.Restore();
        using var stemBuilder = new SKPathBuilder();
        stemBuilder.MoveTo(0.485f, 0.08f);
        stemBuilder.CubicTo(0.475f, 0.025f, stemX - 0.012f, -0.04f, stemX - 0.009f, -0.12f);
        stemBuilder.QuadTo(stemX, -0.129f, stemX + 0.009f, -0.12f);
        stemBuilder.CubicTo(stemX + 0.008f, -0.035f, 0.509f, 0.025f, 0.515f, 0.08f);
        stemBuilder.QuadTo(0.5f, 0.098f, 0.485f, 0.08f);
        stemBuilder.Close();
        using var stem = stemBuilder.Detach();
        using var bark = SKShader.CreateLinearGradient(
            new SKPoint(stemX - 0.018f, 0), new SKPoint(stemX + 0.025f, 0),
            [new SKColor(133, 94, 49), new SKColor(72, 45, 24), new SKColor(45, 31, 20)],
            null, SKShaderTileMode.Clamp);
        paint.Shader = bark;
        canvas.DrawPath(stem, paint);
        paint.Shader = null;
        paint.Color = new SKColor(170, 129, 72, 150);
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 0.003f;
        using var ridgeBuilder = new SKPathBuilder();
        ridgeBuilder.MoveTo(0.494f, 0.06f);
        ridgeBuilder.CubicTo(0.485f, 0.02f, stemX - 0.006f, -0.04f, stemX - 0.004f, -0.105f);
        using var ridge = ridgeBuilder.Detach();
        canvas.DrawPath(ridge, paint);
    }

    private static SKColor Lighten(SKColor color) => new(
        (byte)Math.Min(255, color.Red + 58),
        (byte)Math.Min(255, color.Green + 60),
        (byte)Math.Min(255, color.Blue + 35));
}
