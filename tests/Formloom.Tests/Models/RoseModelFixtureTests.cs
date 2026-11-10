using System.Collections.Immutable;
using Formloom.Core.Models.Catalog;
using Formloom.Core.Models.Definitions;
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
        Assert.Equal(31, geometry.Paths.Length);
        Assert.Equal("rose.stem", geometry.Paths[0].Id);

        ModelGeometryPath FindPath(string id) =>
            Assert.Single(geometry.Paths.Where(path => path.Id == id));

        ModelGeometryPath[] FindRadial(string idPrefix) =>
            geometry.Paths
                .Where(path => path.Id.StartsWith(idPrefix + "[", StringComparison.Ordinal))
                .ToArray();

        var leftLeaf = FindPath("rose.leaf.left");
        var rightLeaf = FindPath("rose.leaf.right");
        var leftVein = FindPath("rose.leaf.left.vein");
        var rightVein = FindPath("rose.leaf.right.vein");

        Assert.True(leftLeaf.Closed);
        Assert.True(rightLeaf.Closed);
        Assert.False(leftVein.Closed);
        Assert.False(rightVein.Closed);
        Assert.True(leftVein.Detail);
        Assert.True(rightVein.Detail);
        Assert.Equal("leaf.vein", leftVein.MaterialSlot);
        Assert.Equal("leaf.vein", rightVein.MaterialSlot);
        Assert.Equal(3, leftVein.Segments.Length);
        Assert.Equal(3, rightVein.Segments.Length);
        Assert.Equal(
            leftLeaf.Landmarks.Select(item => item.Position),
            leftVein.Landmarks.Select(item => item.Position));
        Assert.Equal(
            rightLeaf.Landmarks.Select(item => item.Position),
            rightVein.Landmarks.Select(item => item.Position));

        var outer = FindRadial("rose.petals.outer");
        var middle = FindRadial("rose.petals.middle");
        var inner = FindRadial("rose.petals.inner");
        var outerFolds = FindRadial("rose.petals.outer.fold");
        var middleFolds = FindRadial("rose.petals.middle.fold");
        var innerFolds = FindRadial("rose.petals.inner.fold");

        Assert.Equal(5, outer.Length);
        Assert.Equal(5, middle.Length);
        Assert.Equal(3, inner.Length);
        Assert.Equal(5, outerFolds.Length);
        Assert.Equal(5, middleFolds.Length);
        Assert.Equal(3, innerFolds.Length);

        Assert.All(outer, path => Assert.InRange(path.MaterialTone!.Value, 0.43f - (0.08f * variationAmount), 0.43f + (0.08f * variationAmount)));
        Assert.All(middle, path => Assert.InRange(path.MaterialTone!.Value, 0.64f - (0.08f * variationAmount), 0.64f + (0.08f * variationAmount)));
        Assert.All(inner, path => Assert.InRange(path.MaterialTone!.Value, 0.84f - (0.06f * variationAmount), 0.84f + (0.06f * variationAmount)));

        Assert.All(outer, path => Assert.True(path.Closed));
        Assert.All(middle, path => Assert.True(path.Closed));
        Assert.All(inner, path => Assert.True(path.Closed));
        Assert.All(outerFolds, path => Assert.False(path.Closed));
        Assert.All(middleFolds, path => Assert.False(path.Closed));
        Assert.All(innerFolds, path => Assert.False(path.Closed));

        Assert.All(outerFolds.Concat(middleFolds).Concat(innerFolds), path =>
        {
            Assert.True(path.Detail);
            Assert.Equal("petal.fold", path.MaterialSlot);
            Assert.Equal(3, path.Segments.Length);
        });

        for (var index = 0; index < outer.Length; index++)
        {
            Assert.Equal(
                outer[index].Landmarks.Select(item => item.Position),
                outerFolds[index].Landmarks.Select(item => item.Position));
        }

        for (var index = 0; index < middle.Length; index++)
        {
            Assert.Equal(
                middle[index].Landmarks.Select(item => item.Position),
                middleFolds[index].Landmarks.Select(item => item.Position));
        }

        for (var index = 0; index < inner.Length; index++)
        {
            Assert.Equal(
                inner[index].Landmarks.Select(item => item.Position),
                innerFolds[index].Landmarks.Select(item => item.Position));
        }

        Assert.All(geometry.Paths, path => Assert.NotNull(path.MaterialSlot));
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
    public void VariesMappedLeavesWithIndependentStreamsAndKeepsVeinsAligned()
    {
        var catalog = ModelDefinitionCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Fixtures"));
        Assert.True(catalog.IsSuccess, string.Join(Environment.NewLine, catalog.Diagnostics.Select(item => item.Message)));
        var definition = Assert.Single(catalog.Models).Value;
        var frames = ImmutableDictionary<string, ModelMountFrameValues>.Empty.Add(
            "vase-opening",
            new(new(0.5f, 0.65f), 0.12f, 0.07f, 0.58f, 0.015f, 0.4f));

        var baseline = ModelDefinitionEvaluator.Evaluate(
            definition,
            ModelEvaluationContext.Empty(42, 0f) with { Frames = frames });
        var varied = ModelDefinitionEvaluator.Evaluate(
            definition,
            ModelEvaluationContext.Empty(42, 1f) with { Frames = frames });

        Assert.True(baseline.IsSuccess, string.Join(Environment.NewLine, baseline.Diagnostics.Select(item => item.Message)));
        Assert.True(varied.IsSuccess, string.Join(Environment.NewLine, varied.Diagnostics.Select(item => item.Message)));
        var baselinePaths = Assert.IsType<ModelGeometry>(baseline.Geometry).Paths;
        var variedPaths = Assert.IsType<ModelGeometry>(varied.Geometry).Paths;

        ModelGeometryPath Find(ImmutableArray<ModelGeometryPath> paths, string id) =>
            Assert.Single(paths.Where(path => path.Id == id));

        var baselineLeft = Find(baselinePaths, "rose.leaf.left");
        var variedLeft = Find(variedPaths, "rose.leaf.left");
        var variedLeftVein = Find(variedPaths, "rose.leaf.left.vein");
        var baselineRight = Find(baselinePaths, "rose.leaf.right");
        var variedRight = Find(variedPaths, "rose.leaf.right");

        Assert.NotEqual(baselineLeft.Segments[0], variedLeft.Segments[0]);
        Assert.NotEqual(baselineRight.Segments[0], variedRight.Segments[0]);
        Assert.Equal(
            variedLeft.Landmarks.Select(item => item.Position),
            variedLeftVein.Landmarks.Select(item => item.Position));

        var rightLeafIndex = Enumerable.Range(0, definition.Nodes.Length)
            .Single(index => definition.Nodes[index].Id == "rose.leaf.right");
        var rightVeinIndex = Enumerable.Range(0, definition.Nodes.Length)
            .Single(index => definition.Nodes[index].Id == "rose.leaf.right.vein");
        var rightLeafNode = Assert.IsType<ModelProfileMapNodeDefinition>(definition.Nodes[rightLeafIndex]);
        var rightVeinNode = Assert.IsType<ModelProfileMapNodeDefinition>(definition.Nodes[rightVeinIndex]);
        const string alternateStream = "rose.leaves.right.alternate";
        var changedDefinition = definition with
        {
            Nodes = definition.Nodes
                .SetItem(rightLeafIndex, rightLeafNode with { Stream = alternateStream })
                .SetItem(rightVeinIndex, rightVeinNode with { Stream = alternateStream }),
        };

        var changed = ModelDefinitionEvaluator.Evaluate(
            changedDefinition,
            ModelEvaluationContext.Empty(42, 1f) with { Frames = frames });

        Assert.True(changed.IsSuccess, string.Join(Environment.NewLine, changed.Diagnostics.Select(item => item.Message)));
        var changedPaths = Assert.IsType<ModelGeometry>(changed.Geometry).Paths;
        Assert.Equal(variedLeft.Segments, Find(changedPaths, "rose.leaf.left").Segments);
        Assert.NotEqual(variedRight.Segments[0], Find(changedPaths, "rose.leaf.right").Segments[0]);
    }

    [Fact]
    public void RequiresAStableStreamWhenProfileVariationIsDeclared()
    {
        var catalog = ModelDefinitionCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Fixtures"));
        Assert.True(catalog.IsSuccess, string.Join(Environment.NewLine, catalog.Diagnostics.Select(item => item.Message)));
        var definition = Assert.Single(catalog.Models).Value;
        var nodeIndex = Enumerable.Range(0, definition.Nodes.Length)
            .Single(index => definition.Nodes[index].Id == "rose.leaf.left");
        var node = Assert.IsType<ModelProfileMapNodeDefinition>(definition.Nodes[nodeIndex]);
        var invalidDefinition = definition with
        {
            Nodes = definition.Nodes.SetItem(nodeIndex, node with { Stream = null }),
        };

        var diagnostics = ModelDefinitionValidator.Validate(invalidDefinition, "<test>");

        Assert.Contains(diagnostics, item => item.Code == "definition.profile-variation-stream-required");
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
