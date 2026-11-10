using System.Collections.Immutable;
using System.Numerics;
using Formloom.Core.Core;
using Formloom.Core.Models.Geometry;
using Xunit;

namespace Formloom.Tests.Models;

public sealed class ModelGeometryTests
{
    [Fact]
    public void ValidatesOrderedSceneNormalizedPaths()
    {
        var geometry = new ModelGeometry(
            "flowers.example",
            ModelGeometryCoordinateSpace.SceneNormalized,
            [Path("stem", "stem.node", false), Path("leaf", "leaf.node", true)]);

        Assert.Empty(ModelGeometryValidator.Validate(geometry));
        Assert.Equal("stem", geometry.Paths[0].Id);
        Assert.Equal("leaf", geometry.Paths[1].Id);
    }

    [Fact]
    public void RejectsMissingModelIdAndEmptyPaths()
    {
        var result = ModelGeometryValidator.Validate(
            new(" ", ModelGeometryCoordinateSpace.SceneNormalized, ImmutableArray<ModelGeometryPath>.Empty));

        Assert.Contains(result, item => item.Code == "geometry.model-id-required");
        Assert.Contains(result, item => item.Code == "geometry.paths-required");
    }

    [Fact]
    public void RejectsDuplicatePathIds()
    {
        var result = ModelGeometryValidator.Validate(
            new("flowers.example", ModelGeometryCoordinateSpace.SceneNormalized, [Path("same", "a"), Path("same", "b")]));

        Assert.Contains(result, item => item.Code == "geometry.path-id-duplicate");
    }

    [Fact]
    public void RejectsMissingSegmentsAndSourceNode()
    {
        var result = ModelGeometryValidator.Validate(
            new("flowers.example", ModelGeometryCoordinateSpace.SceneNormalized,
                [new("empty", " ", ImmutableArray<CubicSegment>.Empty, false, null, false)]));

        Assert.Contains(result, item => item.Code == "geometry.source-node-required");
        Assert.Contains(result, item => item.Code == "geometry.segments-required");
    }

    [Fact]
    public void RejectsNonFiniteCoordinates()
    {
        var segment = new CubicSegment(new(float.NaN, 0), Vector2.Zero, Vector2.One, Vector2.One);
        var result = ModelGeometryValidator.Validate(
            new("flowers.example", ModelGeometryCoordinateSpace.SceneNormalized,
                [new("path", "node", [segment], false, null, false)]));

        Assert.Contains(result, item => item.Code == "geometry.segment-non-finite");
    }

    [Fact]
    public void RejectsDisconnectedSegments()
    {
        var segments = ImmutableArray.Create(
            new CubicSegment(Vector2.Zero, new(0.2f, 0), new(0.8f, 0), Vector2.One),
            new CubicSegment(new(2, 2), new(2, 3), new(3, 3), new(3, 2)));
        var result = ModelGeometryValidator.Validate(
            new("flowers.example", ModelGeometryCoordinateSpace.SceneNormalized,
                [new("path", "node", segments, false, null, false)]));

        Assert.Contains(result, item => item.Code == "geometry.segment-disconnected");
    }

    [Fact]
    public void RejectsClosedPathThatDoesNotClose()
    {
        var result = ModelGeometryValidator.Validate(
            new("flowers.example", ModelGeometryCoordinateSpace.SceneNormalized,
                [Path("path", "node", true, new(2, 2))]));

        Assert.Contains(result, item => item.Code == "geometry.closed-path-open");
    }

    [Fact]
    public void RejectsInvalidMaterialGradientEndpointsAndStops()
    {
        var geometry = new ModelGeometry(
            "flowers.gradient-test",
            ModelGeometryCoordinateSpace.SceneNormalized,
            [Path("petal", "petal.node", true)])
        {
            MaterialTokens = ImmutableDictionary<string, string>.Empty
                .Add("petal.base", "#B02232")
                .Add("petal.light", "#ED5D67"),
            MaterialGradients = ImmutableDictionary<string, ModelGeometryLinearGradient>.Empty.Add(
                "petal.base",
                new(
                    new(0.5f, 0.5f),
                    new(0.5f, 0.5f),
                    [
                        new(0.7f, "petal.light"),
                        new(0.2f, "petal.base"),
                    ])),
        };

        var diagnostics = ModelGeometryValidator.Validate(geometry);

        Assert.Contains(diagnostics, item => item.Code == "geometry.material-gradient-endpoints-invalid");
        Assert.Contains(diagnostics, item => item.Code == "geometry.material-gradient-stop-invalid");
    }

    [Fact]
    public void BoundsContainEveryCubicControlPoint()
    {
        var path = new ModelGeometryPath(
            "path", "node",
            [new(new(0.2f, 0.3f), new(0.1f, 0.8f), new(0.9f, 0.7f), new(0.8f, 0.2f))],
            false, null, false);

        var bounds = ModelGeometryBounds.From(path);

        Assert.Equal(0.1f, bounds.Left);
        Assert.Equal(0.2f, bounds.Top);
        Assert.Equal(0.9f, bounds.Right);
        Assert.Equal(0.8f, bounds.Bottom);
    }

    private static ModelGeometryPath Path(string id, string source, bool closed = false, Vector2? end = null) =>
        new(id, source, [new(Vector2.Zero, new(0.25f, 0), new(0.75f, 0), end ?? Vector2.Zero)], closed, "default", false);
}
