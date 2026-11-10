using System.Collections.Immutable;
using System.Numerics;
using Formloom.Core.Core;
using Formloom.Core.Ceramics;
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
    public void FillsClosedPathsWithAuthoredLinearGradients()
    {
        var geometry = new ModelGeometry(
            "models.gradient-test",
            ModelGeometryCoordinateSpace.SceneNormalized,
            [Rectangle("surface", 0.1f, 0.1f, 0.9f, 0.9f, "surface")])
        {
            MaterialTokens = ImmutableDictionary<string, string>.Empty
                .Add("surface", "#00FF00")
                .Add("gradient.start", "#FF0000")
                .Add("gradient.end", "#0000FF"),
            MaterialGradients = ImmutableDictionary<string, ModelGeometryLinearGradient>.Empty.Add(
                "surface",
                new(
                    new(0.5f, 0.25f),
                    new(0.5f, 0.75f),
                    [
                        new(0f, "gradient.start"),
                        new(1f, "gradient.end"),
                    ])),
        };

        var rendered = ModelGeometryRenderer.Render(geometry, 128, 128, new RenderOptions(Antialiasing: false));

        using var bitmap = Decode(rendered.PngBytes);
        Assert.Equal(new SKColor(255, 0, 0), bitmap.GetPixel(64, 24));
        Assert.Equal(new SKColor(0, 0, 255), bitmap.GetPixel(64, 104));
        var midpoint = bitmap.GetPixel(64, 64);
        Assert.InRange(midpoint.Red, (byte)126, (byte)129);
        Assert.Equal((byte)0, midpoint.Green);
        Assert.InRange(midpoint.Blue, (byte)126, (byte)129);
        Assert.Equal((byte)255, midpoint.Alpha);
    }

    [Fact]
    public void OmitsDetailPathsWhenDetailRenderingIsDisabled()
    {
        var geometry = new ModelGeometry(
            "models.detail-test",
            ModelGeometryCoordinateSpace.SceneNormalized,
            [
                Rectangle("surface", 0.1f, 0.1f, 0.9f, 0.9f, "surface"),
                Rectangle("detail", 0.4f, 0.4f, 0.6f, 0.6f, "detail") with { Detail = true },
            ])
        {
            MaterialTokens = ImmutableDictionary<string, string>.Empty
                .Add("surface", "#168040")
                .Add("detail", "#E02040"),
        };

        var detailed = ModelGeometryRenderer.Render(geometry, 128, 128, new RenderOptions(Antialiasing: false));
        var simplified = ModelGeometryRenderer.RenderWithOptions(
            geometry,
            128,
            128,
            new ModelGeometryRenderOptions(Antialiasing: false, IncludeDetails: false));

        using var detailedBitmap = Decode(detailed.PngBytes);
        using var simplifiedBitmap = Decode(simplified.PngBytes);
        Assert.Equal(new SKColor(224, 32, 64), detailedBitmap.GetPixel(64, 64));
        Assert.Equal(new SKColor(22, 128, 64), simplifiedBitmap.GetPixel(64, 64));
    }

    [Fact]
    public void AppliesHostOcclusionMaskWithoutChangingUnoccludedModelPixels()
    {
        var geometry = new ModelGeometry(
            "models.host-occlusion-test",
            ModelGeometryCoordinateSpace.SceneNormalized,
            [Rectangle("surface", 0.1f, 0.1f, 0.9f, 0.9f, "surface")])
        {
            MaterialTokens = ImmutableDictionary<string, string>.Empty.Add("surface", "#168040"),
        };

        using var maskBuilder = new SKPathBuilder();
        maskBuilder.MoveTo(0f, 0.5f);
        maskBuilder.LineTo(1f, 0.5f);
        maskBuilder.LineTo(1f, 1f);
        maskBuilder.LineTo(0f, 1f);
        maskBuilder.Close();
        using var occlusionMask = maskBuilder.Detach();

        var rendered = ModelGeometryRenderer.RenderWithOptions(
            geometry,
            128,
            128,
            new ModelGeometryRenderOptions(Antialiasing: false)
            {
                OcclusionMask = occlusionMask,
            });

        using var bitmap = Decode(rendered.PngBytes);
        Assert.Equal(new SKColor(22, 128, 64), bitmap.GetPixel(64, 30));
        Assert.Equal((byte)0, bitmap.GetPixel(64, 80).Alpha);
        Assert.Equal((byte)0, bitmap.GetPixel(10, 80).Alpha);
    }

    [Fact]
    public void VaseNearRimOcclusionKeepsTheModelInsideTheOpeningAndHidesItsLowerContinuation()
    {
        var scene = SceneGenerator.Generate(
            new("a red rose in a blue ceramic vase on a wooden table", Width: 256, Height: 256)).Scene!;
        var vase = Assert.Single(scene.Ceramics);
        using var mask = CeramicOcclusionMask.BelowVaseNearRim(vase);
        var profile = CeramicForms.For(vase.Kind);
        var centerX = vase.Body.Center.X - (vase.Body.Size.X / 2) + (profile.MouthCenter.X * vase.Body.Size.X);
        var centerY = vase.Body.Center.Y - (vase.Body.Size.Y / 2) + (profile.MouthCenter.Y * vase.Body.Size.Y);
        var nearRimY = centerY + ((profile.MouthRadius.Y + (profile.RimThickness * 1.1f)) * vase.Body.Size.Y);

        var geometry = new ModelGeometry(
            "models.vase-mounted-test",
            ModelGeometryCoordinateSpace.SceneNormalized,
            [Rectangle("stem", centerX - 0.04f, centerY - 0.04f, centerX + 0.04f, nearRimY + 0.08f, "stem")])
        {
            MaterialTokens = ImmutableDictionary<string, string>.Empty.Add("stem", "#168040"),
        };

        var rendered = ModelGeometryRenderer.RenderWithOptions(
            geometry,
            256,
            256,
            new ModelGeometryRenderOptions(Antialiasing: false)
            {
                OcclusionMask = mask,
            });

        using var bitmap = Decode(rendered.PngBytes);
        var x = Math.Clamp((int)(centerX * 256), 0, 255);
        var visibleY = Math.Clamp((int)((centerY + (profile.MouthRadius.Y * vase.Body.Size.Y * 0.5f)) * 256), 0, 255);
        var hiddenY = Math.Clamp((int)((nearRimY + 0.025f) * 256), 0, 255);
        Assert.Equal(new SKColor(22, 128, 64), bitmap.GetPixel(x, visibleY));
        Assert.Equal((byte)0, bitmap.GetPixel(x, hiddenY).Alpha);
    }

    [Fact]
    public void RendersPrimaryGeometryAsSilhouetteAndOmitsDetailPaths()
    {
        var geometry = new ModelGeometry(
            "models.silhouette-test",
            ModelGeometryCoordinateSpace.SceneNormalized,
            [
                Rectangle("surface", 0.1f, 0.1f, 0.5f, 0.5f, "surface"),
                Rectangle("detail", 0.6f, 0.6f, 0.9f, 0.9f, "detail") with { Detail = true },
                new ModelGeometryPath(
                    "stem",
                    "stem",
                    [Line(new(0.1f, 0.75f), new(0.5f, 0.75f))],
                    false,
                    "stem",
                    false)
                {
                    StrokeWidth = 0.04f,
                },
            ])
        {
            MaterialTokens = ImmutableDictionary<string, string>.Empty
                .Add("surface", "#E02040")
                .Add("detail", "#20A0E0")
                .Add("stem", "#168040"),
            MaterialGradients = ImmutableDictionary<string, ModelGeometryLinearGradient>.Empty.Add(
                "surface",
                new(new(0.1f, 0.1f), new(0.5f, 0.5f), [new(0f, "surface"), new(1f, "detail")])),
        };

        var rendered = ModelGeometryRenderer.RenderWithOptions(
            geometry,
            128,
            128,
            new ModelGeometryRenderOptions(Antialiasing: false, Silhouette: true));

        using var bitmap = Decode(rendered.PngBytes);
        Assert.Equal(SKColors.Black, bitmap.GetPixel(30, 30));
        Assert.Equal(SKColors.Black, bitmap.GetPixel(38, 96));
        Assert.Equal((byte)0, bitmap.GetPixel(96, 96).Alpha);
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
    public void StrokesOpenPathsAtTheirDeclaredWidthWithRoundCaps()
    {
        var geometry = new ModelGeometry(
            "models.stroke-test",
            ModelGeometryCoordinateSpace.SceneNormalized,
            [
                new ModelGeometryPath(
                    "stem",
                    "stem",
                    [Line(new(0.25f, 0.5f), new(0.75f, 0.5f))],
                    false,
                    "stem.base",
                    false)
                {
                    StrokeWidth = 0.08f,
                },
            ])
        {
            MaterialTokens = ImmutableDictionary<string, string>.Empty.Add("stem.base", "#FF1428"),
        };

        var rendered = ModelGeometryRenderer.Render(geometry, 128, 128, new RenderOptions(Antialiasing: false));

        using var bitmap = Decode(rendered.PngBytes);
        Assert.Equal(new SKColor(255, 20, 40), bitmap.GetPixel(64, 64));
        Assert.Equal(new SKColor(255, 20, 40), bitmap.GetPixel(28, 64));
        Assert.Equal((byte)0, bitmap.GetPixel(20, 64).Alpha);
        Assert.Equal((byte)0, bitmap.GetPixel(64, 54).Alpha);
    }

    [Fact]
    public void RejectsInvalidStrokeWidthsAndStrokesWithoutMaterialSlots()
    {
        var invalidWidth = new ModelGeometry(
            "models.invalid-stroke",
            ModelGeometryCoordinateSpace.SceneNormalized,
            [
                new ModelGeometryPath(
                    "stem",
                    "stem",
                    [Line(new(0.1f, 0.5f), new(0.9f, 0.5f))],
                    false,
                    "stem.base",
                    false)
                {
                    StrokeWidth = 0,
                },
            ]);
        var missingSlot = invalidWidth with
        {
            Paths = invalidWidth.Paths.SetItem(0, invalidWidth.Paths[0] with
            {
                MaterialSlot = null,
                StrokeWidth = 0.01f,
            }),
        };

        Assert.Contains(ModelGeometryValidator.Validate(invalidWidth), item => item.Code == "geometry.stroke-width-invalid");
        Assert.Contains(ModelGeometryValidator.Validate(missingSlot), item => item.Code == "geometry.stroke-material-slot-required");
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
        Assert.Equal(
            new[] { "petal.light", "petal.base" },
            red.MaterialGradients["petal.base"].Stops.Select(stop => stop.Token));
        Assert.Equal(
            new[] { "petal.light", "petal.base" },
            pink.MaterialGradients["petal.base"].Stops.Select(stop => stop.Token));
        AssertRoseStrokesAreAuthoredInTheFixture(red);
        AssertRoseStrokesAreAuthoredInTheFixture(pink);

        var redPreview = ModelGeometryRenderer.Render(red, 512, 512);
        var pinkPreview = ModelGeometryRenderer.Render(pink, 512, 512);
        var redSimplePreview = ModelGeometryRenderer.RenderWithOptions(
            red, 512, 512, new ModelGeometryRenderOptions(IncludeDetails: false));
        var pinkSimplePreview = ModelGeometryRenderer.RenderWithOptions(
            pink, 512, 512, new ModelGeometryRenderOptions(IncludeDetails: false));
        var redSilhouettePreview = ModelGeometryRenderer.RenderWithOptions(
            red, 512, 512, new ModelGeometryRenderOptions(Silhouette: true));
        var pinkSilhouettePreview = ModelGeometryRenderer.RenderWithOptions(
            pink, 512, 512, new ModelGeometryRenderOptions(Silhouette: true));
        using var redBitmap = Decode(redPreview.PngBytes);
        using var pinkBitmap = Decode(pinkPreview.PngBytes);

        Assert.Equal("flowers.rose-probe", redPreview.Subject);
        Assert.True(CountFilledPixels(redBitmap) > 1000);
        Assert.True(CountFilledPixels(pinkBitmap) > 1000);
        Assert.False(redBitmap.Bytes.SequenceEqual(pinkBitmap.Bytes));
        Assert.False(redPreview.PngBytes.SequenceEqual(redSimplePreview.PngBytes));
        Assert.False(redSimplePreview.PngBytes.SequenceEqual(redSilhouettePreview.PngBytes));

        File.WriteAllBytes(
            Path.Combine(AppContext.BaseDirectory, "formloom-model-rose-gradients-red.png"),
            redPreview.PngBytes);
        File.WriteAllBytes(
            Path.Combine(AppContext.BaseDirectory, "formloom-model-rose-gradients-pink.png"),
            pinkPreview.PngBytes);
        File.WriteAllBytes(
            Path.Combine(AppContext.BaseDirectory, "formloom-model-rose-details-omitted-red.png"),
            redSimplePreview.PngBytes);
        File.WriteAllBytes(
            Path.Combine(AppContext.BaseDirectory, "formloom-model-rose-details-omitted-pink.png"),
            pinkSimplePreview.PngBytes);
        File.WriteAllBytes(
            Path.Combine(AppContext.BaseDirectory, "formloom-model-rose-silhouette-red.png"),
            redSilhouettePreview.PngBytes);
        File.WriteAllBytes(
            Path.Combine(AppContext.BaseDirectory, "formloom-model-rose-silhouette-pink.png"),
            pinkSilhouettePreview.PngBytes);
    }

    private static void AssertRoseStrokesAreAuthoredInTheFixture(ModelGeometry geometry)
    {
        var stem = Assert.Single(geometry.Paths, path => path.Id == "rose.stem");
        Assert.Equal(0.006f, stem.StrokeWidth);

        var veins = geometry.Paths
            .Where(path => path.Id is "rose.leaf.left.vein" or "rose.leaf.right.vein")
            .ToArray();
        Assert.Equal(2, veins.Length);
        Assert.All(veins, path => Assert.Equal(0.0015f, path.StrokeWidth));

        var folds = geometry.Paths
            .Where(path => path.Id.StartsWith("rose.petals.", StringComparison.Ordinal) &&
                           path.Id.Contains(".fold[", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(13, folds.Length);
        Assert.All(folds, path => Assert.Equal(0.0018f, path.StrokeWidth));
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
