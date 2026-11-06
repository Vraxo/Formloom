using System.Collections.Immutable;
using Formloom.Core;
using Formloom.Core.Randomness;
using SkiaSharp;

namespace Formloom.Rendering.Skia;

public static class StillLifeRenderer
{
    public static RenderedImage Render(StillLifeScene scene) => RenderCore(scene, silhouette: false);

    public static RenderedImage RenderSilhouette(StillLifeScene scene) => RenderCore(scene, silhouette: true);

    internal static RenderedImage RenderShadowMask(StillLifeScene scene) => RenderCore(scene, silhouette: false, shadowMask: true);

    internal static RenderedImage RenderWithoutInstanceDetail(StillLifeScene scene, int depth) =>
        RenderCore(scene, silhouette: false, omittedDetailDepth: depth);

    private static RenderedImage RenderCore(StillLifeScene scene, bool silhouette, bool shadowMask = false, int? omittedDetailDepth = null)
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

        var error = SceneValidator.Validate(scene);
        if (error is not null)
        {
            throw new ArgumentException(error, nameof(scene));
        }

        var info = new SKImageInfo(scene.Width, scene.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info) ?? throw new InvalidOperationException("Could not allocate raster surface.");
        var canvas = surface.Canvas;
        canvas.Scale(scene.Width / 1024f, scene.Height / 1024f);
        canvas.Clear(SKColors.Transparent);
        if (!silhouette && !shadowMask)
        {
            DrawSetting(canvas, scene);
        }

        var ordered = scene.Objects.OrderBy(entry => entry.Depth).ToArray();
        if (scene.Containment is { } containment)
        {
            DrawContainment(canvas, scene, containment, silhouette, shadowMask, omittedDetailDepth);
        }
        else if (shadowMask)
        {
            foreach (var entry in ordered)
            {
                DrawObject(canvas, scene, entry, silhouette: false, shadowsOnly: ordered.Length > 1, skipBody: true);
            }
        }
        else
        {
            if (ordered.Length > 1 && !silhouette)
            {
                foreach (var entry in ordered)
                {
                    DrawObject(canvas, scene, entry, silhouette: false, shadowsOnly: true, omittedDetailDepth: omittedDetailDepth);
                }
            }

            foreach (var entry in ordered)
            {
                DrawObject(canvas, scene, entry, silhouette, skipShadows: ordered.Length > 1, omittedDetailDepth: omittedDetailDepth);
            }
        }

        using var image = surface.Snapshot();
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return new(scene.Width, scene.Height, encoded.ToArray(), scene.SubjectToken);
    }

    private static void DrawObject(
        SKCanvas canvas,
        StillLifeScene scene,
        SceneEntry entry,
        bool silhouette,
        bool skipShadows = false,
        bool shadowsOnly = false,
        bool skipBody = false,
        int? omittedDetailDepth = null)
    {
        if (entry.Fruit is { } fruit)
        {
            DrawFruit(canvas, ForInstance(scene, fruit, omittedDetailDepth), silhouette, skipShadows, shadowsOnly, skipBody);
            return;
        }

        var ceramic = entry.Ceramic!;
        var beverage = scene.CupContents is { } contents && contents.Cup == ceramic
            ? contents.Beverage
            : (BeverageKind?)null;
        var flower = scene.VaseFlower is { } planted && planted.Vase == ceramic && !shadowsOnly && !skipBody
            ? planted
            : null;
        if (flower is not null)
        {
            DrawVaseFlower(canvas, scene, flower, silhouette, entry.Depth == omittedDetailDepth);
        }

        DrawCeramic(canvas, scene, ceramic, silhouette, skipShadows, shadowsOnly, skipBody, entry.Depth == omittedDetailDepth, beverage, flower);
    }

    private static StillLifeScene ForInstance(StillLifeScene scene, FruitInstance fruit, int? omittedDetailDepth = null) => scene with
    {
        Apple = fruit.Body,
        Subject = fruit.Subject,
        Seed = fruit.Seed,
        Instances = null,
        SurfaceDetail = fruit.Depth == omittedDetailDepth ? 0 : scene.SurfaceDetail,
    };

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

    private static void DrawFruit(SKCanvas canvas, StillLifeScene scene, bool silhouette, bool skipShadows = false, bool shadowsOnly = false, bool skipBody = false)
    {
        var apple = scene.Body;
        var centerX = apple.Center.X * 1024;
        var top = (apple.Center.Y - (apple.Size.Y / 2)) * 1024;
        var width = apple.Size.X * 1024;
        var height = apple.Size.Y * 1024;
        var left = centerX - (width / 2);
        var bottom = top + height;
        using var paint = new SKPaint { IsAntialias = true };
        if (!silhouette && !skipShadows)
        {
            var shadowScale = shadowsOnly ? width / (0.43f * 1024) : 1;
            using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 14 * shadowScale);
            paint.MaskFilter = blur;
            paint.Color = new SKColor(12, 20, 17, 80);
            canvas.DrawOval(centerX - (scene.LightDirection.X * 65 * shadowScale), bottom + (3 * shadowScale), width * (shadowsOnly ? 0.5f : 0.55f), 24 * shadowScale, paint);
            paint.MaskFilter = null;
            DrawContactShadow(canvas, centerX - (width * 0.13f), bottom - (height * 0.009f), width * 0.22f, height * 0.035f);
            DrawContactShadow(canvas, centerX + (width * 0.13f), bottom - (height * 0.005f), width * 0.23f, height * 0.035f);
        }

        if (shadowsOnly || skipBody)
        {
            return;
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

    private static void DrawCeramic(
        SKCanvas canvas,
        StillLifeScene scene,
        CeramicInstance ceramic,
        bool silhouette,
        bool skipShadows,
        bool shadowsOnly,
        bool skipBody,
        bool omitDetail,
        BeverageKind? beverage = null,
        VaseFlower? flower = null)
    {
        var profile = CeramicForms.For(ceramic.Kind);
        var body = ceramic.Body;
        var centerX = body.Center.X * 1024;
        var top = (body.Center.Y - (body.Size.Y / 2)) * 1024;
        var width = body.Size.X * 1024;
        var height = body.Size.Y * 1024;
        var left = centerX - (width / 2);
        var contactY = top + (profile.BaseBottom * height);
        using var paint = new SKPaint { IsAntialias = true };
        if (!silhouette && !skipShadows)
        {
            var shadowScale = shadowsOnly ? width / (0.43f * 1024) : 1;
            using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 14 * shadowScale);
            paint.MaskFilter = blur;
            paint.Color = new SKColor(12, 20, 17, 80);
            canvas.DrawOval(centerX - (scene.LightDirection.X * 65 * shadowScale), contactY + (3 * shadowScale), width * (shadowsOnly ? 0.5f : 0.55f), 24 * shadowScale, paint);
            paint.MaskFilter = null;
            var baseHalf = width * profile.BaseWidth * 0.28f;
            DrawContactShadow(canvas, centerX - baseHalf, contactY - (height * 0.006f), width * 0.17f, height * 0.03f);
            DrawContactShadow(canvas, centerX + baseHalf, contactY - (height * 0.003f), width * 0.18f, height * 0.03f);
        }

        if (shadowsOnly || skipBody)
        {
            return;
        }

        canvas.Save();
        canvas.Translate(left, top);
        canvas.Scale(width, height);
        var material = CeramicMaterial.For(ceramic, scene.Variation, omitDetail ? 0 : scene.SurfaceDetail);
        if (profile.Handle is { } handle)
        {
            using var ring = CreateRing(handle);
            if (silhouette)
            {
                paint.Color = SKColors.Black;
                paint.Shader = null;
            }
            else
            {
                using var ringShader = SKShader.CreateLinearGradient(
                    new SKPoint(handle.Center.X - handle.OuterRadius.X, handle.Center.Y - handle.OuterRadius.Y),
                    new SKPoint(handle.Center.X + handle.OuterRadius.X, handle.Center.Y + handle.OuterRadius.Y),
                    [material.LipColor, material.BaseColor, material.EdgeColor], null, SKShaderTileMode.Clamp);
                paint.Color = SKColors.White;
                paint.Shader = ringShader;
            }

            canvas.DrawPath(ring, paint);
            paint.Shader = null;
        }

        using var outline = CreatePath(profile.Outline);
        if (silhouette)
        {
            paint.Color = SKColors.Black;
            canvas.DrawPath(outline, paint);
            canvas.Restore();
            return;
        }

        using var glaze = SKShader.CreateRadialGradient(
            new SKPoint(0.5f + (scene.LightDirection.X * 0.30f), 0.5f + (scene.LightDirection.Y * 0.34f)), 1.05f,
            [Lighten(material.BaseColor), material.BaseColor, material.EdgeColor],
            [0f, 0.46f, 1f], SKShaderTileMode.Clamp);
        paint.Color = SKColors.White;
        paint.Shader = glaze;
        canvas.DrawPath(outline, paint);
        paint.Shader = null;
        canvas.Save();
        canvas.ClipPath(outline);
        DrawGlazeMottle(canvas, ceramic, material);
        var accents = !omitDetail && scene.SurfaceDetail > 0;
        DrawMouth(canvas, scene, profile, material, paint, beverage, accents, flower, new(centerX, top), new(width, height));

        // The cup's finish sheen normally washes over the mouth as well. It is held off the
        // drink so the glaze can never recolour the beverage: the lip and body keep it, the
        // liquid surface does not.
        canvas.Save();
        if (beverage is not null)
        {
            var surface = CupLiquidSurface.For(profile);
            using var drink = CreateEllipsePath(surface.ClipCenter, surface.ClipRadius);
            canvas.ClipPath(drink, SKClipOperation.Difference, antialias: true);
        }

        using var highlight = SKShader.CreateRadialGradient(
            new SKPoint(0.5f + (scene.LightDirection.X * 0.30f), 0.5f + (scene.LightDirection.Y * 0.36f)), material.HighlightRadius,
            [new SKColor(255, 250, 235, material.HighlightAlpha), new SKColor(255, 250, 235, 0)], null, SKShaderTileMode.Clamp);
        paint.Color = SKColors.White;
        paint.Shader = highlight;
        canvas.DrawRect(0, 0, 1, 1, paint);
        paint.Shader = null;
        canvas.Restore();
        canvas.Restore();
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 0.006f;
        paint.Color = material.EdgeColor.WithAlpha(140);
        canvas.DrawPath(outline, paint);
        paint.Style = SKPaintStyle.Fill;
        canvas.Restore();
    }

    // A contained scene draws the bowl whole and then clips the fruit away wherever the
    // bowl's near wall and lip sit in front of it. Clipping rather than repainting keeps
    // every bowl pixel identical to a standalone bowl, so the only changed pixels are the
    // content itself and its cavity contact shadow.
    private static void DrawContainment(
        SKCanvas canvas,
        StillLifeScene scene,
        BowlContainment containment,
        bool silhouette,
        bool shadowMask,
        int? omittedDetailDepth)
    {
        var bowl = containment.Bowl;
        var content = containment.Content;
        if (shadowMask)
        {
            // Only the bowl touches the table; contained fruit casts no tabletop shadow.
            DrawCeramic(canvas, scene, bowl, silhouette: false, skipShadows: false, shadowsOnly: false, skipBody: true, omitDetail: false);
            return;
        }

        DrawCeramic(
            canvas,
            scene,
            bowl,
            silhouette,
            skipShadows: false,
            shadowsOnly: false,
            skipBody: false,
            omitDetail: bowl.Depth == omittedDetailDepth);
        if (!silhouette)
        {
            DrawCavityContactShadow(canvas, containment);
        }

        canvas.Save();
        using (var near = CreateCanvasPath(containment, BowlGeometry.FrontWall(containment.Profile)))
        {
            canvas.ClipPath(near, SKClipOperation.Difference, antialias: true);
        }

        DrawFruit(
            canvas,
            ForInstance(scene, content, content.Depth == omittedDetailDepth ? content.Depth : null),
            silhouette,
            skipShadows: true);
        canvas.Restore();
    }

    // Maps normalized bowl-local segments onto the canvas. The near-wall band (the
    // opening's lower arc plus the bowl's front chain) is everything in front of the
    // contained fruit; the opening itself clips the cavity contact shadow.
    private static SKPath CreateCanvasPath(BowlContainment containment, ImmutableArray<CubicSegment> segments)
    {
        var body = containment.Bowl.Body;
        var width = body.Size.X * 1024;
        var height = body.Size.Y * 1024;
        var left = (body.Center.X * 1024) - (width / 2);
        var top = (body.Center.Y - (body.Size.Y / 2)) * 1024;
        using var builder = new SKPathBuilder();
        var start = segments[0].Start;
        builder.MoveTo(left + (start.X * width), top + (start.Y * height));
        foreach (var segment in segments)
        {
            builder.CubicTo(
                left + (segment.Control1.X * width), top + (segment.Control1.Y * height),
                left + (segment.Control2.X * width), top + (segment.Control2.Y * height),
                left + (segment.End.X * width), top + (segment.End.Y * height));
        }

        builder.Close();
        return builder.Detach();
    }

    // Subtle contact shadow clipped to the cavity, hugging the inside of the near rim under
    // the fruit. It is painted before the content, so the fruit covers its middle and only
    // the pool around the base stays visible. It never leaves the opening.
    private static void DrawCavityContactShadow(SKCanvas canvas, BowlContainment containment)
    {
        var body = containment.Bowl.Body;
        var profile = containment.Profile;
        var width = body.Size.X * 1024;
        var height = body.Size.Y * 1024;
        var left = (body.Center.X * 1024) - (width / 2);
        var top = (body.Center.Y - (body.Size.Y / 2)) * 1024;
        var centerX = left + (profile.MouthCenter.X * width);
        var centerY = top + (profile.MouthCenter.Y * height);
        var radiusY = profile.MouthRadius.Y * height;
        var frontRim = centerY + radiusY;
        var contentWidth = containment.Content.Body.Size.X * 1024;
        canvas.Save();
        using (var opening = CreateCanvasPath(containment, BowlGeometry.Opening(profile)))
        {
            canvas.ClipPath(opening);
        }

        // The content is wider than the visible cavity sliver on either side, so the shadow
        // is a pool that spreads out from under it into both side lobes of the opening.
        DrawCavityPool(canvas, centerX, frontRim - (radiusY * 0.30f), contentWidth * 1.05f, radiusY * 0.80f, 86);
        DrawCavityPool(canvas, centerX, frontRim - (radiusY * 0.10f), contentWidth * 0.75f, radiusY * 0.45f, 120);
        canvas.Restore();
    }

    private static void DrawCavityPool(SKCanvas canvas, float x, float y, float radiusX, float radiusY, byte alpha)
    {
        canvas.Save();
        canvas.Translate(x, y);
        canvas.Scale(radiusX, radiusY);
        using var gradient = SKShader.CreateRadialGradient(
            SKPoint.Empty, 1,
            [new SKColor(8, 8, 10, alpha), new SKColor(8, 8, 10, (byte)(alpha * 0.8f)), new SKColor(8, 8, 10, 0)],
            [0f, 0.62f, 1f], SKShaderTileMode.Clamp);
        using var paint = new SKPaint { IsAntialias = true, Color = SKColors.White, Shader = gradient };
        canvas.DrawOval(0, 0, 1, 1, paint);
        canvas.Restore();
    }

    // A still surface inside the cup mouth: a shaded body, a soft meniscus at its own edge
    // and, when accents are enabled, two broad reflections. Everything is clipped to the
    // inner opening, so the lip and rim strokes stay in front of it and nothing can reach the
    // exterior, the handle or the table.
    private static void DrawCupLiquid(SKCanvas canvas, CeramicProfile profile, BeverageKind beverage, bool accents, SKPaint paint)
    {
        var surface = CupLiquidSurface.For(profile);
        var material = BeverageMaterial.For(beverage);
        canvas.Save();
        using (var opening = CreateEllipsePath(surface.ClipCenter, surface.ClipRadius))
        {
            canvas.ClipPath(opening, antialias: true);
        }

        using (var body = SKShader.CreateRadialGradient(
            new SKPoint(
                surface.Center.X - (surface.Radius.X * 0.22f),
                surface.Center.Y - (surface.Radius.Y * 0.34f)),
            surface.Radius.X * 0.92f,
            [material.Light, material.Base, material.Edge],
            [0f, 0.52f, 1f],
            SKShaderTileMode.Clamp))
        {
            paint.Color = SKColors.White;
            paint.Shader = body;
            canvas.DrawOval(surface.Center.X, surface.Center.Y, surface.Radius.X, surface.Radius.Y, paint);
            paint.Shader = null;
        }

        // Meniscus: a thin darkening that hugs the drink's own boundary. The clip removes the
        // part that would fall on the ceramic, so only the visible edge is affected.
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = profile.RimThickness * 0.7f;
        paint.Color = material.Edge.WithAlpha(150);
        canvas.DrawOval(surface.Center.X, surface.Center.Y, surface.Radius.X, surface.Radius.Y, paint);
        paint.Style = SKPaintStyle.Fill;
        paint.Color = SKColors.White;

        if (accents)
        {
            DrawLiquidSheen(canvas, surface, material, 0.30f, 0.30f, 0.42f, material.SheenAlpha, paint);
            DrawLiquidSheen(canvas, surface, material, 0.58f, 0.62f, 0.22f, material.GlintAlpha, paint);
        }

        canvas.Restore();
    }

    // Broad, soft reflection: a scaled radial falloff inside the surface, never a hard shape.
    private static void DrawLiquidSheen(
        SKCanvas canvas,
        CupLiquidSurface surface,
        BeverageMaterial material,
        float offsetX,
        float offsetY,
        float radius,
        byte alpha,
        SKPaint paint)
    {
        var radiusX = surface.Radius.X * radius;
        var radiusY = surface.Radius.Y * radius * 0.9f;
        canvas.Save();
        canvas.Translate(surface.Center.X - (surface.Radius.X * offsetX), surface.Center.Y - (surface.Radius.Y * offsetY));
        canvas.Scale(radiusX, radiusY);
        using var shader = SKShader.CreateRadialGradient(
            SKPoint.Empty,
            1,
            [material.Sheen.WithAlpha(alpha), material.Sheen.WithAlpha((byte)(alpha * 0.45f)), material.Sheen.WithAlpha(0)],
            [0f, 0.55f, 1f],
            SKShaderTileMode.Clamp);
        paint.Color = SKColors.White;
        paint.Shader = shader;
        canvas.DrawOval(0, 0, 1, 1, paint);
        paint.Shader = null;
        canvas.Restore();
    }

    // A tulip in a vase. The flower is drawn before the vase and clipped to the half-plane
    // above the mouth's top edge, so nothing can reach the exterior wall or the table. The
    // mouth's paint then picks the stem up inside the opening (see DrawStemInsideOpening),
    // which is the only place the planted vase differs from the empty one below the rim.
    private static void DrawVaseFlower(
        SKCanvas canvas,
        StillLifeScene scene,
        VaseFlower flower,
        bool silhouette,
        bool omitDetail)
    {
        var plant = flower.Plant;
        var material = TulipMaterial.For(flower.Color);
        canvas.Save();
        canvas.Scale(1024, 1024);
        using (var clip = CreateFlowerClip(flower))
        {
            canvas.ClipPath(clip, antialias: true);
        }

        using var paint = new SKPaint { IsAntialias = true, Color = SKColors.White };
        DrawTulipStem(canvas, plant, material, silhouette, paint);
        foreach (var leaf in plant.Leaves)
        {
            DrawTulipLeaf(canvas, leaf, material, silhouette, omitDetail, paint);
        }

        foreach (var petal in plant.Bloom.Petals)
        {
            DrawTulipPetal(canvas, scene, petal, material, silhouette, omitDetail, paint);
        }

        canvas.Restore();
    }

    // The stem's visible continuation inside the mouth. The flower pass stops at the mouth's
    // top tangent, and this pass picks the stem up inside the opening: clipped to the opening
    // ellipse, darkened so it reads as recessed, and painted under the rim strokes so the near
    // lip covers where it ends.
    private static void DrawStemInsideOpening(
        SKCanvas canvas,
        VaseFlower flower,
        SKPaint paint,
        System.Numerics.Vector2 origin,
        System.Numerics.Vector2 size)
    {
        canvas.Save();
        // The mouth is drawn in vase-local 0..1 space; the flower path is in canvas 0..1 space,
        // so this maps canvas-normalized points onto the vase's own axes.
        var toCanvasSpace = SKMatrix.CreateScaleTranslation(
            1024f / size.X,
            1024f / size.Y,
            -origin.X / size.X,
            -origin.Y / size.Y);
        canvas.Concat(in toCanvasSpace);
        using (var opening = CreateEllipsePath(flower.OpeningCenter, flower.OpeningRadius))
        {
            canvas.ClipPath(opening, antialias: true);
        }

        float[] recess = [0.74f, 0, 0, 0, 0, 0, 0.74f, 0, 0, 0, 0, 0, 0.74f, 0, 0, 0, 0, 0, 1, 0];
        paint.ColorFilter = SKColorFilter.CreateColorMatrix(recess);
        DrawTulipStem(canvas, flower.Plant, TulipMaterial.For(flower.Color), silhouette: false, paint: paint);
        paint.ColorFilter = null;
        canvas.Restore();
    }

    // The only hard edge the flower has is the mouth's own top tangent, taken from the vase
    // profile: a profile-aware half-plane, not a screen-space rectangle around the bloom.
    private static SKPath CreateFlowerClip(VaseFlower flower)
    {
        var top = flower.MouthTop;
        using var builder = new SKPathBuilder();
        builder.MoveTo(0, top);
        builder.LineTo(1, top);
        builder.LineTo(1, -0.05f);
        builder.LineTo(0, -0.05f);
        builder.Close();
        return builder.Detach();
    }

    private static void DrawTulipStem(SKCanvas canvas, TulipPlant plant, TulipMaterial material, bool silhouette, SKPaint paint)
    {
        using var stem = CreateOpenPath(plant.Stem);
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = plant.StemThickness;
        paint.StrokeCap = SKStrokeCap.Round;
        if (silhouette)
        {
            paint.Color = SKColors.Black;
            canvas.DrawPath(stem, paint);
        }
        else
        {
            using var shader = SKShader.CreateLinearGradient(
                new SKPoint(plant.StemBase.X - (plant.StemThickness * 1.2f), plant.StemTop.Y),
                new SKPoint(plant.StemBase.X + (plant.StemThickness * 1.2f), plant.StemTop.Y),
                [material.StemLight, material.StemBase, material.StemEdge],
                null, SKShaderTileMode.Clamp);
            paint.Color = SKColors.White;
            paint.Shader = shader;
            canvas.DrawPath(stem, paint);
            paint.Shader = null;
        }

        paint.Style = SKPaintStyle.Fill;
        paint.StrokeCap = SKStrokeCap.Butt;
    }

    private static void DrawTulipLeaf(
        SKCanvas canvas,
        TulipLeaf leaf,
        TulipMaterial material,
        bool silhouette,
        bool omitDetail,
        SKPaint paint)
    {
        using var blade = CreatePath(leaf.Outline);
        if (silhouette)
        {
            paint.Color = SKColors.Black;
            canvas.DrawPath(blade, paint);
            return;
        }

        using var shade = SKShader.CreateLinearGradient(
            new SKPoint(leaf.Attachment.X, leaf.Attachment.Y),
            new SKPoint(leaf.Tip.X, leaf.Tip.Y),
            [material.LeafLight, material.LeafBase, material.LeafEdge],
            [0f, 0.55f, 1f], SKShaderTileMode.Clamp);
        paint.Color = SKColors.White;
        paint.Shader = shade;
        canvas.DrawPath(blade, paint);
        paint.Shader = null;
        if (omitDetail)
        {
            return;
        }

        using var vein = CreateOpenPath(leaf.Vein);
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = leaf.Width * 0.08f;
        paint.Color = material.LeafEdge.WithAlpha(140);
        canvas.DrawPath(vein, paint);
        paint.Style = SKPaintStyle.Fill;
    }

    private static void DrawTulipPetal(
        SKCanvas canvas,
        StillLifeScene scene,
        TulipPetal petal,
        TulipMaterial material,
        bool silhouette,
        bool omitDetail,
        SKPaint paint)
    {
        using var shape = CreatePath(petal.Outline);
        if (silhouette)
        {
            paint.Color = SKColors.Black;
            canvas.DrawPath(shape, paint);
            return;
        }

        // The tone hierarchy is what makes overlapping petals read as a cup: the back petal
        // stays dark, the front one is lifted towards the light colour.
        var baseColor = Blend(material.PetalBase, material.PetalLight, petal.Tone * 0.55f);
        var light = Blend(baseColor, material.PetalLight, 0.45f);
        var edge = Blend(baseColor, material.PetalEdge, 0.8f);
        var bounds = shape.Bounds;
        var reach = MathF.Max(bounds.Width, bounds.Height) * 0.85f;
        using var shade = SKShader.CreateRadialGradient(
            new SKPoint(
                bounds.MidX + (scene.LightDirection.X * bounds.Width * 0.22f),
                bounds.MidY + (scene.LightDirection.Y * bounds.Height * 0.26f)),
            reach,
            [light, baseColor, edge],
            [0f, 0.55f, 1f], SKShaderTileMode.Clamp);
        paint.Color = SKColors.White;
        paint.Shader = shade;
        canvas.DrawPath(shape, paint);
        paint.Shader = null;
        if (omitDetail)
        {
            return;
        }

        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = reach * 0.03f;
        paint.Color = material.PetalEdge.WithAlpha(110);
        canvas.DrawPath(shape, paint);
        paint.Style = SKPaintStyle.Fill;
    }

    private static SKPath CreateEllipsePath(System.Numerics.Vector2 center, System.Numerics.Vector2 radius)
    {
        const float Handle = 0.55228475f;
        using var builder = new SKPathBuilder();
        builder.MoveTo(center.X - radius.X, center.Y);
        builder.CubicTo(center.X - radius.X, center.Y - (Handle * radius.Y), center.X - (Handle * radius.X), center.Y - radius.Y, center.X, center.Y - radius.Y);
        builder.CubicTo(center.X + (Handle * radius.X), center.Y - radius.Y, center.X + radius.X, center.Y - (Handle * radius.Y), center.X + radius.X, center.Y);
        builder.CubicTo(center.X + radius.X, center.Y + (Handle * radius.Y), center.X + (Handle * radius.X), center.Y + radius.Y, center.X, center.Y + radius.Y);
        builder.CubicTo(center.X - (Handle * radius.X), center.Y + radius.Y, center.X - radius.X, center.Y + (Handle * radius.Y), center.X - radius.X, center.Y);
        builder.Close();
        return builder.Detach();
    }

    private static void DrawGlazeMottle(SKCanvas canvas, CeramicInstance ceramic, CeramicMaterial material)
    {
        if (material.MottleAlpha == 0)
        {
            return;
        }

        var random = Pcg32.ForStream(ceramic.Seed, ceramic.Kind.ToString().ToLowerInvariant() + ".glaze.detail");
        using var paint = new SKPaint { IsAntialias = true, Color = SKColors.White };
        for (var index = 0; index < 3; index++)
        {
            var x = 0.2f + ((float)random.NextDouble() * 0.6f);
            var y = 0.2f + ((float)random.NextDouble() * 0.6f);
            var radius = 0.18f + ((float)random.NextDouble() * 0.16f);
            using var shader = SKShader.CreateRadialGradient(
                new SKPoint(x, y), radius,
                [new SKColor(255, 252, 240, material.MottleAlpha), new SKColor(255, 252, 240, 0)], null, SKShaderTileMode.Clamp);
            paint.Shader = shader;
            canvas.DrawRect(0, 0, 1, 1, paint);
        }

        paint.Shader = null;
    }

    private static void DrawMouth(
        SKCanvas canvas,
        StillLifeScene scene,
        CeramicProfile profile,
        CeramicMaterial material,
        SKPaint paint,
        BeverageKind? beverage,
        bool accents,
        VaseFlower? flower,
        System.Numerics.Vector2 origin,
        System.Numerics.Vector2 size)
    {
        var center = profile.MouthCenter;
        var radius = profile.MouthRadius;
        using var cavity = SKShader.CreateRadialGradient(
            new SKPoint(center.X + (scene.LightDirection.X * 0.02f), center.Y + (profile.CavityDepth * 0.9f)), radius.X * 1.15f,
            [material.CavityColor.WithAlpha(240), material.CavityColor, Darken(material.BaseColor)],
            [0f, 0.62f, 1f], SKShaderTileMode.Clamp);
        paint.Color = SKColors.White;
        paint.Shader = cavity;
        canvas.DrawOval(center.X, center.Y, radius.X, radius.Y, paint);
        paint.Shader = null;
        paint.Color = material.LipColor.WithAlpha(38);
        canvas.DrawOval(center.X, center.Y - (radius.Y * 0.28f), radius.X * 0.7f, radius.Y * 0.48f, paint);
        paint.Color = material.CavityColor.WithAlpha(215);
        canvas.DrawOval(center.X, center.Y + (profile.CavityDepth * 0.5f), radius.X * 0.54f, radius.Y * 0.5f, paint);

        // A planted stem continues into the opening here, after the cavity base and before the
        // rim strokes, so the near lip cuts it and it reads as descending into the vase instead
        // of stopping behind the far lip. It is clipped to the opening itself, so it still
        // cannot reach the exterior wall.
        if (flower is not null)
        {
            DrawStemInsideOpening(canvas, flower, paint, origin, size);
        }

        // The drink goes after the cavity base and before the final rim and lip strokes, so
        // the ceramic edge always reads as being in front of the liquid.
        if (beverage is { } drink && profile.Kind == CeramicKind.Cup)
        {
            DrawCupLiquid(canvas, profile, drink, accents, paint);
        }

        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = profile.RimThickness;
        paint.Color = material.LipColor.WithAlpha(225);
        canvas.DrawOval(center.X, center.Y, radius.X, radius.Y, paint);
        var mouth = new SKRect(center.X - radius.X, center.Y - radius.Y, center.X + radius.X, center.Y + radius.Y);
        paint.StrokeWidth = profile.RimThickness * 2.2f;
        paint.Color = material.LipColor.WithAlpha(235);
        canvas.DrawArc(mouth, 0, 180, false, paint);
        paint.Style = SKPaintStyle.Fill;
    }

    private static SKPath CreateRing(CeramicHandle handle)
    {
        using var builder = new SKPathBuilder();
        AppendEllipse(builder, handle.Center, handle.OuterRadius, clockwise: true);
        AppendEllipse(builder, handle.Center, handle.InnerRadius, clockwise: false);
        builder.Close();
        return builder.Detach();
    }

    private static void AppendEllipse(SKPathBuilder builder, System.Numerics.Vector2 center, System.Numerics.Vector2 radius, bool clockwise)
    {
        const int Steps = 40;
        for (var index = 0; index <= Steps; index++)
        {
            var step = clockwise ? index : Steps - index;
            var angle = step * MathF.Tau / Steps;
            var x = center.X + (MathF.Cos(angle) * radius.X);
            var y = center.Y + (MathF.Sin(angle) * radius.Y);
            if (index == 0)
            {
                builder.MoveTo(x, y);
            }
            else
            {
                builder.LineTo(x, y);
            }
        }
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

    private static SKPath CreatePath(ImmutableArray<CubicSegment> segments)
    {
        using var builder = new SKPathBuilder();
        var start = segments[0].Start;
        builder.MoveTo(start.X, start.Y);
        foreach (var segment in segments)
        {
            builder.CubicTo(segment.Control1.X, segment.Control1.Y, segment.Control2.X, segment.Control2.Y, segment.End.X, segment.End.Y);
        }

        builder.Close();
        return builder.Detach();
    }

    private static SKPath CreateOpenPath(ImmutableArray<CubicSegment> segments)
    {
        using var builder = new SKPathBuilder();
        var start = segments[0].Start;
        builder.MoveTo(start.X, start.Y);
        foreach (var segment in segments)
        {
            builder.CubicTo(segment.Control1.X, segment.Control1.Y, segment.Control2.X, segment.Control2.Y, segment.End.X, segment.End.Y);
        }

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

    private static SKColor Blend(SKColor from, SKColor to, float amount) => new(
        (byte)Math.Clamp(from.Red + ((to.Red - from.Red) * amount), 0f, 255f),
        (byte)Math.Clamp(from.Green + ((to.Green - from.Green) * amount), 0f, 255f),
        (byte)Math.Clamp(from.Blue + ((to.Blue - from.Blue) * amount), 0f, 255f),
        from.Alpha);

    private static SKColor Lighten(SKColor color) => new(
        (byte)Math.Min(255, color.Red + 58),
        (byte)Math.Min(255, color.Green + 60),
        (byte)Math.Min(255, color.Blue + 35));

    private static SKColor Darken(SKColor color) => new(
        (byte)(color.Red * 0.42),
        (byte)(color.Green * 0.42),
        (byte)(color.Blue * 0.44));
}
