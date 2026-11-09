using System.Text.Json;
using Formloom.Core.Models.Definitions;
using Xunit;

namespace Formloom.Tests.Models;

public sealed class ModelDefinitionJsonTests
{
    private const string DefinitionFixture =
        """
        {
          "format": "formloom.model",
          "schemaVersion": 1,
          "id": "flowers.rose",
          "name": "Rose",
          "category": "flower",
          "prompt": {
            "article": "a",
            "noun": "rose",
            "aliases": [],
            "materialVariantParameter": "color",
            "descriptors": {
              "color": ["red", "pink"]
            }
          },
          "mounts": ["vase-opening"],
          "parameters": {
            "variation": {
              "type": "number",
              "defaultValue": { "kind": "literal", "value": 0.35 },
              "minimum": { "kind": "literal", "value": 0 },
              "maximum": { "kind": "literal", "value": 1 },
              "values": []
            },
            "color": {
              "type": "choice",
              "defaultChoice": "red",
              "values": ["red", "pink"]
            }
          },
          "profiles": {
            "petal.broad": {
              "closed": true,
              "interpolation": "closedCubic",
              "points": [
                { "x": 0.50, "y": 0.02 },
                { "x": 0.15, "y": 0.14 },
                { "x": 0.03, "y": 0.39 }
              ]
            }
          },
          "materials": {
            "red": {
              "tokens": {
                "petal.base": "#B02232",
                "petal.light": "#ED5D67"
              }
            },
            "pink": {
              "tokens": {
                "petal.base": "#D03A74",
                "petal.light": "#FF91BE"
              }
            }
          },
          "nodes": [
            {
              "op": "path.cubic",
              "id": "stem.path",
              "segments": [
                {
                  "start": {
                    "x": { "kind": "literal", "value": 0.5 },
                    "y": { "kind": "literal", "value": 0.8 }
                  },
                  "control1": {
                    "x": { "kind": "literal", "value": 0.52 },
                    "y": { "kind": "literal", "value": 0.6 }
                  },
                  "control2": {
                    "x": { "kind": "literal", "value": 0.48 },
                    "y": { "kind": "literal", "value": 0.4 }
                  },
                  "end": {
                    "x": { "kind": "literal", "value": 0.5 },
                    "y": { "kind": "literal", "value": 0.2 }
                  }
                }
              ],
              "materialSlot": "stem",
              "detail": false
            },
            {
              "op": "curve.project",
              "id": "leaf.left.attachment",
              "curve": "stem.path",
              "target": {
                "x": { "kind": "literal", "value": 0.5 },
                "y": { "kind": "literal", "value": 0.65 }
              }
            },
            {
              "op": "profile.map",
              "id": "leaf.left",
              "profile": "leaf.serrated",
              "origin": {
                "x": { "kind": "reference", "path": "leaf.left.attachment.x" },
                "y": { "kind": "reference", "path": "leaf.left.attachment.y" }
              },
              "angle": { "kind": "literal", "value": -1.43 },
              "mapping": {
                "scaleX": { "kind": "literal", "value": 1 },
                "scaleY": { "kind": "literal", "value": 1 },
                "length": { "kind": "literal", "value": 0.2 },
                "width": { "kind": "literal", "value": 0.07 },
                "rootOffset": { "kind": "literal", "value": 0 },
                "bend": { "kind": "literal", "value": 0.01 },
                "transverseNormalization": { "kind": "literal", "value": 0.47 },
                "reverseLongitudinal": true
              },
              "materialSlot": "leaf",
              "detail": false
            },
            {
              "op": "array.radial",
              "id": "bloom.outer",
              "profile": "petal.broad",
              "center": {
                "x": { "kind": "literal", "value": 0.5 },
                "y": { "kind": "literal", "value": 0.25 }
              },
              "radiusX": {
                "kind": "arithmetic",
                "operator": "minimum",
                "operands": [
                  {
                    "kind": "arithmetic",
                    "operator": "multiply",
                    "operands": [
                      { "kind": "reference", "path": "frame.vase-opening.radiusX" },
                      { "kind": "literal", "value": 1.14 }
                    ]
                  },
                  {
                    "kind": "arithmetic",
                    "operator": "multiply",
                    "operands": [
                      { "kind": "reference", "path": "frame.vase-opening.headroom" },
                      { "kind": "literal", "value": 0.35 }
                    ]
                  }
                ]
              },
              "radiusY": { "kind": "literal", "value": 0.08 },
              "count": 5,
              "startAngle": { "kind": "literal", "value": -1.5707963 },
              "phase": { "kind": "literal", "value": 0 },
              "mapping": {
                "scaleX": { "kind": "literal", "value": 1 },
                "scaleY": { "kind": "literal", "value": 1 },
                "length": { "kind": "literal", "value": 0.94 },
                "width": { "kind": "literal", "value": 1.04 },
                "rootOffset": { "kind": "literal", "value": 0.015 },
                "bend": { "kind": "literal", "value": 0.025 },
                "transverseNormalization": { "kind": "literal", "value": 0.5 },
                "reverseLongitudinal": false
              },
              "variation": {
                "angle": { "kind": "literal", "value": 0.055 },
                "length": { "kind": "literal", "value": 0.035 },
                "width": { "kind": "literal", "value": 0.05 },
                "bend": { "kind": "literal", "value": 0.055 },
                "tone": { "kind": "literal", "value": 0.08 }
              },
              "stream": "petals.outer",
              "materialSlot": "petal",
              "detail": false
            },
            {
              "op": "geometry.group",
              "id": "rose",
              "parts": ["stem.path", "leaf.left", "bloom.outer"]
            }
          ],
          "outputs": ["rose"]
        }
        """;

    [Fact]
    public void DeserializesModelAndEverySupportedOperationToTypedRecords()
    {
        var model = Deserialize(DefinitionFixture);

        Assert.Equal("flowers.rose", model.Id);
        Assert.Equal(1, model.SchemaVersion);
        Assert.Equal("rose", model.Prompt.Noun);
        Assert.Equal("color", model.Prompt.MaterialVariantParameter);
        Assert.Equal(ModelParameterType.Choice, model.Parameters["color"].Type);
        Assert.Equal(ModelProfileInterpolation.ClosedCubic, model.Profiles["petal.broad"].Interpolation);
        Assert.Equal(new[] { "red", "pink" }, model.Prompt.Descriptors["color"].ToArray());

        Assert.Collection(
            model.Nodes,
            node => Assert.IsType<ModelCubicPathNodeDefinition>(node),
            node => Assert.IsType<ModelCurveProjectNodeDefinition>(node),
            node => Assert.IsType<ModelProfileMapNodeDefinition>(node),
            node => Assert.IsType<ModelRadialArrayNodeDefinition>(node),
            node => Assert.IsType<ModelGeometryGroupNodeDefinition>(node));

        var radial = Assert.IsType<ModelRadialArrayNodeDefinition>(model.Nodes[3]);
        var radius = Assert.IsType<ModelArithmeticExpression>(radial.RadiusX);
        Assert.Equal(ModelArithmeticOperator.Minimum, radius.Operator);
        Assert.Equal(2, radius.Operands.Length);
    }

    [Fact]
    public void SerializationRoundTripPreservesOperationDiscriminatorsAndExpressions()
    {
        var original = Deserialize(DefinitionFixture);
        var serialized = JsonSerializer.Serialize(original, ModelDefinitionJson.CreateOptions());
        var roundTripped = Deserialize(serialized);

        Assert.Equal(original.Id, roundTripped.Id);
        Assert.Equal(original.SchemaVersion, roundTripped.SchemaVersion);
        Assert.Equal(original.Nodes.Length, roundTripped.Nodes.Length);
        Assert.Equal(original.Outputs.ToArray(), roundTripped.Outputs.ToArray());
        Assert.IsType<ModelRadialArrayNodeDefinition>(roundTripped.Nodes[3]);
        Assert.IsType<ModelReferenceExpression>(
            Assert.IsType<ModelPointExpression>(
                Assert.IsType<ModelProfileMapNodeDefinition>(roundTripped.Nodes[2]).Origin).X);
    }

    [Fact]
    public void SerializerOmitsNumericFieldsFromChoiceParameters()
    {
        var model = Deserialize(DefinitionFixture);
        var serialized = JsonSerializer.Serialize(model, ModelDefinitionJson.CreateOptions());
        using var document = JsonDocument.Parse(serialized);
        var parameters = document.RootElement.GetProperty("parameters");
        var choice = parameters.GetProperty("color");
        var number = parameters.GetProperty("variation");

        Assert.False(choice.TryGetProperty("defaultValue", out _));
        Assert.False(choice.TryGetProperty("minimum", out _));
        Assert.False(choice.TryGetProperty("maximum", out _));
        Assert.False(number.TryGetProperty("defaultChoice", out _));
    }

    [Fact]
    public void UnknownOperationDiscriminatorIsRejected()
    {
        var unsupported = DefinitionFixture.Replace(
            "\"op\": \"array.radial\"",
            "\"op\": \"flowers.rose\"",
            StringComparison.Ordinal);

        Assert.Throws<JsonException>(() => Deserialize(unsupported));
    }

    [Fact]
    public void UnknownPropertiesAreRejected()
    {
        var unsupported = DefinitionFixture.Replace(
            "\"name\": \"Rose\"",
            "\"name\": \"Rose\", \"unknown\": true",
            StringComparison.Ordinal);

        Assert.Throws<JsonException>(() => Deserialize(unsupported));
    }

    private static ModelDefinition Deserialize(string json) =>
        JsonSerializer.Deserialize<ModelDefinition>(json, ModelDefinitionJson.CreateOptions())
        ?? throw new JsonException("The model definition was empty.");
}