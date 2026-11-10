using System.Collections.Immutable;
using System.Numerics;
using Formloom.Core.Core;
using Formloom.Core.Geometry;
using Formloom.Core.Models.Catalog;
using Formloom.Core.Models.Definitions;
using Formloom.Core.Models.Geometry;

namespace Formloom.Core.Models.Evaluation;

public static class ModelDefinitionEvaluator
{
    public static ModelEvaluationResult Evaluate(
        ModelDefinition definition,
        ModelEvaluationContext context,
        ModelDefinitionCatalogOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(context);

        var settings = options ?? new ModelDefinitionCatalogOptions();
        var diagnostics = ImmutableArray.CreateBuilder<ModelEvaluationDiagnostic>();
        var emptyNumbers = ImmutableDictionary<string, double>.Empty;
        var emptyChoices = ImmutableDictionary<string, string>.Empty;

        foreach (var diagnostic in ModelDefinitionValidator.Validate(definition, "<evaluation>", settings))
        {
            diagnostics.Add(new(diagnostic.Code, diagnostic.Location, diagnostic.Message));
        }

        if (diagnostics.Count > 0)
        {
            return Failure(definition.Id, emptyNumbers, emptyChoices, diagnostics);
        }

        ValidateContext(definition, context, diagnostics);
        if (diagnostics.Count > 0)
        {
            return Failure(definition.Id, emptyNumbers, emptyChoices, diagnostics);
        }

        var numberParameters = ResolveNumberParameters(definition, context, settings, diagnostics);
        var choiceParameters = ResolveChoiceParameters(definition, context, diagnostics);
        if (diagnostics.Count > 0)
        {
            return Failure(definition.Id, numberParameters, choiceParameters, diagnostics);
        }

        var nodeOutputs = new Dictionary<string, ModelNodeEvaluationOutput>(StringComparer.Ordinal);
        for (var index = 0; index < definition.Nodes.Length; index++)
        {
            var node = definition.Nodes[index];
            if (!TryEvaluateNode(
                definition,
                node,
                index,
                context,
                numberParameters,
                choiceParameters,
                settings,
                nodeOutputs,
                diagnostics,
                out var output))
            {
                continue;
            }

            nodeOutputs.Add(node.Id, output);
        }

        if (diagnostics.Count > 0)
        {
            return Failure(definition.Id, numberParameters, choiceParameters, diagnostics);
        }

        var paths = ImmutableArray.CreateBuilder<ModelGeometryPath>();
        foreach (var outputId in definition.Outputs)
        {
            if (nodeOutputs.TryGetValue(outputId, out var output))
            {
                paths.AddRange(output.Paths);
            }
        }

        var geometry = new ModelGeometry(
            definition.Id,
            ModelGeometryCoordinateSpace.SceneNormalized,
            paths.ToImmutable());
        foreach (var diagnostic in ModelGeometryValidator.Validate(geometry))
        {
            diagnostics.Add(new(diagnostic.Code, diagnostic.PathId ?? "geometry", diagnostic.Message));
        }

        return diagnostics.Count == 0
            ? new(geometry, numberParameters, choiceParameters, [])
            : Failure(definition.Id, numberParameters, choiceParameters, diagnostics);
    }

    private static void ValidateContext(
        ModelDefinition definition,
        ModelEvaluationContext context,
        ImmutableArray<ModelEvaluationDiagnostic>.Builder diagnostics)
    {
        if (!float.IsFinite(context.VariationAmount) || context.VariationAmount is < 0 or > 1)
        {
            Add("evaluation.invalid-variation", "context.variationAmount", "Variation amount must be finite and in the range 0..1.", diagnostics);
        }

        if (context.NumberParameters is null || context.ChoiceParameters is null || context.Frames is null)
        {
            Add("evaluation.invalid-context", "context", "Parameter and frame collections must be initialized.", diagnostics);
            return;
        }

        foreach (var (name, value) in context.NumberParameters)
        {
            if (!definition.Parameters.TryGetValue(name, out var parameter) || parameter.Type != ModelParameterType.Number)
            {
                Add("evaluation.unknown-number-override", $"context.numberParameters.{name}", "The override must target a declared number parameter.", diagnostics);
            }
            else if (!double.IsFinite(value))
            {
                Add("evaluation.non-finite-number-override", $"context.numberParameters.{name}", "Number overrides must be finite.", diagnostics);
            }
        }

        foreach (var name in context.ChoiceParameters.Keys)
        {
            if (!definition.Parameters.TryGetValue(name, out var parameter) || parameter.Type != ModelParameterType.Choice)
            {
                Add("evaluation.unknown-choice-override", $"context.choiceParameters.{name}", "The override must target a declared choice parameter.", diagnostics);
            }
        }

        foreach (var (mount, frame) in context.Frames)
        {
            if (!definition.Mounts.Contains(mount, StringComparer.Ordinal))
            {
                Add("evaluation.undeclared-frame", $"context.frames.{mount}", "A frame was supplied for a mount not declared by this model.", diagnostics);
                continue;
            }

            if (frame is null ||
                !IsFinite(frame.OpeningCenter) ||
                !float.IsFinite(frame.RadiusX) ||
                !float.IsFinite(frame.RadiusY) ||
                !float.IsFinite(frame.MouthTop) ||
                !float.IsFinite(frame.RimThickness) ||
                !float.IsFinite(frame.Headroom) ||
                frame.RadiusX <= 0 ||
                frame.RadiusY <= 0 ||
                frame.RimThickness < 0 ||
                frame.Headroom < 0)
            {
                Add("evaluation.invalid-frame", $"context.frames.{mount}", "Frame metrics must be finite; radii must be positive and rim thickness/headroom must be non-negative.", diagnostics);
            }
        }
    }

    private static ImmutableDictionary<string, double> ResolveNumberParameters(
        ModelDefinition definition,
        ModelEvaluationContext context,
        ModelDefinitionCatalogOptions options,
        ImmutableArray<ModelEvaluationDiagnostic>.Builder diagnostics)
    {
        var resolved = new Dictionary<string, double>(StringComparer.Ordinal);
        var resolving = new HashSet<string>(StringComparer.Ordinal);
        var emptyNodes = new Dictionary<string, ModelNodeEvaluationOutput>(StringComparer.Ordinal);
        string? referenceError = null;

        double? ResolveReference(string name)
        {
            if (TryResolve(name, out var value, out var error))
            {
                return value;
            }

            referenceError = error;
            return null;
        }

        bool TryResolve(string name, out double value, out string error)
        {
            value = 0;
            error = string.Empty;
            if (resolved.TryGetValue(name, out value))
            {
                return true;
            }

            if (!definition.Parameters.TryGetValue(name, out var parameter) || parameter.Type != ModelParameterType.Number)
            {
                error = $"Number parameter '{name}' does not exist.";
                return false;
            }

            if (!resolving.Add(name))
            {
                error = $"Number parameter dependency cycle detected at '{name}'.";
                return false;
            }

            referenceError = null;
            if (!TryEvaluateExpression(parameter.Minimum, $"parameters.{name}.minimum", out var minimum, out error) ||
                !TryEvaluateExpression(parameter.Maximum, $"parameters.{name}.maximum", out var maximum, out error))
            {
                resolving.Remove(name);
                return false;
            }

            if (minimum > maximum)
            {
                error = $"Number parameter '{name}' has a minimum greater than its maximum.";
                resolving.Remove(name);
                return false;
            }

            if (context.NumberParameters.TryGetValue(name, out var supplied))
            {
                value = supplied;
            }
            else if (!TryEvaluateExpression(parameter.DefaultValue, $"parameters.{name}.defaultValue", out value, out error))
            {
                resolving.Remove(name);
                return false;
            }

            if (value < minimum || value > maximum)
            {
                error = $"Number parameter '{name}' must be between {minimum} and {maximum}.";
                resolving.Remove(name);
                return false;
            }

            resolving.Remove(name);
            resolved.Add(name, value);
            return true;
        }

        bool TryEvaluateExpression(ModelNumberExpression? expression, string location, out double value, out string error)
        {
            referenceError = null;
            var success = ModelNumberExpressionEvaluator.TryEvaluate(
                expression,
                ResolveReference,
                context.Frames,
                emptyNodes,
                options,
                out value,
                out error);
            if (!success && referenceError is not null)
            {
                error = referenceError;
            }

            if (!success)
            {
                Add("evaluation.parameter-expression-failed", location, error, diagnostics);
            }

            return success;
        }

        foreach (var name in definition.Parameters.Keys.OrderBy(item => item, StringComparer.Ordinal))
        {
            if (!definition.Parameters.TryGetValue(name, out var parameter) || parameter.Type != ModelParameterType.Number)
            {
                continue;
            }

            if (!TryResolve(name, out _, out var error))
            {
                if (!diagnostics.Any(item => item.Location.StartsWith($"parameters.{name}", StringComparison.Ordinal)))
                {
                    Add("evaluation.parameter-resolution-failed", $"parameters.{name}", error, diagnostics);
                }
            }
        }

        return resolved.ToImmutableDictionary(StringComparer.Ordinal);
    }

    private static ImmutableDictionary<string, string> ResolveChoiceParameters(
        ModelDefinition definition,
        ModelEvaluationContext context,
        ImmutableArray<ModelEvaluationDiagnostic>.Builder diagnostics)
    {
        var resolved = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
        foreach (var (name, parameter) in definition.Parameters.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            if (parameter.Type != ModelParameterType.Choice)
            {
                continue;
            }

            var value = context.ChoiceParameters.TryGetValue(name, out var supplied)
                ? supplied
                : parameter.DefaultChoice;
            if (string.IsNullOrWhiteSpace(value) || !parameter.Values.Contains(value, StringComparer.Ordinal))
            {
                Add("evaluation.invalid-choice", $"parameters.{name}", $"Choice '{value}' is not a permitted value for '{name}'.", diagnostics);
                continue;
            }

            resolved.Add(name, value);
        }

        return resolved.ToImmutable();
    }

    private static bool TryEvaluateNode(
        ModelDefinition definition,
        ModelNodeDefinition node,
        int nodeIndex,
        ModelEvaluationContext context,
        ImmutableDictionary<string, double> numberParameters,
        ImmutableDictionary<string, string> choiceParameters,
        ModelDefinitionCatalogOptions options,
        IReadOnlyDictionary<string, ModelNodeEvaluationOutput> priorNodes,
        ImmutableArray<ModelEvaluationDiagnostic>.Builder diagnostics,
        out ModelNodeEvaluationOutput output)
    {
        output = new([], [], ImmutableDictionary<string, Vector2>.Empty);
        var location = $"nodes[{nodeIndex}]";

        bool TryNumber(ModelNumberExpression? expression, string property, out float result)
        {
            result = 0;
            if (!ModelNumberExpressionEvaluator.TryEvaluate(
                expression,
                name => numberParameters.TryGetValue(name, out var value) ? value : null,
                context.Frames,
                priorNodes,
                options,
                out var number,
                out var error))
            {
                Add("evaluation.expression-failed", $"{location}.{property}", error, diagnostics);
                return false;
            }

            if (!double.IsFinite(number) || number > float.MaxValue || number < -float.MaxValue)
            {
                Add("evaluation.expression-out-of-range", $"{location}.{property}", "The evaluated value cannot be represented as a finite geometry coordinate.", diagnostics);
                return false;
            }

            result = (float)number;
            return true;
        }

        bool TryPoint(ModelPointExpression? expression, string property, out Vector2 point)
        {
            point = Vector2.Zero;
            if (expression is null ||
                !TryNumber(expression.X, $"{property}.x", out var x) ||
                !TryNumber(expression.Y, $"{property}.y", out var y))
            {
                if (expression is null)
                {
                    Add("evaluation.point-required", $"{location}.{property}", "A point expression is required.", diagnostics);
                }

                return false;
            }

            point = new(x, y);
            return true;
        }

        bool TryMapping(
            ModelProfileMappingDefinition mapping,
            string property,
            out ProfileMappingOptions result)
        {
            result = default;
            if (!TryNumber(mapping.ScaleX, $"{property}.scaleX", out var scaleX) ||
                !TryNumber(mapping.ScaleY, $"{property}.scaleY", out var scaleY) ||
                !TryNumber(mapping.Length, $"{property}.length", out var length) ||
                !TryNumber(mapping.Width, $"{property}.width", out var width) ||
                !TryNumber(mapping.RootOffset, $"{property}.rootOffset", out var rootOffset) ||
                !TryNumber(mapping.Bend, $"{property}.bend", out var bend) ||
                !TryNumber(mapping.TransverseNormalization, $"{property}.transverseNormalization", out var transverseNormalization))
            {
                return false;
            }

            result = new(
                scaleX,
                scaleY,
                length,
                width,
                rootOffset,
                bend,
                transverseNormalization,
                mapping.ReverseLongitudinal);
            return true;
        }

        try
        {
            switch (node)
            {
                case ModelCubicPathNodeDefinition cubic:
                    var segments = ImmutableArray.CreateBuilder<CubicSegment>(cubic.Segments.Length);
                    for (var index = 0; index < cubic.Segments.Length; index++)
                    {
                        var segment = cubic.Segments[index];
                        if (!TryPoint(segment.Start, $"segments[{index}].start", out var start) ||
                            !TryPoint(segment.Control1, $"segments[{index}].control1", out var control1) ||
                            !TryPoint(segment.Control2, $"segments[{index}].control2", out var control2) ||
                            !TryPoint(segment.End, $"segments[{index}].end", out var end))
                        {
                            return false;
                        }

                        segments.Add(new(start, control1, control2, end));
                    }

                    var cubicSegments = segments.MoveToImmutable();
                    output = new(
                        [new(cubic.Id, cubic.Id, cubicSegments, false, cubic.MaterialSlot, cubic.Detail)],
                        cubicSegments,
                        ImmutableDictionary<string, Vector2>.Empty);
                    return true;

                case ModelCurveProjectNodeDefinition projection:
                    if (!priorNodes.TryGetValue(projection.Curve, out var curve) || curve.CurveSegments.IsDefaultOrEmpty)
                    {
                        Add("evaluation.curve-not-available", $"{location}.curve", $"Curve node '{projection.Curve}' has no evaluated cubic segments.", diagnostics);
                        return false;
                    }

                    if (!TryPoint(projection.Target, "target", out var target))
                    {
                        return false;
                    }

                    var projected = CubicCurveProjector.ProjectNearest(curve.CurveSegments, target);
                    output = new(
                        [],
                        [],
                        ImmutableDictionary<string, Vector2>.Empty
                            .Add("point", projected.Point)
                            .Add("tangent", projected.Tangent));
                    return true;

                case ModelProfileMapNodeDefinition mapped:
                    if (!definition.Profiles.TryGetValue(mapped.Profile, out var profile) ||
                        !TryPoint(mapped.Origin, "origin", out var origin) ||
                        !TryNumber(mapped.Angle, "angle", out var angle) ||
                        !TryMapping(mapped.Mapping, "mapping", out var mapping))
                    {
                        return false;
                    }

                    if (mapped.Variation is { } variation)
                    {
                        if (!TryNumber(variation.Angle, "variation.angle", out var profileAngleVariation) ||
                            !TryNumber(variation.Length, "variation.length", out var profileLengthVariation) ||
                            !TryNumber(variation.Width, "variation.width", out var profileWidthVariation) ||
                            !TryNumber(variation.Bend, "variation.bend", out var profileBendVariation))
                        {
                            return false;
                        }

                        if (profileAngleVariation < 0 || profileLengthVariation is < 0 or > 1 ||
                            profileWidthVariation is < 0 or > 1 || profileBendVariation < 0)
                        {
                            Add(
                                "evaluation.invalid-profile-variation",
                                $"{location}.variation",
                                "Profile variation ranges must be non-negative; length and width ranges cannot exceed 1.",
                                diagnostics);
                            return false;
                        }

                        var random = Pcg32.ForStream(context.Seed, $"{definition.Id}/{mapped.Stream}");
                        angle += Jitter(random, profileAngleVariation, context.VariationAmount);
                        mapping = mapping with
                        {
                            Length = mapping.Length * (1 + Jitter(random, profileLengthVariation, context.VariationAmount)),
                            Width = mapping.Width * (1 + Jitter(random, profileWidthVariation, context.VariationAmount)),
                            Bend = mapping.Bend + Jitter(random, profileBendVariation, context.VariationAmount),
                        };
                    }

                    var mappedDirection = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                    var mappedPoints = MapProfile(profile, origin, mappedDirection, mapping);
                    var mappedPath = CreateProfilePath(
                        mapped.Id,
                        mapped.Id,
                        profile,
                        mappedPoints,
                        origin,
                        mappedDirection,
                        mapping,
                        mapped.MaterialSlot,
                        null,
                        mapped.Detail);
                    output = new(
                        [mappedPath],
                        [],
                        mappedPath.Landmarks.ToImmutableDictionary(item => item.Name, item => item.Position, StringComparer.Ordinal));
                    return true;

                case ModelRadialArrayNodeDefinition radial:
                    if (!definition.Profiles.TryGetValue(radial.Profile, out var radialProfile) ||
                        !TryPoint(radial.Center, "center", out var center) ||
                        !TryNumber(radial.RadiusX, "radiusX", out var radiusX) ||
                        !TryNumber(radial.RadiusY, "radiusY", out var radiusY) ||
                        !TryNumber(radial.StartAngle, "startAngle", out var startAngle) ||
                        !TryNumber(radial.Phase, "phase", out var phase) ||
                        !TryNumber(radial.Tone ?? new ModelLiteralExpression(0), "tone", out var baseTone) ||
                        !TryMapping(radial.Mapping, "mapping", out var radialMapping) ||
                        !TryNumber(radial.Variation.Angle, "variation.angle", out var angleVariation) ||
                        !TryNumber(radial.Variation.Length, "variation.length", out var lengthVariation) ||
                        !TryNumber(radial.Variation.Width, "variation.width", out var widthVariation) ||
                        !TryNumber(radial.Variation.Bend, "variation.bend", out var bendVariation) ||
                        !TryNumber(radial.Variation.Tone, "variation.tone", out var toneVariation))
                    {
                        return false;
                    }

                    var elements = RadialArrayGenerator.Generate(
                        radial.Count,
                        startAngle,
                        phase,
                        new(angleVariation, lengthVariation, widthVariation, bendVariation, toneVariation),
                        context.VariationAmount,
                        context.Seed,
                        $"{definition.Id}/{radial.Stream}");
                    var radialPaths = ImmutableArray.CreateBuilder<ModelGeometryPath>(elements.Length);
                    foreach (var element in elements)
                    {
                        var elementMapping = radialMapping with
                        {
                            ScaleX = radialMapping.ScaleX * radiusX,
                            ScaleY = radialMapping.ScaleY * radiusY,
                            Length = radialMapping.Length * element.LengthScale,
                            Width = radialMapping.Width * element.WidthScale,
                            Bend = radialMapping.Bend + element.BendOffset,
                        };
                        var direction = new Vector2(MathF.Cos(element.Angle), MathF.Sin(element.Angle));
                        var elementPoints = MapProfile(radialProfile, center, direction, elementMapping);
                        var tone = baseTone + element.ToneOffset;
                        if (!float.IsFinite(tone) || tone is < 0 or > 1)
                        {
                            Add("evaluation.material-tone-out-of-range", $"{location}.tone", "Base tone plus element variation must remain in the range 0..1.", diagnostics);
                            return false;
                        }

                        var pathOutput = CreateProfilePath(
                            $"{radial.Id}[{element.Index}]",
                            radial.Id,
                            radialProfile,
                            elementPoints,
                            center,
                            direction,
                            elementMapping,
                            radial.MaterialSlot,
                            tone,
                            radial.Detail);
                        radialPaths.Add(pathOutput);
                    }

                    output = new(radialPaths.MoveToImmutable(), [], ImmutableDictionary<string, Vector2>.Empty);
                    return true;

                case ModelGeometryGroupNodeDefinition group:
                    var groupPaths = ImmutableArray.CreateBuilder<ModelGeometryPath>();
                    foreach (var part in group.Parts)
                    {
                        if (!priorNodes.TryGetValue(part, out var partOutput))
                        {
                            Add("evaluation.group-part-not-found", $"{location}.parts", $"Group member '{part}' has no evaluated node output.", diagnostics);
                            return false;
                        }

                        groupPaths.AddRange(partOutput.Paths);
                    }

                    output = new(groupPaths.ToImmutable(), [], ImmutableDictionary<string, Vector2>.Empty);
                    return true;

                default:
                    Add("evaluation.unsupported-operation", $"{location}.op", $"Operation type '{node.GetType().Name}' is not supported.", diagnostics);
                    return false;
            }
        }
        catch (ArgumentException exception)
        {
            Add("evaluation.invalid-operation-input", location, exception.Message, diagnostics);
            return false;
        }
        catch (InvalidOperationException exception)
        {
            Add("evaluation.operation-failed", location, exception.Message, diagnostics);
            return false;
        }
    }

    private static float Jitter(Pcg32 random, float range, float amount) =>
        (((float)random.NextDouble() * 2) - 1) * range * amount;

    private static ImmutableArray<Vector2> MapProfile(
        ModelProfileDefinition profile,
        Vector2 origin,
        Vector2 direction,
        ProfileMappingOptions options) =>
        ProfileMapper.Map(
            profile.Points.Select(point => new Vector2(point.X, point.Y)).ToArray(),
            origin,
            direction,
            options);

    private static ModelGeometryPath CreateProfilePath(
        string id,
        string sourceNodeId,
        ModelProfileDefinition profile,
        ImmutableArray<Vector2> points,
        Vector2 origin,
        Vector2 direction,
        ProfileMappingOptions mapping,
        string? materialSlot,
        float? materialTone,
        bool detail)
    {
        var pathSegments = CreateProfileSegments(points, profile);
        var rootT = mapping.ReverseLongitudinal ? 1f : 0f;
        var tipT = mapping.ReverseLongitudinal ? 0f : 1f;
        var root = MapProfileLandmark(origin, direction, mapping, new(0.5f, rootT));
        var tip = MapProfileLandmark(origin, direction, mapping, new(0.5f, tipT));
        var landmarks = ImmutableArray.Create(
            new ModelGeometryLandmark("attachment", origin),
            new ModelGeometryLandmark("root", root),
            new ModelGeometryLandmark("tip", tip));
        return new ModelGeometryPath(id, sourceNodeId, pathSegments, profile.Closed, materialSlot, detail)
        {
            MaterialTone = materialTone,
            Landmarks = landmarks,
        };
    }

    private static Vector2 MapProfileLandmark(
        Vector2 origin,
        Vector2 direction,
        ProfileMappingOptions options,
        Vector2 point) =>
        ProfileMapper.Map([point], origin, direction, options)[0];

    private static ImmutableArray<CubicSegment> CreateProfileSegments(
        ImmutableArray<Vector2> points,
        ModelProfileDefinition profile)
    {
        if (profile.Interpolation == ModelProfileInterpolation.ClosedCubic)
        {
            return CurveBuilder.ClosedCubic(points.ToArray());
        }

        if (profile.Interpolation == ModelProfileInterpolation.OpenCubic)
        {
            return CurveBuilder.OpenCubic(points.ToArray());
        }

        var segmentCount = profile.Closed ? points.Length : points.Length - 1;
        var segments = ImmutableArray.CreateBuilder<CubicSegment>(segmentCount);
        for (var index = 0; index < segmentCount; index++)
        {
            var start = points[index];
            var end = points[(index + 1) % points.Length];
            var delta = end - start;
            segments.Add(new(start, start + (delta / 3), start + ((delta * 2) / 3), end));
        }

        return segments.MoveToImmutable();
    }

    private static ModelEvaluationResult Failure(
        string modelId,
        ImmutableDictionary<string, double> numberParameters,
        ImmutableDictionary<string, string> choiceParameters,
        ImmutableArray<ModelEvaluationDiagnostic>.Builder diagnostics) =>
        new(
            null,
            numberParameters,
            choiceParameters,
            diagnostics.Select(item => item with { ModelId = modelId }).ToImmutableArray());

    private static void Add(
        string code,
        string location,
        string message,
        ImmutableArray<ModelEvaluationDiagnostic>.Builder diagnostics) =>
        diagnostics.Add(new(code, location, message));

    private static bool IsFinite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
}
