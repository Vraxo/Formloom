using System.Collections.Immutable;
using System.Numerics;
using Formloom.Core.Core;
using Formloom.Core.Models.Definitions;
using Formloom.Core.Models.Evaluation;
using Formloom.Core.Models.Geometry;
using Xunit;

namespace Formloom.Tests.Models;

public sealed class ModelDefinitionEvaluatorTests
{
    [Fact]
    public void EvaluatesTypedNodesIntoOrderedGenericGeometry()
    {
        var definition = CreateDefinition();
        var context = CreateContext();

        var result = ModelDefinitionEvaluator.Evaluate(definition, context);

        Assert.True(result.IsSuccess, string.Join(Environment.NewLine, result.Diagnostics.Select(item => item.Message)));
        var geometry = Assert.IsType<ModelGeometry>(result.Geometry);
        Assert.Equal(ModelGeometryCoordinateSpace.SceneNormalized, geometry.CoordinateSpace);
        Assert.Equal(6, geometry.Paths.Length);
        Assert.Equal("stem.path", geometry.Paths[0].Id);
        Assert.Equal("leaf.left", geometry.Paths[1].Id);
        Assert.Equal("bloom.outer[0]", geometry.Paths[2].Id);
        Assert.Equal("bloom.outer[3]", geometry.Paths[5].Id);
        Assert.All(geometry.Paths.Skip(2), path =>
        {
            Assert.Equal("petal", path.MaterialSlot);
            Assert.InRange(path.MaterialTone!.Value, 0.40f, 0.50f);
        });

        var leaf = geometry.Paths[1];
        Assert.Contains(leaf.Landmarks, item => item.Name == "attachment");
        Assert.Contains(leaf.Landmarks, item => item.Name == "root");
        Assert.Contains(leaf.Landmarks, item => item.Name == "tip");
        Assert.Empty(ModelGeometryValidator.Validate(geometry));
        Assert.Equal(0.35, result.NumberParameters["variation"]);
        Assert.Equal("red", result.ChoiceParameters["color"]);
    }

    [Fact]
    public void UsesParameterOverridesAndDeterministicRadialStreams()
    {
        var definition = CreateDefinition();
        var context = CreateContext() with
        {
            Seed = 12345,
            VariationAmount = 0.8f,
            NumberParameters = ImmutableDictionary<string, double>.Empty.Add("variation", 0.8),
            ChoiceParameters = ImmutableDictionary<string, string>.Empty.Add("color", "pink"),
        };

        var first = ModelDefinitionEvaluator.Evaluate(definition, context);
        var second = ModelDefinitionEvaluator.Evaluate(definition, context);

        Assert.True(first.IsSuccess, string.Join(Environment.NewLine, first.Diagnostics.Select(item => item.Message)));
        var firstPaths = Assert.IsType<ModelGeometry>(first.Geometry).Paths;
        var secondPaths = Assert.IsType<ModelGeometry>(second.Geometry).Paths;
        Assert.Equal(firstPaths.Select(path => path.Id), secondPaths.Select(path => path.Id));
        Assert.Equal(
            firstPaths.SelectMany(path => path.Segments),
            secondPaths.SelectMany(path => path.Segments));
        Assert.Equal(0.8, first.NumberParameters["variation"]);
        Assert.Equal("pink", first.ChoiceParameters["color"]);
    }

    [Fact]
    public void RejectsNumberOverridesOutsideDeclaredRange()
    {
        var context = CreateContext() with
        {
            NumberParameters = ImmutableDictionary<string, double>.Empty.Add("variation", 1.5),
        };

        var result = ModelDefinitionEvaluator.Evaluate(CreateDefinition(), context);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, item => item.Code == "evaluation.parameter-resolution-failed" ||
            item.Code == "evaluation.parameter-expression-failed");
    }

    [Fact]
    public void RejectsMissingMountFrame()
    {
        var result = ModelDefinitionEvaluator.Evaluate(CreateDefinition(), ModelEvaluationContext.Empty());

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, item => item.Code == "evaluation.expression-failed");
    }

    [Fact]
    public void RejectsInvalidChoiceOverrides()
    {
        var context = CreateContext() with
        {
            ChoiceParameters = ImmutableDictionary<string, string>.Empty.Add("color", "ultraviolet"),
        };

        var result = ModelDefinitionEvaluator.Evaluate(CreateDefinition(), context);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, item => item.Code == "evaluation.invalid-choice");
    }

    [Fact]
    public void RejectsCyclicNumericParameterDefaultsAndIncludesModelIdInDiagnostics()
    {
        var definition = CreateDefinition();
        var parameter = definition.Parameters["variation"];
        var invalidDefinition = definition with
        {
            Parameters = definition.Parameters.SetItem(
                "variation",
                parameter with { DefaultValue = Reference("parameters.variation") }),
        };

        var result = ModelDefinitionEvaluator.Evaluate(invalidDefinition, CreateContext());

        Assert.False(result.IsSuccess);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("evaluation.parameter-expression-failed", diagnostic.Code);
        Assert.Equal("flowers.evaluator-test", diagnostic.ModelId);
    }

    [Fact]
    public void RejectsDivisionByZeroInAnEvaluatedExpression()
    {
        var definition = CreateDefinition();
        var invalidNode = Assert.IsType<ModelRadialArrayNodeDefinition>(definition.Nodes[3]);
        var invalidDefinition = definition with
        {
            Nodes = definition.Nodes.SetItem(
                3,
                invalidNode with
                {
                    RadiusX = new ModelArithmeticExpression(
                        ModelArithmeticOperator.Divide,
                        [Literal(1), Literal(0)]),
                }),
        };

        var result = ModelDefinitionEvaluator.Evaluate(invalidDefinition, CreateContext());

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, item => item.Code == "evaluation.expression-failed");
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(1f)]
    public void EvaluatesFiniteGeometryAtVariationExtremes(float variationAmount)
    {
        var context = CreateContext() with { Seed = ulong.MaxValue, VariationAmount = variationAmount };

        var result = ModelDefinitionEvaluator.Evaluate(CreateDefinition(), context);

        Assert.True(result.IsSuccess, string.Join(Environment.NewLine, result.Diagnostics.Select(item => item.Message)));
        var geometry = Assert.IsType<ModelGeometry>(result.Geometry);
        foreach (var segment in geometry.Paths.SelectMany(path => path.Segments))
        {
            Assert.True(float.IsFinite(segment.Start.X));
            Assert.True(float.IsFinite(segment.Start.Y));
            Assert.True(float.IsFinite(segment.Control1.X));
            Assert.True(float.IsFinite(segment.Control1.Y));
            Assert.True(float.IsFinite(segment.Control2.X));
            Assert.True(float.IsFinite(segment.Control2.Y));
            Assert.True(float.IsFinite(segment.End.X));
            Assert.True(float.IsFinite(segment.End.Y));
        }

        Assert.All(
            geometry.Paths.Where(path => path.MaterialTone.HasValue),
            path => Assert.InRange(path.MaterialTone!.Value, 0f, 1f));
    }

    [Fact]
    public void RejectsFiniteExpressionThatExceedsFloatCoordinateRange()
    {
        var definition = CreateDefinition();
        var radial = Assert.IsType<ModelRadialArrayNodeDefinition>(definition.Nodes[3]);
        var invalidDefinition = definition with
        {
            Nodes = definition.Nodes.SetItem(
                3,
                radial with { RadiusX = Literal((double)float.MaxValue * 2) }),
        };

        var result = ModelDefinitionEvaluator.Evaluate(invalidDefinition, CreateContext());

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, item => item.Code == "evaluation.expression-out-of-range");
    }

    [Fact]
    public void RejectsFiniteMappingInputsThatOverflowGeneratedGeometry()
    {
        var definition = CreateDefinition();
        var radial = Assert.IsType<ModelRadialArrayNodeDefinition>(definition.Nodes[3]);
        var invalidDefinition = definition with
        {
            Nodes = definition.Nodes.SetItem(
                3,
                radial with
                {
                    Mapping = radial.Mapping with
                    {
                        ScaleY = Literal(100),
                        Length = Literal(float.MaxValue),
                    },
                }),
        };

        var context = CreateContext() with { VariationAmount = 0 };
        var result = ModelDefinitionEvaluator.Evaluate(invalidDefinition, context);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, item => item.Code == "geometry.segment-non-finite" ||
            item.Code == "geometry.landmark-invalid");
    }

    [Fact]
    public void RejectsMaterialToneOutsideNormalizedRange()
    {
        var definition = CreateDefinition();
        var radial = Assert.IsType<ModelRadialArrayNodeDefinition>(definition.Nodes[3]);
        var invalidDefinition = definition with
        {
            Nodes = definition.Nodes.SetItem(3, radial with { Tone = Literal(1.1) }),
        };

        var result = ModelDefinitionEvaluator.Evaluate(invalidDefinition, CreateContext());

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, item => item.Code == "evaluation.material-tone-out-of-range");
    }

    [Fact]
    public void ProjectsOntoAnEarlierProfileMappedPath()
    {
        var definition = CreateDefinition();
        var mappedStem = new ModelProfileMapNodeDefinition(
            "stem.profile",
            "stem",
            Point(Literal(0.5), Literal(0.8)),
            Literal(-Math.PI / 2),
            Mapping(0.6, 0.08, reverse: false) with { Bend = Literal(0) },
            "stem",
            false);
        var attachment = Assert.IsType<ModelCurveProjectNodeDefinition>(definition.Nodes[1]) with
        {
            Curve = "stem.profile",
            Target = Point(Literal(0.5), Literal(0.5)),
        };
        var leaf = Assert.IsType<ModelProfileMapNodeDefinition>(definition.Nodes[2]);
        var radial = Assert.IsType<ModelRadialArrayNodeDefinition>(definition.Nodes[3]);
        var group = Assert.IsType<ModelGeometryGroupNodeDefinition>(definition.Nodes[4]) with
        {
            Parts = ["stem.profile", "leaf.left", "bloom.outer"],
        };

        definition = definition with
        {
            Profiles = definition.Profiles.Add(
                "stem",
                new(
                    false,
                    ModelProfileInterpolation.OpenCubic,
                    [
                        new(0.5f, 0f),
                        new(0.5f, 1f / 3f),
                        new(0.5f, 2f / 3f),
                        new(0.5f, 1f),
                    ])),
            Nodes = [mappedStem, attachment, leaf, radial, group],
        };

        var result = ModelDefinitionEvaluator.Evaluate(definition, CreateContext());

        Assert.True(result.IsSuccess, string.Join(Environment.NewLine, result.Diagnostics.Select(item => item.Message)));
        var geometry = Assert.IsType<ModelGeometry>(result.Geometry);
        var stemPath = Assert.Single(geometry.Paths, path => path.Id == "stem.profile");
        Assert.NotEmpty(stemPath.Segments);
        var leafPath = Assert.Single(geometry.Paths, path => path.Id == "leaf.left");
        var attachmentLandmark = Assert.Single(leafPath.Landmarks, item => item.Name == "attachment");
        Assert.Equal(0.5f, attachmentLandmark.Position.X, 4);
        Assert.Equal(0.5f, attachmentLandmark.Position.Y, 4);
    }

    private static ModelDefinition CreateDefinition()
    {
        var number = new ModelParameterDefinition(
            ModelParameterType.Number,
            Literal(0.35),
            Literal(0),
            Literal(1),
            null,
            []);
        var choice = new ModelParameterDefinition(
            ModelParameterType.Choice,
            null,
            null,
            null,
            "red",
            ["red", "pink"]);
        var profiles = ImmutableDictionary<string, ModelProfileDefinition>.Empty
            .Add("petal", new(true, ModelProfileInterpolation.ClosedCubic,
            [
                new(0.5f, 0.02f),
                new(0.08f, 0.42f),
                new(0.5f, 1f),
                new(0.92f, 0.42f),
            ]))
            .Add("leaf", new(true, ModelProfileInterpolation.ClosedCubic,
            [
                new(0.5f, 1f),
                new(0.2f, 0.65f),
                new(0.5f, 0f),
                new(0.8f, 0.65f),
            ]));
        var materials = ImmutableDictionary<string, ModelMaterialVariantDefinition>.Empty
            .Add("red", new(ImmutableDictionary<string, string>.Empty.Add("petal.base", "#aa2233")))
            .Add("pink", new(ImmutableDictionary<string, string>.Empty.Add("petal.base", "#dd6699")));
        var stem = new ModelCubicPathNodeDefinition(
            "stem.path",
            [
                new(
                    Point(Literal(0.5), Literal(0.8)),
                    Point(Literal(0.52), Literal(0.6)),
                    Point(Literal(0.48), Literal(0.4)),
                    Point(Literal(0.5), Literal(0.2))),
            ],
            "stem",
            false);
        var attachment = new ModelCurveProjectNodeDefinition(
            "leaf.attachment",
            "stem.path",
            Point(Literal(0.5), Literal(0.65)));
        var leafMapping = Mapping(0.2, 0.08, reverse: true);
        var leaf = new ModelProfileMapNodeDefinition(
            "leaf.left",
            "leaf",
            Point(Reference("node:leaf.attachment:point:x"), Reference("node:leaf.attachment:point:y")),
            Literal(0),
            leafMapping,
            "leaf",
            false);
        var radial = new ModelRadialArrayNodeDefinition(
            "bloom.outer",
            "petal",
            Point(Reference("frame.vase-opening.openingCenter.x"), Reference("frame.vase-opening.openingCenter.y")),
            Reference("frame.vase-opening.radiusX"),
            Reference("frame.vase-opening.radiusY"),
            4,
            Literal(-Math.PI / 2),
            Literal(0),
            Mapping(0.8, 0.7, reverse: false),
            new(Literal(0.04), Literal(0.05), Literal(0.05), Literal(0.03), Literal(0.02)),
            Literal(0.43),
            "petals.outer",
            "petal",
            false);
        var group = new ModelGeometryGroupNodeDefinition(
            "flower",
            ["stem.path", "leaf.left", "bloom.outer"]);
        return new(
            "formloom.model",
            1,
            "flowers.evaluator-test",
            "Evaluator Test Flower",
            "flower",
            new(
                "a",
                "test flower",
                [],
                "color",
                ImmutableDictionary<string, ImmutableArray<string>>.Empty.Add("color", ["red", "pink"])),
            ["vase-opening"],
            ImmutableDictionary<string, ModelParameterDefinition>.Empty
                .Add("variation", number)
                .Add("color", choice),
            profiles,
            materials,
            [stem, attachment, leaf, radial, group],
            ["flower"]);
    }

    private static ModelEvaluationContext CreateContext() =>
        ModelEvaluationContext.Empty(42, 0.5f) with
        {
            Frames = ImmutableDictionary<string, ModelMountFrameValues>.Empty.Add(
                "vase-opening",
                new(new(0.5f, 0.65f), 0.12f, 0.07f, 0.58f, 0.015f, 0.4f)),
        };

    private static ModelProfileMappingDefinition Mapping(double length, double width, bool reverse) =>
        new(
            Literal(1),
            Literal(1),
            Literal(length),
            Literal(width),
            Literal(0),
            Literal(0.01),
            Literal(0.5),
            reverse);

    private static ModelPointExpression Point(ModelNumberExpression x, ModelNumberExpression y) => new(x, y);

    private static ModelLiteralExpression Literal(double value) => new(value);

    private static ModelReferenceExpression Reference(string path) => new(path);
}
