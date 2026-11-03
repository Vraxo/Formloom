using Formloom.Core;
using Formloom.Core.Randomness;
using SkiaSharp;

namespace Formloom.Rendering.Skia;

public static class StillLifeRenderer
{
    public static RenderedImage Render(StillLifeScene scene) => RenderCore(scene, silhouette: false);

    public static RenderedImage RenderSilhouette(StillLifeScene scene) => RenderCore(scene, silhouette: true);

    private static RenderedImage RenderCore(StillLifeScene scene, bool silhouette)
    {
        ArgumentNullException.ThrowIfNull(scene);
        if (scene.Width is < 64 or > 4096 || scene.Height is < 64 or > 4096)
        {
            throw new ArgumentOutOfRangeException(nameof(scene));
        }

        if (!double.IsFinite(scene.SurfaceDetail) || scene.SurfaceDetail is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(scene));
        }

        var info = new SKImageInfo(scene.Width, scene.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info) ?? throw new InvalidOperationException("Could not allocate raster surface.");
        var canvas = surface.Canvas;
        canvas.Scale(scene.Width / 1024f, scene.Height / 1024f);
        canvas.Clear(SKColors.Transparent);
        if (!silhouette)
        {
            DrawSetting(canvas, scene);
        }

        DrawFruit(canvas, scene, silhouette);
        using var image = surface.Snapshot();
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return new(scene.Width, scene.Height, encoded.ToArray(), scene.Subject.ToString().ToLowerInvariant());
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

    private static void DrawFruit(SKCanvas canvas, StillLifeScene scene, bool silhouette)
    {
        var apple = scene.Body;
        var centerX = apple.Center.X * 1024;
        var top = (apple.Center.Y - (apple.Size.Y / 2)) * 1024;
        var width = apple.Size.X * 1024;
        var height = apple.Size.Y * 1024;
        var left = centerX - (width / 2);
        var bottom = top + height;
        using var paint = new SKPaint { IsAntialias = true };
        if (!silhouette)
        {
            using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 14);
            paint.MaskFilter = blur;
            paint.Color = new SKColor(12, 20, 17, 80);
            canvas.DrawOval(centerX - (scene.LightDirection.X * 65), bottom + 3, width * 0.55f, 24, paint);
            paint.MaskFilter = null;
            DrawContactShadow(canvas, centerX - (width * 0.13f), bottom - (height * 0.009f), width * 0.22f, height * 0.035f);
            DrawContactShadow(canvas, centerX + (width * 0.13f), bottom - (height * 0.005f), width * 0.23f, height * 0.035f);
        }

        canvas.Save();
        canvas.Translate(left, top);
        canvas.Scale(width, height);
        var contour = OrganicForms.Create(scene.Subject, apple.Asymmetry, scene.Seed, scene.Variation);
        using var shape = CreatePath(contour);
        if (silhouette)
        {
            paint.Color = SKColors.Black;
            canvas.DrawPath(shape, paint);
        }
        else
        {
            var material = FruitMaterial.For(scene);
            var baseColor = material.BaseColor;
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
            DrawSurface(canvas, scene, material, paint);

            using var highlight = SKShader.CreateRadialGradient(
                new SKPoint(0.5f + (scene.LightDirection.X * 0.31f), 0.5f + (scene.LightDirection.Y * 0.38f)), material.HighlightRadius,
                [new SKColor(255, 245, 215, material.HighlightAlpha), new SKColor(255, 245, 215, 0)], null, SKShaderTileMode.Clamp);
            paint.Color = SKColors.White;
            paint.Shader = highlight;
            canvas.DrawRect(0, 0, 1, 1, paint);
            paint.Shader = null;
            canvas.Restore();
        }

        if (scene.Subject == FruitKind.Orange)
        {
            DrawCalyx(canvas, contour.TopAttachment, paint, silhouette);
            canvas.Restore();
            return;
        }

        var stemX = 0.5f + ((float)apple.StemLean * 0.2f);
        DrawStemAttachment(canvas, shape, stemX, contour.TopAttachment, silhouette);
        canvas.Save();
        canvas.Translate(stemX + contour.TopAttachment.X - 0.5f, contour.TopAttachment.Y - 0.155f);
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
        paint.Color = silhouette ? SKColors.Black : SKColors.White;
        paint.Shader = silhouette ? null : leafShader;
        canvas.DrawPath(leaf, paint);
        if (silhouette)
        {
            canvas.Restore();
            canvas.Restore();
            return;
        }

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

    private static void DrawStemAttachment(SKCanvas canvas, SKPath body, float stemX, System.Numerics.Vector2 attachment, bool silhouette)
    {
        using var paint = new SKPaint { IsAntialias = true, Color = SKColors.White };
        if (!silhouette)
        {
            canvas.Save();
            canvas.ClipPath(body);
            canvas.Translate(attachment.X, attachment.Y);
            canvas.Scale(0.115f, 0.055f);
            using var indentation = SKShader.CreateRadialGradient(
                SKPoint.Empty, 1,
                [new SKColor(48, 23, 14, 195), new SKColor(69, 26, 17, 95), new SKColor(69, 26, 17, 0)],
                [0f, 0.35f, 1f], SKShaderTileMode.Clamp);
            paint.Shader = indentation;
            canvas.DrawOval(0, 0, 1, 1, paint);
            canvas.Restore();
        }

        canvas.Save();
        canvas.Translate(attachment.X - 0.5f, attachment.Y - 0.08f);
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
        paint.Color = silhouette ? SKColors.Black : SKColors.White;
        paint.Shader = silhouette ? null : bark;
        canvas.DrawPath(stem, paint);
        if (silhouette)
        {
            canvas.Restore();
            return;
        }

        paint.Shader = null;
        paint.Color = new SKColor(170, 129, 72, 150);
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 0.003f;
        using var ridgeBuilder = new SKPathBuilder();
        ridgeBuilder.MoveTo(0.494f, 0.06f);
        ridgeBuilder.CubicTo(0.485f, 0.02f, stemX - 0.006f, -0.04f, stemX - 0.004f, -0.105f);
        using var ridge = ridgeBuilder.Detach();
        canvas.DrawPath(ridge, paint);
        canvas.Restore();
    }

    private static SKPath CreatePath(OrganicContour contour)
    {
        using var builder = new SKPathBuilder();
        var start = contour.Segments[0].Start;
        builder.MoveTo(start.X, start.Y);
        foreach (var segment in contour.Segments)
        {
            builder.CubicTo(segment.Control1.X, segment.Control1.Y, segment.Control2.X, segment.Control2.Y, segment.End.X, segment.End.Y);
        }

        builder.Close();
        return builder.Detach();
    }

    private static void DrawSurface(SKCanvas canvas, StillLifeScene scene, FruitMaterial material, SKPaint paint)
    {
        var stream = scene.Subject.ToString().ToLowerInvariant() + ".surface";
        var random = Pcg32.ForStream(scene.Seed, stream);
        for (var index = 0; index < material.DetailCount; index++)
        {
            var x = (float)random.NextDouble();
            var y = (float)random.NextDouble();
            var radius = 0.001f + ((float)random.NextDouble() * 0.0015f);
            if (scene.Subject == FruitKind.Orange)
            {
                var light = Math.Clamp(1 - x, 0.2f, 1);
                paint.Color = new SKColor(109, 55, 16, (byte)(scene.SurfaceDetail * 22));
                canvas.DrawOval(x, y, radius, radius * 0.75f, paint);
                paint.Color = new SKColor(255, 211, 121, (byte)(scene.SurfaceDetail * light * 28));
                canvas.DrawOval(x - (radius * 0.4f), y - (radius * 0.5f), radius * 0.65f, radius * 0.4f, paint);
            }
            else
            {
                var alpha = scene.Subject == FruitKind.Apple ? 10 + (scene.Body.SurfaceVariation * 110) : 16;
                paint.Color = new SKColor(255, 220, 144, (byte)(alpha * scene.SurfaceDetail));
                canvas.DrawOval(x, y, radius, 0.002f, paint);
            }
        }
    }

    private static void DrawCalyx(SKCanvas canvas, System.Numerics.Vector2 attachment, SKPaint paint, bool silhouette)
    {
        paint.Color = silhouette ? SKColors.Black : new SKColor(92, 91, 35);
        canvas.DrawOval(attachment.X, attachment.Y + 0.012f, 0.035f, 0.012f, paint);
        paint.Color = silhouette ? SKColors.Black : new SKColor(154, 134, 58);
        paint.StrokeWidth = 0.004f;
        for (var index = 0; index < 5; index++)
        {
            var angle = index * MathF.Tau / 5;
            canvas.DrawLine(
                attachment.X, attachment.Y + 0.012f,
                attachment.X + (MathF.Cos(angle) * 0.03f), attachment.Y + 0.012f + (MathF.Sin(angle) * 0.008f), paint);
        }
    }

    private static SKColor Lighten(SKColor color) => new(
        (byte)Math.Min(255, color.Red + 58),
        (byte)Math.Min(255, color.Green + 60),
        (byte)Math.Min(255, color.Blue + 35));
}
