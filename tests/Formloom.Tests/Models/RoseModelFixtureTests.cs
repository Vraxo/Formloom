using System.Collections.Immutable;
using Formloom.Core.Models.Catalog;
using Formloom.Core.Models.Evaluation;
using Formloom.Core.Models.Geometry;
using Xunit;

namespace Formloom.Tests.Models;

public sealed class RoseModelFixtureTests
{
    [Theory]
    [InlineData(0f)]
    [InlineData(0.35f)]
    [InlineData(1f)]
    public void DiscoversAndEvaluatesRoseGeometryFromExternalDefinition(float variationAmount)
    {
        var fixtureRoot = Path.Combine(AppContext.BaseDirectory, "Fixtures");
        var catalog = ModelDefinitionCatalog.Load(fixtureRoot);

        Assert.True(catalog.IsSuccess, string.Join(Environment.NewLine, catalog.Diagnostics.Select(item => item.Message)));
        var definition = Assert.Single(catalog.Models).Value;
        Assert.Equal("flowers.rose-probe", definition.Id);

        var context = ModelEvaluationContext.Empty(42, variationAmount) with
        {
            Frames = ImmutableDictionary<string, ModelMountFrameValues>.Empty.Add(
                "vase-opening",
                new(new(0.5f, 0.65f), 0.12f, 0.07f, 0.58f, 0.015f, 0.4f)),
        };

        var result = ModelDefinitionEvaluator.Evaluate(definition, context);

        Assert.True(result.IsSuccess, string.Join(Environment.NewLine, result.Diagnostics.Select(item => item.Message)));
        Assert.Equal("red", result.ChoiceParameters["color"]);

        var geometry = Assert.IsType<ModelGeometry>(result.Geometry);
        Assert.Equal(16, geometry.Paths.Length);
        Assert.Equal("rose.stem", geometry.Paths[0].Id);
        Assert.Equal("rose.leaf.left", geometry.Paths[1].Id);
        Assert.Equal("rose.leaf.right", geometry.Paths[2].Id);

        var outer = geometry.Paths.Where(path => path.Id.StartsWith("rose.petals.outer[", StringComparison.Ordinal)).ToArray();
        var middle = geometry.Paths.Where(path => path.Id.StartsWith("rose.petals.middle[", StringComparison.Ordinal)).ToArray();
        var inner = geometry.Paths.Where(path => path.Id.StartsWith("rose.petals.inner[", StringComparison.Ordinal)).ToArray();
        Assert.Equal(5, outer.Length);
        Assert.Equal(5, middle.Length);
        Assert.Equal(3, inner.Length);
        Assert.All(outer, path => Assert.InRange(path.MaterialTone!.Value, 0.43f - (0.08f * variationAmount), 0.43f + (0.08f * variationAmount)));
        Assert.All(middle, path => Assert.InRange(path.MaterialTone!.Value, 0.64f - (0.08f * variationAmount), 0.64f + (0.08f * variationAmount)));
        Assert.All(inner, path => Assert.InRange(path.MaterialTone!.Value, 0.84f - (0.06f * variationAmount), 0.84f + (0.06f * variationAmount)));
        Assert.All(geometry.Paths, path => Assert.NotNull(path.MaterialSlot));

        Assert.Contains(geometry.Paths[1].Landmarks, item => item.Name == "attachment");
        Assert.Contains(geometry.Paths[1].Landmarks, item => item.Name == "root");
        Assert.Contains(geometry.Paths[1].Landmarks, item => item.Name == "tip");

        Assert.Empty(ModelGeometryValidator.Validate(geometry));
        Assert.All(
            geometry.Paths.SelectMany(path => path.Segments),
            segment =>
            {
                Assert.True(float.IsFinite(segment.Start.X));
                Assert.True(float.IsFinite(segment.Start.Y));
                Assert.True(float.IsFinite(segment.Control1.X));
                Assert.True(float.IsFinite(segment.Control1.Y));
                Assert.True(float.IsFinite(segment.Control2.X));
                Assert.True(float.IsFinite(segment.Control2.Y));
                Assert.True(float.IsFinite(segment.End.X));
                Assert.True(float.IsFinite(segment.End.Y));
            });
    }

    [Fact]
    public void KeepsRoseEvaluationDeterministicForTheSameSeedAndVariation()
    {
        var catalog = ModelDefinitionCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Fixtures"));
        Assert.True(catalog.IsSuccess, string.Join(Environment.NewLine, catalog.Diagnostics.Select(item => item.Message)));
        var definition = Assert.Single(catalog.Models).Value;
        var context = ModelEvaluationContext.Empty(ulong.MaxValue, 1f) with
        {
            Frames = ImmutableDictionary<string, ModelMountFrameValues>.Empty.Add(
                "vase-opening",
                new(new(0.5f, 0.65f), 0.12f, 0.07f, 0.58f, 0.015f, 0.4f)),
        };

        var first = ModelDefinitionEvaluator.Evaluate(definition, context);
        var second = ModelDefinitionEvaluator.Evaluate(definition, context);

        Assert.True(first.IsSuccess, string.Join(Environment.NewLine, first.Diagnostics.Select(item => item.Message)));
        Assert.True(second.IsSuccess, string.Join(Environment.NewLine, second.Diagnostics.Select(item => item.Message)));
        var firstPaths = Assert.IsType<ModelGeometry>(first.Geometry).Paths;
        var secondPaths = Assert.IsType<ModelGeometry>(second.Geometry).Paths;
        Assert.Equal(firstPaths.Select(path => path.Id), secondPaths.Select(path => path.Id));
        Assert.Equal(firstPaths.SelectMany(path => path.Segments), secondPaths.SelectMany(path => path.Segments));
    }
}
