using System.Collections.Immutable;
using System.Numerics;
using Formloom.Core;
using Formloom.Rendering.Skia;
using SkiaSharp;
using Xunit;

namespace Formloom.Tests;

public sealed class CompositionInvariantTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void InstanceDetailCannotLeakOutsideItsOwnContourOrThroughForeground(int targetDepth)
    {
        var generated = SceneGenerator.Generate(new("an orange and a red apple on a wooden table", Width: 256, Height: 256)).Scene!;
        var back = generated.Fruits[0] with
        {
            Depth = 0,
            Body = generated.Fruits[0].Body with { Center = new Vector2(0.46f, 0.60f) },
        };
        var front = generated.Fruits[1] with
        {
            Depth = 1,
            Body = generated.Fruits[1].Body with { Center = new Vector2(0.57f, 0.60f) },
        };
        var scene = generated with { Instances = ImmutableArray.Create(back, front) };
        Assert.Null(SceneValidator.Validate(scene));
        var target = scene.Fruits.Single(fruit => fruit.Depth == targetDepth);
        using var targetMask = BodyMask(scene, target);
        using var backMask = BodyMask(scene, back);
        using var frontMask = BodyMask(scene, front);
        using var detailed = SKBitmap.Decode(StillLifeRenderer.Render(scene).PngBytes);
        using var changed = SKBitmap.Decode(StillLifeRenderer.RenderWithoutInstanceDetail(scene, targetDepth).PngBytes);
        var frontScene = scene with { Instances = null, Apple = front.Body, Subject = front.Subject, Seed = front.Seed };
        using var foreground = SKBitmap.Decode(StillLifeRenderer.Render(frontScene).PngBytes);
        var overlapPixels = 0;
        var changedPixels = 0;
        for (var y = 0; y < scene.Height; y++)
        {
            for (var x = 0; x < scene.Width; x++)
            {
                var before = detailed.GetPixel(x, y);
                var after = changed.GetPixel(x, y);
                if (targetMask.GetPixel(x, y).Alpha == 0)
                {
                    Assert.Equal(before, after);
                }

                if (backMask.GetPixel(x, y).Alpha == 255 && frontMask.GetPixel(x, y).Alpha == 255)
                {
                    overlapPixels++;
                    Assert.Equal(foreground.GetPixel(x, y), before);
                    if (targetDepth == 0)
                    {
                        Assert.Equal(before, after);
                    }
                }

                if (before != after)
                {
                    changedPixels++;
                }
            }
        }

        Assert.True(overlapPixels > 200);
        Assert.True(changedPixels > 20);
    }

    [Theory]
    [InlineData("a red apple and a green pear on a wooden table")]
    [InlineData("a red apple, a green pear, and an orange on a wooden table")]
    public void CastAndContactShadowMasksAreGroundedAndClearOfCanvasEdges(string prompt)
    {
        ulong[] seeds = [0, 42, 1337];
        double[] variations = [0, 1];
        foreach (var seed in seeds)
        {
            foreach (var variation in variations)
            {
                var scene = SceneGenerator.Generate(new(prompt, seed, variation, Width: 256, Height: 256)).Scene!;
                using var shadows = SKBitmap.Decode(StillLifeRenderer.RenderShadowMask(scene).PngBytes);
                for (var edge = 0; edge < scene.Width; edge++)
                {
                    Assert.Equal((byte)0, shadows.GetPixel(edge, 0).Alpha);
                    Assert.Equal((byte)0, shadows.GetPixel(edge, scene.Height - 1).Alpha);
                    Assert.Equal((byte)0, shadows.GetPixel(0, edge).Alpha);
                    Assert.Equal((byte)0, shadows.GetPixel(scene.Width - 1, edge).Alpha);
                }

                foreach (var fruit in scene.Fruits)
                {
                    var bottom = fruit.Body.Center.Y + (fruit.Body.Size.Y / 2);
                    var contactX = (int)(fruit.Body.Center.X * scene.Width);
                    var contactY = (int)(bottom * scene.Height);
                    Assert.True(shadows.GetPixel(contactX, contactY).Alpha > 20);
                    var castX = (int)((fruit.Body.Center.X + (fruit.Body.Size.X * 0.4f)) * scene.Width);
                    Assert.True(shadows.GetPixel(castX, contactY).Alpha > 0);
                }
            }
        }
    }

    [Fact]
    public void DiagnosticPassesDoNotChangeSubsequentNormalRendering()
    {
        var scene = SceneGenerator.Generate(new("a red apple, a green pear, and an orange on a wooden table", Width: 128, Height: 128)).Scene!;
        var original = StillLifeRenderer.Render(scene);
        using var shadows = SKBitmap.Decode(StillLifeRenderer.RenderShadowMask(scene).PngBytes);
        using var changed = SKBitmap.Decode(StillLifeRenderer.RenderWithoutInstanceDetail(scene, scene.Fruits[0].Depth).PngBytes);
        Assert.NotNull(shadows);
        Assert.NotNull(changed);
        Assert.Equal(original.PngBytes, StillLifeRenderer.Render(scene).PngBytes);
    }

    private static SKBitmap BodyMask(StillLifeScene scene, FruitInstance fruit)
    {
        var bitmap = new SKBitmap(scene.Width, scene.Height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        canvas.Scale(scene.Width, scene.Height);
        canvas.Translate(fruit.Body.Center.X - (fruit.Body.Size.X / 2), fruit.Body.Center.Y - (fruit.Body.Size.Y / 2));
        canvas.Scale(fruit.Body.Size.X, fruit.Body.Size.Y);
        var contour = OrganicForms.Create(fruit.Subject, fruit.Body.Asymmetry, fruit.Seed, scene.Variation);
        using var builder = new SKPathBuilder();
        builder.MoveTo(contour.Segments[0].Start.X, contour.Segments[0].Start.Y);
        foreach (var segment in contour.Segments)
        {
            builder.CubicTo(segment.Control1.X, segment.Control1.Y, segment.Control2.X, segment.Control2.Y, segment.End.X, segment.End.Y);
        }

        builder.Close();
        using var path = builder.Detach();
        using var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
        canvas.DrawPath(path, paint);
        return bitmap;
    }
}
