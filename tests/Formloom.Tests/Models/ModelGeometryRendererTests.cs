using System.Collections.Immutable;
using System.Numerics;
using Formloom.Core.Core;
using Formloom.Core.Models.Catalog;
using Formloom.Core.Models.Evaluation;
using Formloom.Core.Models.Geometry;
using Formloom.Rendering.Skia.Models;
using Formloom.Rendering.Skia.Scenes;
using SkiaSharp;
using Xunit;

namespace Formloom.Tests.Models;

public sealed class ModelGeometryRendererTests
{
    [Fact]
    public void FillsClosedPathsInPathOrderUsingTheirMaterialSlots()
    {
        var geometry = new ModelGeometry(
            "models.fill-test",
            ModelGeometryCoordinateSpace.SceneNormalized,
            [
                Rectangle("base", 0.1f, 0.1f, 0.9f, 0.9f, "petal.base"),
                Rectangle("highlight", 0.4f, 0.4f, 0.7f, 0.7f, "petal.light"),
            ])
        {
            MaterialTokens = ImmutableDictionary<string, string>.Empty
                .Add("petal.base", "#AA1E32")
                .Add("petal.light", "#14BE5A"),
        };

        var rendered = ModelGeometryRenderer.Render(geometry, 128, 96, new RenderOptions(Antialiasing: false));

        Assert.Equal("models.fill-test", rendered.Subject);
        using var bitmap = Decode(rendered.PngBytes);
        Assert.Equal(new SKColor(170, 30, 50), bitmap.GetPixel(30, 30));
        Assert.Equal(new SKColor(20, 190, 90), bitmap.GetPixel(64, 48));
        Assert.Equal((byte)0, bitmap.GetPixel(120, 48).Alpha);
    }

    [Fact]
    public void AppliesAlphaFromEightDigitMaterialTokens()
    {
        var geometry = new ModelGeometry(
            "models.alpha-test",
            ModelGeometryCoordinateSpace.SceneNormalized,
            [Rectangle("translucent", 0.2f, 0.2f, 0.8f, 0.8f, "surface")])
        {
            MaterialTokens = ImmutableDictionary<string, string>.Empty.Add("surface", "#1681C480"),
        };

        var rendered = ModelGeometryRenderer.Render(geometry, 128, 128, new RenderOptions(Antialiasing: false));

        using var bitmap = Decode(rendered.PngBytes);
        var pixel = bitmap.GetPixel(64, 64);
        Assert.Equal((byte)128, pixel.Alpha);
        Assert.InRange(pixel.Red, (byte)20, (byte)24);
        Assert.InRange(pixel.Green, (byte)126, (byte)132);
        Assert.InRange(pixel.Blue, (byte)194, (byte)198);
    }

    [Fact]
    public void LeavesOpenPathsUnfilled()
    {
        var geometry = new ModelGeometry(
            "models.open-path-test",
            ModelGeometryCoordinateSpace.SceneNormalized,
            [
                new(
                    "stem",
                    "stem",
                    [Line(new(0.1f, 0.5f), new(0.9f, 0.5f))],
                    false,
                    "stem.base",
                    false),
            ])
        {
            MaterialTokens = ImmutableDictionary<string, string>.Empty.Add("stem.base", "#627C32"),
        };

        var rendered = ModelGeometryRenderer.Render(geometry, 128, 128, new RenderOptions(Antialiasing: false));

        using var bitmap = Decode(rendered.PngBytes);
        Assert.Equal((byte)0, bitmap.GetPixel(64, 64).Alpha);
    }

    [Fact]
    public void RejectsClosedPathsWithUnresolvedMaterialSlots()
    {
        var geometry = new ModelGeometry(
            "models.missing-slot",
            ModelGeometryCoordinateSpace.SceneNormalized,
            [Rectangle("petal", 0.1f, 0.1f, 0.9f, 0.9f, "petal.base")]);

        var exception = Assert.Throws<ArgumentException>(() => ModelGeometryRenderer.Render(geometry));

        Assert.Contains("petal.base", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RendersAndExportsBothExternalRoseMaterialVariants()
    {
        var red = EvaluateRose("red");
        var pink = EvaluateRose("pink");

        Assert.Equal("#B02232", red.MaterialTokens["petal.base"]);
        Assert.Equal("#D03A74", pink.MaterialTokens["petal.base"]);

        var redPreview = ModelGeometryRenderer.Render(red, 512, 512);
        var pinkPreview = ModelGeometryRenderer.Render(pink, 512, 512);
        using var redBitmap = Decode(redPreview.PngBytes);
        using var pinkBitmap = Decode(pinkPreview.PngBytes);

        Assert.Equal("flowers.rose-probe", redPreview.Subject);
        Assert.True(CountFilledPixels(redBitmap) > 1000);
        Assert.True(CountFilledPixels(pinkBitmap) > 1000);
        Assert.False(redBitmap.Bytes.SequenceEqual(pinkBitmap.Bytes));

        File.WriteAllBytes(
            Path.Combine(AppContext.BaseDirectory, "formloom-model-rose-solid-fill-red.png"),
            redPreview.PngBytes);
        File.WriteAllBytes(
            Path.Combine(AppContext.BaseDirectory, "formloom-model-rose-solid-fill-pink.png"),
            pinkPreview.PngBytes);
    }

    private static ModelGeometry EvaluateRose(string color)
    {
        var catalog = ModelDefinitionCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Fixtures"));
        Assert.True(catalog.IsSuccess, string.Join(Environment.NewLine, catalog.Diagnostics.Select(item => item.Message)));
        var definition = Assert.Single(catalog.Models).Value;
        var context = ModelEvaluationContext.Empty(42, 0f) with
        {
            ChoiceParameters = ImmutableDictionary<string, string>.Empty.Add("color", color),
            Frames = ImmutableDictionary<string, ModelMountFrameValues>.Empty.Add(
                "vase-opening",
                new(new(0.5f, 0.65f), 0.12f, 0.07f, 0.58f, 0.015f, 0.4f)),
        };

        var result = ModelDefinitionEvaluator.Evaluate(definition, context);
        Assert.True(result.IsSuccess, string.Join(Environment.NewLine, result.Diagnostics.Select(item => item.Message)));
        return Assert.IsType<ModelGeometry>(result.Geometry);
    }

    private static int CountFilledPixels(SKBitmap bitmap)
    {
        var count = 0;
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).Alpha > 0)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static SKBitmap Decode(byte[] pngBytes) =>
        SKBitmap.Decode(pngBytes) ?? throw new InvalidOperationException("The renderer returned an invalid PNG.");

    private static ModelGeometryPath Rectangle(
        string id,
        float left,
        float top,
        float right,
        float bottom,
        string materialSlot)
    {
        var topLeft = new Vector2(left, top);
        var topRight = new Vector2(right, top);
        var bottomRight = new Vector2(right, bottom);
        var bottomLeft = new Vector2(left, bottom);
        return new(
            id,
            id,
            [
                Line(topLeft, topRight),
                Line(topRight, bottomRight),
                Line(bottomRight, bottomLeft),
                Line(bottomLeft, topLeft),
            ],
            true,
            materialSlot,
            false);
    }

    private static CubicSegment Line(Vector2 start, Vector2 end)
    {
        var delta = end - start;
        return new(start, start + (delta / 3f), start + ((delta * 2f) / 3f), end);
    }
}
