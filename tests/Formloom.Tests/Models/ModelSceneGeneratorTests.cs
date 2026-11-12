using Formloom.Core.Ceramics;
using Formloom.Core.Models.Generation;
using Formloom.Core.Scenes;
using Xunit;

namespace Formloom.Tests.Models;

public sealed class ModelSceneGeneratorTests
{
    [Theory]
    [InlineData("a red rose in a blue ceramic vase on a wooden table", "red", "Blue", "model-flowers-rose-probe-in-blue-vase")]
    [InlineData("a pink rose in a cream ceramic vase on a wooden table", "pink", "Cream", "model-flowers-rose-probe-in-cream-vase")]
    public void ResolvesCatalogDescriptorsAndEvaluatesAgainstTheGeneratedVase(
        string prompt,
        string color,
        string glaze,
        string subjectToken)
    {
        var generated = ModelSceneGenerator.Generate(
            new GenerationRequest(prompt, Seed: 42, Variation: 0.35, Width: 256, Height: 256),
            Path.Combine(AppContext.BaseDirectory, "Fixtures"));

        Assert.True(generated.IsModelPrompt, "The model prompt should be claimed by the catalog.");
        Assert.True(generated.IsSuccess, generated.Error);
        Assert.Equal("flowers.rose-probe", generated.Geometry!.ModelId);
        Assert.Equal(subjectToken, generated.SubjectToken);
        var vase = Assert.Single(generated.HostScene!.Ceramics);
        Assert.Equal(CeramicKind.Vase, vase.Kind);
        Assert.Equal(glaze, vase.Glaze.ToString());
        Assert.Equal(43, generated.Geometry.Paths.Length);
        Assert.All(generated.Geometry.Paths, path => Assert.NotEmpty(path.Segments));
        Assert.Equal(color == "red" ? "#B02232" : "#D03A74", generated.Geometry.MaterialTokens["petal.base"]);
    }

    [Fact]
    public void CatalogModelEvaluationIsDeterministicForTheSameRequest()
    {
        var request = new GenerationRequest(
            "a red rose in a blue ceramic vase on a wooden table",
            Seed: 913,
            Variation: 0.7,
            Width: 256,
            Height: 256);
        var modelRoot = Path.Combine(AppContext.BaseDirectory, "Fixtures");

        var first = ModelSceneGenerator.Generate(request, modelRoot);
        var second = ModelSceneGenerator.Generate(request, modelRoot);

        Assert.True(first.IsSuccess, first.Error);
        Assert.True(second.IsSuccess, second.Error);
        Assert.Equal(first.Geometry!.Paths, second.Geometry!.Paths);
        Assert.Equal(first.Geometry.MaterialTokens, second.Geometry.MaterialTokens);
        Assert.Equal(first.SubjectToken, second.SubjectToken);
    }

    [Fact]
    public void MatchedModelPromptDoesNotSilentlyFallBackForAnUnsupportedHost()
    {
        var generated = ModelSceneGenerator.Generate(
            new GenerationRequest("a red rose in a blue ceramic cup on a wooden table"),
            Path.Combine(AppContext.BaseDirectory, "Fixtures"));

        Assert.True(generated.IsModelPrompt);
        Assert.False(generated.IsSuccess);
        Assert.Contains("exactly one blue or cream ceramic vase", generated.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void UnrelatedPromptsAreLeftForTheExistingSceneGrammar()
    {
        var generated = ModelSceneGenerator.Generate(
            new GenerationRequest("a red apple on a wooden table"),
            Path.Combine(AppContext.BaseDirectory, "Fixtures"));

        Assert.False(generated.IsModelPrompt);
        Assert.Null(generated.Error);
    }
}
