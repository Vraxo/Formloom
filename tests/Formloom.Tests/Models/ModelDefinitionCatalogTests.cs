using Formloom.Core.Models.Catalog;
using Xunit;

namespace Formloom.Tests.Models;

public sealed class ModelDefinitionCatalogTests
{
    private const string ValidDefinition =
        """
        {
          "format": "formloom.model",
          "schemaVersion": 1,
          "id": "flowers.test-flower",
          "name": "Test Flower",
          "category": "flower",
          "prompt": {
            "article": "a",
            "noun": "test flower",
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
                { "x": 0.5, "y": 0.02 },
                { "x": 0.1, "y": 0.6 },
                { "x": 0.5, "y": 1.0 }
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
              "op": "array.radial",
              "id": "bloom.outer",
              "profile": "petal.broad",
              "center": {
                "x": { "kind": "reference", "path": "frame.vase-opening.openingCenter.x" },
                "y": { "kind": "reference", "path": "frame.vase-opening.openingCenter.y" }
              },
              "radiusX": { "kind": "reference", "path": "frame.vase-opening.radiusX" },
              "radiusY": { "kind": "reference", "path": "frame.vase-opening.radiusY" },
              "count": 5,
              "startAngle": { "kind": "literal", "value": -1.5707963 },
              "phase": { "kind": "literal", "value": 0 },
              "tone": { "kind": "literal", "value": 0.43 },
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
              "detail": false
            }
          ],
          "outputs": ["bloom.outer"]
        }
        """;

    [Fact]
    public void DiscoversValidExternalDefinitionRecursively()
    {
        InDirectory(root =>
        {
            var directory = Directory.CreateDirectory(Path.Combine(root, "flowers"));
            File.WriteAllText(Path.Combine(directory.FullName, "test.model.json"), ValidDefinition);
            File.WriteAllText(Path.Combine(directory.FullName, "notes.json"), "{ not a model }");

            var result = ModelDefinitionCatalog.Load(root);

            Assert.True(result.IsSuccess);
            var model = Assert.Single(result.Models);
            Assert.Equal("flowers.test-flower", model.Key);
            Assert.Equal("Test Flower", model.Value.Name);
            Assert.Empty(result.Diagnostics);
        });
    }

    [Fact]
    public void MissingDirectoryReturnsDiagnosticInsteadOfThrowing()
    {
        InDirectory(root =>
        {
            var missing = Path.Combine(root, "missing");
            var result = ModelDefinitionCatalog.Load(missing);

            Assert.Empty(result.Models);
            Assert.Contains(result.Diagnostics, item => item.Code == "catalog.root-not-found");
        });
    }

    [Fact]
    public void EmptyRootPathReturnsDiagnostic()
    {
        var result = ModelDefinitionCatalog.Load(" ");

        Assert.Empty(result.Models);
        Assert.Contains(result.Diagnostics, item => item.Code == "catalog.root-required");
    }

    [Fact]
    public void MalformedJsonProducesPathAwareDiagnosticAndDoesNotStopOtherModels()
    {
        InDirectory(root =>
        {
            File.WriteAllText(Path.Combine(root, "a.model.json"), "{ malformed");
            File.WriteAllText(Path.Combine(root, "b.model.json"), ValidDefinition);

            var result = ModelDefinitionCatalog.Load(root);

            Assert.Single(result.Models);
            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal("catalog.invalid-json", diagnostic.Code);
            Assert.Equal("a.model.json", diagnostic.FilePath);
        });
    }

    [Fact]
    public void UnsupportedSchemaVersionIsRejectedBeforeCatalogInsertion()
    {
        InDirectory(root =>
        {
            var unsupported = ValidDefinition.Replace(
                "\"schemaVersion\": 1",
                "\"schemaVersion\": 2",
                StringComparison.Ordinal);
            File.WriteAllText(Path.Combine(root, "test.model.json"), unsupported);

            var result = ModelDefinitionCatalog.Load(root);

            Assert.Empty(result.Models);
            Assert.Contains(result.Diagnostics, item => item.Code == "definition.unsupported-schema-version");
        });
    }

    [Fact]
    public void DuplicateModelIdsExcludeEveryConflictingDefinition()
    {
        InDirectory(root =>
        {
            File.WriteAllText(Path.Combine(root, "a.model.json"), ValidDefinition);
            File.WriteAllText(Path.Combine(root, "b.model.json"), ValidDefinition);

            var result = ModelDefinitionCatalog.Load(root);

            Assert.Empty(result.Models);
            Assert.Contains(result.Diagnostics, item => item.Code == "catalog.duplicate-model-id");
        });
    }

    [Fact]
    public void ConflictingPromptPhrasesExcludeEveryConflictingModel()
    {
        InDirectory(root =>
        {
            var other = ValidDefinition
                .Replace(
                    "\"id\": \"flowers.test-flower\"",
                    "\"id\": \"flowers.other-flower\"",
                    StringComparison.Ordinal)
                .Replace(
                    "\"noun\": \"test flower\"",
                    "\"noun\": \"Test   Flower\"",
                    StringComparison.Ordinal);
            File.WriteAllText(Path.Combine(root, "a.model.json"), ValidDefinition);
            File.WriteAllText(Path.Combine(root, "b.model.json"), other);

            var result = ModelDefinitionCatalog.Load(root);

            Assert.Empty(result.Models);
            Assert.Equal(2, result.Diagnostics.Count(item => item.Code == "catalog.duplicate-prompt-phrase"));
        });
    }

    [Fact]
    public void DuplicateNounAliasPhraseRejectsItsDefinition()
    {
        InDirectory(root =>
        {
            var invalid = ValidDefinition.Replace(
                "\"aliases\": []",
                "\"aliases\": [\"test flower\"]",
                StringComparison.Ordinal);
            File.WriteAllText(Path.Combine(root, "test.model.json"), invalid);

            var result = ModelDefinitionCatalog.Load(root);

            Assert.Empty(result.Models);
            Assert.Contains(result.Diagnostics, item => item.Code == "definition.duplicate-prompt-phrase");
        });
    }

    [Fact]
    public void InvalidPromptDefinitionDoesNotBlockAnotherModel()
    {
        InDirectory(root =>
        {
            var invalid = ValidDefinition.Replace(
                "\"aliases\": []",
                "\"aliases\": [\"test flower\"]",
                StringComparison.Ordinal);
            var valid = ValidDefinition.Replace(
                "\"id\": \"flowers.test-flower\"",
                "\"id\": \"flowers.other-flower\"",
                StringComparison.Ordinal);
            File.WriteAllText(Path.Combine(root, "a-invalid.model.json"), invalid);
            File.WriteAllText(Path.Combine(root, "b-valid.model.json"), valid);

            var result = ModelDefinitionCatalog.Load(root);

            Assert.Contains("flowers.other-flower", result.Models.Keys);
            Assert.DoesNotContain("flowers.test-flower", result.Models.Keys);
            Assert.Contains(result.Diagnostics, item => item.Code == "definition.duplicate-prompt-phrase");
        });
    }

    [Fact]
    public void MissingProfileReferenceRejectsDefinition()
    {
        InDirectory(root =>
        {
            var invalid = ValidDefinition.Replace(
                "\"profile\": \"petal.broad\"",
                "\"profile\": \"petal.missing\"",
                StringComparison.Ordinal);
            File.WriteAllText(Path.Combine(root, "test.model.json"), invalid);

            var result = ModelDefinitionCatalog.Load(root);

            Assert.Empty(result.Models);
            Assert.Contains(result.Diagnostics, item => item.Code == "definition.profile-reference-not-found");
        });
    }

    [Fact]
    public void UnknownNumericReferenceRejectsDefinition()
    {
        InDirectory(root =>
        {
            var invalid = ValidDefinition.Replace(
                "frame.vase-opening.radiusX",
                "parameters.missing",
                StringComparison.Ordinal);
            File.WriteAllText(Path.Combine(root, "test.model.json"), invalid);

            var result = ModelDefinitionCatalog.Load(root);

            Assert.Empty(result.Models);
            Assert.Contains(result.Diagnostics, item => item.Code == "definition.parameter-reference-not-found");
        });
    }

    [Fact]
    public void ExcessiveFileSizeRejectsDefinitionBeforeDeserialization()
    {
        InDirectory(root =>
        {
            File.WriteAllText(Path.Combine(root, "test.model.json"), ValidDefinition);
            var result = ModelDefinitionCatalog.Load(root, new ModelDefinitionCatalogOptions
            {
                MaximumFileBytes = 32,
            });

            Assert.Empty(result.Models);
            Assert.Contains(result.Diagnostics, item => item.Code == "catalog.file-too-large");
        });
    }

    [Fact]
    public void ExcessiveFileCountRejectsDirectoryRatherThanLoadingPartialCatalog()
    {
        InDirectory(root =>
        {
            File.WriteAllText(Path.Combine(root, "a.model.json"), ValidDefinition);
            File.WriteAllText(Path.Combine(root, "b.model.json"), ValidDefinition);
            var result = ModelDefinitionCatalog.Load(root, new ModelDefinitionCatalogOptions
            {
                MaximumFiles = 1,
            });

            Assert.Empty(result.Models);
            Assert.Contains(result.Diagnostics, item => item.Code == "catalog.file-limit-exceeded");
        });
    }

    [Fact]
    public void NodeReferencesMustResolveToEarlierNumericLandmarks()
    {
        InDirectory(root =>
        {
            var invalid = ValidDefinition.Replace(
                "frame.vase-opening.openingCenter.x",
                "node:missing-node:point:x",
                StringComparison.Ordinal);
            File.WriteAllText(Path.Combine(root, "test.model.json"), invalid);

            var result = ModelDefinitionCatalog.Load(root);

            Assert.Empty(result.Models);
            Assert.Contains(result.Diagnostics, item => item.Code == "definition.node-reference-not-found");
        });
    }

    [Fact]
    public void ForwardGroupReferencesAreRejected()
    {
        InDirectory(root =>
        {
            var withEarlyGroup = ValidDefinition.Replace(
                "  \"nodes\": [\n    {",
                "  \"nodes\": [\n    {\n      \"op\": \"geometry.group\",\n      \"id\": \"early.group\",\n      \"parts\": [\"later.group\"]\n    },\n    {",
                StringComparison.Ordinal);
            var invalid = withEarlyGroup.Replace(
                "    }\n  ],\n  \"outputs\": [\"bloom.outer\"]",
                "    },\n    {\n      \"op\": \"geometry.group\",\n      \"id\": \"later.group\",\n      \"parts\": [\"bloom.outer\"]\n    }\n  ],\n  \"outputs\": [\"later.group\"]",
                StringComparison.Ordinal);
            Assert.NotEqual(ValidDefinition, invalid);
            File.WriteAllText(Path.Combine(root, "test.model.json"), invalid);

            var result = ModelDefinitionCatalog.Load(root);

            Assert.Empty(result.Models);
            Assert.Contains(result.Diagnostics, item => item.Code == "definition.group-reference-not-found");
        });
    }

    [Fact]
    public void TotalRepeatedElementsAreBoundedAcrossAllNodes()
    {
        InDirectory(root =>
        {
            File.WriteAllText(Path.Combine(root, "test.model.json"), ValidDefinition);
            var result = ModelDefinitionCatalog.Load(root, new ModelDefinitionCatalogOptions
            {
                MaximumTotalRepeatCount = 4,
            });

            Assert.Empty(result.Models);
            Assert.Contains(result.Diagnostics, item => item.Code == "definition.total-repeat-limit-exceeded");
        });
    }

    [Fact]
    public void ExcessiveOperationDependencyDepthRejectsDefinition()
    {
        InDirectory(root =>
        {
            const int groupCount = 64;
            var groups = Enumerable.Range(0, groupCount)
                .Select(index =>
                {
                    var id = $"group.{index}";
                    var part = index == 0 ? "bloom.outer" : $"group.{index - 1}";
                    return $$"""{"op":"geometry.group","id":"{{id}}","parts":["{{part}}"]}""";
                });
            var expanded = ValidDefinition.Replace(
                "    }\n  ],\n  \"outputs\": [\"bloom.outer\"]",
                "    },\n" + string.Join(",\n", groups) +
                "\n  ],\n  \"outputs\": [\"group.63\"]",
                StringComparison.Ordinal);
            Assert.NotEqual(ValidDefinition, expanded);
            File.WriteAllText(Path.Combine(root, "test.model.json"), expanded);

            var result = ModelDefinitionCatalog.Load(root);

            Assert.Empty(result.Models);
            Assert.Contains(result.Diagnostics, item => item.Code == "definition.graph-depth-limit");
        });
    }

    [Fact]
    public void CatalogCanDiscoverFilesAddedBetweenLoads()
    {
        InDirectory(root =>
        {
            var initial = ModelDefinitionCatalog.Load(root);
            Assert.Empty(initial.Models);

            File.WriteAllText(Path.Combine(root, "new.model.json"), ValidDefinition);
            var reloaded = ModelDefinitionCatalog.Load(root);

            Assert.Contains("flowers.test-flower", reloaded.Models.Keys);
        });
    }

    [Fact]
    public void InvalidMaterialColorTokenRejectsDefinition()
    {
        InDirectory(root =>
        {
            var invalid = ValidDefinition.Replace("#B02232", "red", StringComparison.Ordinal);
            File.WriteAllText(Path.Combine(root, "test.model.json"), invalid);

            var result = ModelDefinitionCatalog.Load(root);

            Assert.Empty(result.Models);
            Assert.Contains(result.Diagnostics, item => item.Code == "definition.invalid-material-token");
        });
    }

    [Fact]
    public void NonPositiveTotalRepeatLimitReturnsDiagnostic()
    {
        InDirectory(root =>
        {
            var result = ModelDefinitionCatalog.Load(root, new ModelDefinitionCatalogOptions
            {
                MaximumTotalRepeatCount = 0,
            });

            Assert.Empty(result.Models);
            Assert.Contains(result.Diagnostics, item => item.Code == "catalog.invalid-options");
        });
    }

    [Fact]
    public void InvalidCatalogLimitsReturnDiagnostic()
    {
        InDirectory(root =>
        {
            var result = ModelDefinitionCatalog.Load(root, new ModelDefinitionCatalogOptions
            {
                MaximumExpressionDepth = 0,
            });

            Assert.Empty(result.Models);
            Assert.Contains(result.Diagnostics, item => item.Code == "catalog.invalid-options");
        });
    }

    [Fact]
    public void DefaultRootIsAdjacentToTheApplication()
    {
        Assert.Equal(
            Path.Combine(AppContext.BaseDirectory, "models"),
            ModelDefinitionCatalog.DefaultRootPath);
    }

    private static void InDirectory(Action<string> action)
    {
        var path = Path.Combine(Path.GetTempPath(), "FormloomModelCatalogTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        try
        {
            action(path);
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
