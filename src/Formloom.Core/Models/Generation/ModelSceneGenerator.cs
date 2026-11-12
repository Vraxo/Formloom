using System.Collections.Immutable;
using System.Numerics;
using Formloom.Core.Ceramics;
using Formloom.Core.Models.Catalog;
using Formloom.Core.Models.Definitions;
using Formloom.Core.Models.Evaluation;
using Formloom.Core.Models.Geometry;
using Formloom.Core.Scenes;

namespace Formloom.Core.Models.Generation;

/// <summary>Resolves catalog-authored model prompts and evaluates their mounted geometry.</summary>
public static class ModelSceneGenerator
{
    private const string VaseOpeningMount = "vase-opening";
    private const float TopMargin = 0.035f;

    /// <summary>
    /// Attempts to generate a model-first scene from the runtime model catalog.
    /// A prompt that does not begin with a catalog model descriptor is left to the legacy
    /// scene grammar. A matched model prompt is never silently downgraded on failure.
    /// </summary>
    public static ModelSceneGenerationResult Generate(GenerationRequest request, string? modelRoot = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var catalog = modelRoot is null
            ? ModelDefinitionCatalog.LoadDefault()
            : ModelDefinitionCatalog.Load(modelRoot);
        if (catalog.Models.Count == 0)
        {
            return ModelSceneGenerationResult.NotMatched;
        }

        var normalized = Normalize(request.Prompt);
        if (normalized is null || !TryResolvePrompt(normalized, catalog.Models.Values, out var definition, out var variant, out var hostPrompt))
        {
            return ModelSceneGenerationResult.NotMatched;
        }

        if (!definition.Mounts.Contains(VaseOpeningMount, StringComparer.Ordinal))
        {
            return Failure("The matched model does not declare a supported vase-opening mount.");
        }

        var hostObjects = PromptParser.ParseObjects(hostPrompt);
        if (hostObjects is null ||
            hostObjects.Value.Length != 1 ||
            hostObjects.Value[0].Ceramic is not { Kind: CeramicKind.Vase })
        {
            return Failure("A catalog model currently requires exactly one blue or cream ceramic vase on a wooden table.");
        }

        var hostResult = SceneGenerator.Generate(new GenerationRequest(
            hostPrompt,
            request.Seed,
            request.Variation,
            request.Width,
            request.Height));
        if (!hostResult.IsSuccess)
        {
            return Failure(hostResult.Error ?? "The model's host scene could not be generated.");
        }

        var hostScene = hostResult.Scene!;
        var vase = hostScene.Ceramics.Single();
        var profile = CeramicForms.For(vase.Kind);
        var left = vase.Body.Center.X - (vase.Body.Size.X * 0.5f);
        var top = vase.Body.Center.Y - (vase.Body.Size.Y * 0.5f);
        var openingCenter = new Vector2(
            left + (profile.MouthCenter.X * vase.Body.Size.X),
            top + (profile.MouthCenter.Y * vase.Body.Size.Y));
        var radiusX = profile.MouthRadius.X * vase.Body.Size.X;
        var radiusY = profile.MouthRadius.Y * vase.Body.Size.Y;
        var mouthTop = openingCenter.Y - radiusY;
        var headroom = mouthTop - TopMargin;
        if (!float.IsFinite(headroom) || headroom <= 0)
        {
            return Failure("The vase does not leave enough space above its opening for this model.");
        }

        var frame = new ModelMountFrameValues(
            openingCenter,
            radiusX,
            radiusY,
            mouthTop,
            profile.RimThickness * MathF.Min(vase.Body.Size.X, vase.Body.Size.Y),
            headroom);
        var context = ModelEvaluationContext.Empty(request.Seed, (float)request.Variation) with
        {
            ChoiceParameters = ImmutableDictionary<string, string>.Empty
                .Add(definition.Prompt.MaterialVariantParameter, variant),
            Frames = ImmutableDictionary<string, ModelMountFrameValues>.Empty
                .Add(VaseOpeningMount, frame),
        };
        var evaluated = ModelDefinitionEvaluator.Evaluate(definition, context);
        if (!evaluated.IsSuccess)
        {
            var details = string.Join(
                Environment.NewLine,
                evaluated.Diagnostics.Select(item => $"{item.Code} ({item.Location}): {item.Message}"));
            return Failure(details);
        }

        var modelToken = definition.Id.Replace('.', '-');
        var subjectToken = $"model-{modelToken}-in-{vase.Glaze.ToString().ToLowerInvariant()}-vase";
        return new(true, hostScene, evaluated.Geometry, subjectToken, null);
    }

    private static bool TryResolvePrompt(
        string prompt,
        IEnumerable<ModelDefinition> definitions,
        out ModelDefinition definition,
        out string variant,
        out string hostPrompt)
    {
        foreach (var candidate in definitions.OrderBy(item => item.Id, StringComparer.Ordinal))
        {
            var promptDefinition = candidate.Prompt;
            if (!promptDefinition.Descriptors.TryGetValue(promptDefinition.MaterialVariantParameter, out var descriptors))
            {
                continue;
            }

            var nouns = promptDefinition.Aliases.Prepend(promptDefinition.Noun);
            foreach (var descriptor in descriptors)
            {
                foreach (var noun in nouns)
                {
                    var phrase = $"{promptDefinition.Article} {descriptor} {noun}";
                    var prefix = phrase + " in ";
                    if (!prompt.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    definition = candidate;
                    variant = descriptor;
                    hostPrompt = prompt[prefix.Length..];
                    return true;
                }
            }
        }

        definition = null!;
        variant = string.Empty;
        hostPrompt = string.Empty;
        return false;
    }

    private static string? Normalize(string? prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return null;
        }

        var normalized = string.Join(
            ' ',
            prompt.Trim().TrimEnd('.', '!', '?').Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return normalized.ToLowerInvariant();
    }

    private static ModelSceneGenerationResult Failure(string error) =>
        new(true, null, null, null, error);
}
