using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Formloom.Core.Models.Definitions;

namespace Formloom.Core.Models.Catalog;

public static class ModelDefinitionValidator
{
    private const string ExpectedFormat = "formloom.model";
    private const int SupportedSchemaVersion = 1;
    private const string SupportedMount = "vase-opening";

    private static readonly IReadOnlyDictionary<string, ModelNodeDefinition> EmptyNodes =
        ImmutableDictionary<string, ModelNodeDefinition>.Empty;

    private static readonly HashSet<string> FrameMetrics = new(StringComparer.Ordinal)
    {
        "openingCenter.x",
        "openingCenter.y",
        "radiusX",
        "radiusY",
        "mouthTop",
        "rimThickness",
        "headroom",
    };

    private static readonly Regex ModelIdPattern = new(
        @"\A[a-z][a-z0-9]*(?:\.[a-z][a-z0-9-]*)+\z",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    private static readonly Regex MaterialTokenPattern = new(
        @"\A#[0-9A-Fa-f]{6}(?:[0-9A-Fa-f]{2})?\z",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    public static ImmutableArray<ModelDefinitionDiagnostic> Validate(
        ModelDefinition definition,
        string filePath,
        ModelDefinitionCatalogOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var settings = options ?? new ModelDefinitionCatalogOptions();
        var diagnostics = ImmutableArray.CreateBuilder<ModelDefinitionDiagnostic>();
        var location = string.IsNullOrWhiteSpace(filePath) ? string.Empty : filePath;

        void Add(string code, string path, string message) =>
            diagnostics.Add(new(code, location, path, message));

        if (!string.Equals(definition.Format, ExpectedFormat, StringComparison.Ordinal))
        {
            Add("definition.invalid-format", "format", $"Expected format '{ExpectedFormat}'.");
        }

        if (definition.SchemaVersion != SupportedSchemaVersion)
        {
            Add(
                "definition.unsupported-schema-version",
                "schemaVersion",
                $"Schema version {definition.SchemaVersion} is not supported. Supported version: {SupportedSchemaVersion}.");
        }

        if (string.IsNullOrWhiteSpace(definition.Id) || !ModelIdPattern.IsMatch(definition.Id))
        {
            Add("definition.invalid-id", "id", "Model IDs must be lowercase, namespaced identifiers such as 'flowers.rose'.");
        }

        if (string.IsNullOrWhiteSpace(definition.Name))
        {
            Add("definition.missing-name", "name", "A model name is required.");
        }

        if (string.IsNullOrWhiteSpace(definition.Category))
        {
            Add("definition.missing-category", "category", "A model category is required.");
        }

        ValidatePrompt(definition, Add);
        ValidateMounts(definition.Mounts, Add);
        ValidateParameters(definition.Parameters, settings, definition.Mounts, Add);
        ValidateProfiles(definition.Profiles, settings, Add);
        ValidateMaterials(definition.Materials, Add);
        ValidateNodes(definition, settings, Add);
        return diagnostics.ToImmutable();
    }

    private static void ValidatePrompt(
        ModelDefinition definition,
        Action<string, string, string> add)
    {
        var prompt = definition.Prompt;
        if (prompt is null)
        {
            add("definition.prompt-required", "prompt", "Prompt metadata is required.");
            return;
        }

        if (prompt.Article is not ("a" or "an"))
        {
            add("definition.invalid-prompt-article", "prompt.article", "Prompt article must be 'a' or 'an'.");
        }

        if (string.IsNullOrWhiteSpace(prompt.Noun))
        {
            add("definition.invalid-prompt-noun", "prompt.noun", "A prompt noun is required.");
        }

        if (prompt.Aliases.IsDefault || prompt.Aliases.Any(string.IsNullOrWhiteSpace) ||
            prompt.Aliases.Distinct(StringComparer.OrdinalIgnoreCase).Count() != prompt.Aliases.Length)
        {
            add("definition.invalid-prompt-aliases", "prompt.aliases", "Prompt aliases must be initialized, non-empty, and unique.");
        }

        if (string.IsNullOrWhiteSpace(prompt.MaterialVariantParameter))
        {
            add("definition.invalid-material-parameter", "prompt.materialVariantParameter", "The material variant parameter is required.");
            return;
        }

        if (prompt.Descriptors is null || prompt.Descriptors.Count == 0)
        {
            add("definition.prompt-descriptors-required", "prompt.descriptors", "At least one prompt descriptor group is required.");
            return;
        }

        foreach (var (key, values) in prompt.Descriptors)
        {
            if (string.IsNullOrWhiteSpace(key) || values.IsDefaultOrEmpty ||
                values.Any(string.IsNullOrWhiteSpace) ||
                values.Distinct(StringComparer.Ordinal).Count() != values.Length)
            {
                add("definition.invalid-prompt-descriptor", $"prompt.descriptors.{key}", "Descriptor values must be initialized, non-empty, and unique.");
            }
        }

        if (definition.Parameters is null ||
            !definition.Parameters.TryGetValue(prompt.MaterialVariantParameter, out var parameter) ||
            parameter is null ||
            parameter.Type != ModelParameterType.Choice)
        {
            add(
                "definition.material-parameter-not-found",
                "prompt.materialVariantParameter",
                "The material variant parameter must reference a declared choice parameter.");
            return;
        }

        if (parameter.Values.IsDefaultOrEmpty)
        {
            return;
        }

        if (definition.Materials is null)
        {
            return;
        }

        foreach (var value in parameter.Values)
        {
            if (string.IsNullOrWhiteSpace(value) || !definition.Materials.ContainsKey(value))
            {
                add(
                    "definition.material-variant-not-found",
                    $"materials.{value}",
                    $"Choice '{value}' is not declared as a material variant.");
            }
        }

        if (!prompt.Descriptors.TryGetValue(prompt.MaterialVariantParameter, out var descriptorValues) ||
            descriptorValues.IsDefault ||
            !descriptorValues.SequenceEqual(parameter.Values, StringComparer.Ordinal))
        {
            add(
                "definition.material-descriptors-mismatch",
                $"prompt.descriptors.{prompt.MaterialVariantParameter}",
                "The material descriptor values must match the material variant choice values in the same order.");
        }
    }

    private static void ValidateMounts(
        ImmutableArray<string> mounts,
        Action<string, string, string> add)
    {
        if (mounts.IsDefault)
        {
            add("definition.mounts-required", "mounts", "Mount declarations must be initialized.");
            return;
        }

        if (mounts.Distinct(StringComparer.Ordinal).Count() != mounts.Length)
        {
            add("definition.duplicate-mount", "mounts", "Mount declarations must be unique.");
        }

        for (var index = 0; index < mounts.Length; index++)
        {
            if (!string.Equals(mounts[index], SupportedMount, StringComparison.Ordinal))
            {
                add(
                    "definition.unsupported-mount",
                    $"mounts[{index}]",
                    $"Mount '{mounts[index]}' is not supported. Supported mount: '{SupportedMount}'.");
            }
        }
    }

    private static void ValidateParameters(
        ImmutableDictionary<string, ModelParameterDefinition>? parameters,
        ModelDefinitionCatalogOptions options,
        ImmutableArray<string> mounts,
        Action<string, string, string> add)
    {
        if (parameters is null)
        {
            add("definition.parameters-required", "parameters", "Parameter declarations are required.");
            return;
        }

        foreach (var (name, parameter) in parameters)
        {
            var path = $"parameters.{name}";
            if (string.IsNullOrWhiteSpace(name) || parameter is null)
            {
                add("definition.invalid-parameter", path, "Parameter names and definitions must be non-empty.");
                continue;
            }

            if (!Enum.IsDefined(parameter.Type))
            {
                add("definition.invalid-parameter-type", $"{path}.type", "The parameter type is not supported.");
                continue;
            }

            if (parameter.Type == ModelParameterType.Number)
            {
                if (parameter.DefaultValue is null || parameter.Minimum is null || parameter.Maximum is null ||
                    parameter.Values.IsDefault || !parameter.Values.IsEmpty || parameter.DefaultChoice is not null)
                {
                    add("definition.invalid-number-parameter", path, "Number parameters require defaultValue, minimum, and maximum, and cannot declare choice values.");
                }

                ValidateExpression(parameter.DefaultValue, $"{path}.defaultValue", options, parameters, mounts, EmptyNodes, add);
                ValidateExpression(parameter.Minimum, $"{path}.minimum", options, parameters, mounts, EmptyNodes, add);
                ValidateExpression(parameter.Maximum, $"{path}.maximum", options, parameters, mounts, EmptyNodes, add);
                continue;
            }

            if (parameter.DefaultValue is not null || parameter.Minimum is not null || parameter.Maximum is not null ||
                parameter.Values.IsDefaultOrEmpty || string.IsNullOrWhiteSpace(parameter.DefaultChoice) ||
                !parameter.Values.Contains(parameter.DefaultChoice, StringComparer.Ordinal) ||
                parameter.Values.Any(string.IsNullOrWhiteSpace) ||
                parameter.Values.Distinct(StringComparer.Ordinal).Count() != parameter.Values.Length)
            {
                add("definition.invalid-choice-parameter", path, "Choice parameters require unique non-empty values and a defaultChoice contained in those values; numeric fields must be absent.");
            }
        }
    }

    private static void ValidateProfiles(
        ImmutableDictionary<string, ModelProfileDefinition>? profiles,
        ModelDefinitionCatalogOptions options,
        Action<string, string, string> add)
    {
        if (profiles is null || profiles.Count == 0)
        {
            add("definition.profiles-required", "profiles", "At least one profile is required.");
            return;
        }

        foreach (var (name, profile) in profiles)
        {
            var path = $"profiles.{name}";
            if (string.IsNullOrWhiteSpace(name) || profile is null)
            {
                add("definition.invalid-profile", path, "Profile names and definitions must be non-empty.");
                continue;
            }

            if (!Enum.IsDefined(profile.Interpolation))
            {
                add("definition.invalid-profile-interpolation", $"{path}.interpolation", "The profile interpolation mode is not supported.");
            }

            if (profile.Points.IsDefaultOrEmpty || profile.Points.Length > options.MaximumProfilePoints)
            {
                add(
                    "definition.invalid-profile-point-count",
                    $"{path}.points",
                    $"Profiles must contain between 2 and {options.MaximumProfilePoints} points.");
                continue;
            }

            if (profile.Interpolation == ModelProfileInterpolation.ClosedCubic && (!profile.Closed || profile.Points.Length < 3))
            {
                add("definition.invalid-closed-profile", path, "Closed-cubic profiles must be closed and contain at least three points.");
            }

            if (profile.Interpolation == ModelProfileInterpolation.OpenCubic && profile.Closed)
            {
                add("definition.invalid-open-profile", path, "Open-cubic profiles must be open.");
            }

            for (var index = 0; index < profile.Points.Length; index++)
            {
                var point = profile.Points[index];
                if (point is null || !float.IsFinite(point.X) || !float.IsFinite(point.Y) ||
                    point.X is < 0 or > 1 || point.Y is < 0 or > 1)
                {
                    add(
                        "definition.invalid-profile-point",
                        $"{path}.points[{index}]",
                        "Profile coordinates must be finite values in the normalized 0..1 range.");
                }
            }
        }
    }

    private static void ValidateMaterials(
        ImmutableDictionary<string, ModelMaterialVariantDefinition>? materials,
        Action<string, string, string> add)
    {
        if (materials is null || materials.Count == 0)
        {
            add("definition.materials-required", "materials", "At least one material variant is required.");
            return;
        }

        foreach (var (name, material) in materials)
        {
            var path = $"materials.{name}";
            if (string.IsNullOrWhiteSpace(name) || material is null || material.Tokens is null || material.Tokens.Count == 0)
            {
                add("definition.invalid-material", path, "Material variants require a name and at least one color token.");
                continue;
            }

            foreach (var (tokenName, value) in material.Tokens)
            {
                if (string.IsNullOrWhiteSpace(tokenName) || string.IsNullOrWhiteSpace(value) || !MaterialTokenPattern.IsMatch(value))
                {
                    add(
                        "definition.invalid-material-token",
                        $"{path}.tokens.{tokenName}",
                        "Material colors must use #RRGGBB or #RRGGBBAA hexadecimal notation.");
                }
            }
        }
    }

    private static void ValidateNodes(
        ModelDefinition definition,
        ModelDefinitionCatalogOptions options,
        Action<string, string, string> add)
    {
        if (definition.Nodes.IsDefaultOrEmpty || definition.Nodes.Length > options.MaximumNodes)
        {
            add(
                "definition.invalid-node-count",
                "nodes",
                $"Definitions must contain between 1 and {options.MaximumNodes} operation nodes.");
            return;
        }

        if (definition.Profiles is null || definition.Mounts.IsDefault)
        {
            return;
        }

        var priorNodes = new Dictionary<string, ModelNodeDefinition>(StringComparer.Ordinal);
        var nodeDepths = new Dictionary<string, int>(StringComparer.Ordinal);
        var totalRepeatCount = 0;
        var allIds = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < definition.Nodes.Length; index++)
        {
            var node = definition.Nodes[index];
            var path = $"nodes[{index}]";
            if (node is null)
            {
                add("definition.null-node", path, "Operation nodes cannot be null.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(node.Id))
            {
                add("definition.invalid-node-id", $"{path}.id", "Every operation node requires an ID.");
                continue;
            }

            if (!allIds.Add(node.Id))
            {
                add("definition.duplicate-node-id", $"{path}.id", $"Node ID '{node.Id}' is duplicated.");
                continue;
            }

            switch (node)
            {
                case ModelCubicPathNodeDefinition pathNode:
                    ValidateCubicPath(pathNode, path, options, definition.Parameters, definition.Mounts, priorNodes, add);
                    break;
                case ModelCurveProjectNodeDefinition curveNode:
                    if (string.IsNullOrWhiteSpace(curveNode.Curve) || !priorNodes.TryGetValue(curveNode.Curve, out var curve) || curve is not ModelCubicPathNodeDefinition)
                    {
                        add("definition.curve-reference-not-found", $"{path}.curve", "curve.project must reference an earlier path.cubic node.");
                    }

                    ValidatePointExpression(curveNode.Target, $"{path}.target", options, definition.Parameters, definition.Mounts, priorNodes, add);
                    break;
                case ModelProfileMapNodeDefinition profileNode:
                    if (string.IsNullOrWhiteSpace(profileNode.Profile) || !definition.Profiles.ContainsKey(profileNode.Profile))
                    {
                        add("definition.profile-reference-not-found", $"{path}.profile", $"Profile '{profileNode.Profile}' does not exist.");
                    }

                    ValidatePointExpression(profileNode.Origin, $"{path}.origin", options, definition.Parameters, definition.Mounts, priorNodes, add);
                    ValidateExpression(profileNode.Angle, $"{path}.angle", options, definition.Parameters, definition.Mounts, priorNodes, add);
                    ValidateMapping(profileNode.Mapping, $"{path}.mapping", options, definition.Parameters, definition.Mounts, priorNodes, add);
                    break;
                case ModelRadialArrayNodeDefinition radialNode:
                    if (string.IsNullOrWhiteSpace(radialNode.Profile) || !definition.Profiles.ContainsKey(radialNode.Profile))
                    {
                        add("definition.profile-reference-not-found", $"{path}.profile", $"Profile '{radialNode.Profile}' does not exist.");
                    }

                    if (radialNode.Count is < 1 || radialNode.Count > options.MaximumRepeatCount)
                    {
                        add(
                            "definition.invalid-repeat-count",
                            $"{path}.count",
                            $"Radial counts must be between 1 and {options.MaximumRepeatCount}.");
                    }
                    else
                    {
                        totalRepeatCount += radialNode.Count;
                        if (totalRepeatCount > options.MaximumTotalRepeatCount)
                        {
                            add(
                                "definition.total-repeat-limit-exceeded",
                                $"{path}.count",
                                $"The model declares {totalRepeatCount} repeated elements; the limit is {options.MaximumTotalRepeatCount}.");
                        }
                    }

                    if (string.IsNullOrWhiteSpace(radialNode.Stream))
                    {
                        add("definition.random-stream-required", $"{path}.stream", "Radial arrays require a stable named random stream.");
                    }

                    ValidatePointExpression(radialNode.Center, $"{path}.center", options, definition.Parameters, definition.Mounts, priorNodes, add);
                    ValidateExpression(radialNode.RadiusX, $"{path}.radiusX", options, definition.Parameters, definition.Mounts, priorNodes, add);
                    ValidateExpression(radialNode.RadiusY, $"{path}.radiusY", options, definition.Parameters, definition.Mounts, priorNodes, add);
                    ValidateExpression(radialNode.StartAngle, $"{path}.startAngle", options, definition.Parameters, definition.Mounts, priorNodes, add);
                    ValidateExpression(radialNode.Phase, $"{path}.phase", options, definition.Parameters, definition.Mounts, priorNodes, add);
                    ValidateMapping(radialNode.Mapping, $"{path}.mapping", options, definition.Parameters, definition.Mounts, priorNodes, add);
                    ValidateVariation(radialNode.Variation, $"{path}.variation", options, definition.Parameters, definition.Mounts, priorNodes, add);
                    if (radialNode.Tone is not null)
                    {
                        ValidateExpression(radialNode.Tone, $"{path}.tone", options, definition.Parameters, definition.Mounts, priorNodes, add);
                    }

                    break;
                case ModelGeometryGroupNodeDefinition groupNode:
                    ValidateGroup(groupNode, path, priorNodes, add);
                    break;
                default:
                    add("definition.unsupported-operation", $"{path}.op", $"Operation type '{node.GetType().Name}' is not supported.");
                    break;
            }

            var depth = 1;
            foreach (var dependency in GetNodeDependencies(node))
            {
                if (nodeDepths.TryGetValue(dependency, out var dependencyDepth))
                {
                    depth = Math.Max(depth, dependencyDepth + 1);
                }
            }

            nodeDepths[node.Id] = depth;
            if (depth > options.MaximumGraphDepth)
            {
                add(
                    "definition.graph-depth-limit",
                    path,
                    $"The operation dependency depth is {depth}; the limit is {options.MaximumGraphDepth}.");
            }

            priorNodes.Add(node.Id, node);
        }

        if (definition.Outputs.IsDefaultOrEmpty)
        {
            add("definition.outputs-required", "outputs", "At least one output node is required.");
            return;
        }

        if (definition.Outputs.Distinct(StringComparer.Ordinal).Count() != definition.Outputs.Length)
        {
            add("definition.duplicate-output", "outputs", "Output node IDs must be unique.");
        }

        for (var index = 0; index < definition.Outputs.Length; index++)
        {
            if (string.IsNullOrWhiteSpace(definition.Outputs[index]) || !priorNodes.ContainsKey(definition.Outputs[index]))
            {
                add("definition.output-reference-not-found", $"outputs[{index}]", $"Output node '{definition.Outputs[index]}' does not exist.");
            }
        }
    }

    private static IEnumerable<string> GetNodeDependencies(ModelNodeDefinition node)
    {
        switch (node)
        {
            case ModelCubicPathNodeDefinition path:
                foreach (var segment in path.Segments)
                {
                    if (segment is null)
                    {
                        continue;
                    }

                    foreach (var dependency in GetPointDependencies(segment.Start))
                    {
                        yield return dependency;
                    }

                    foreach (var dependency in GetPointDependencies(segment.Control1))
                    {
                        yield return dependency;
                    }

                    foreach (var dependency in GetPointDependencies(segment.Control2))
                    {
                        yield return dependency;
                    }

                    foreach (var dependency in GetPointDependencies(segment.End))
                    {
                        yield return dependency;
                    }
                }

                break;
            case ModelCurveProjectNodeDefinition curve:
                if (!string.IsNullOrWhiteSpace(curve.Curve))
                {
                    yield return curve.Curve;
                }

                foreach (var dependency in GetPointDependencies(curve.Target))
                {
                    yield return dependency;
                }

                break;
            case ModelProfileMapNodeDefinition profile:
                foreach (var dependency in GetPointDependencies(profile.Origin))
                {
                    yield return dependency;
                }

                foreach (var dependency in GetExpressionDependencies(profile.Angle))
                {
                    yield return dependency;
                }

                foreach (var dependency in GetMappingDependencies(profile.Mapping))
                {
                    yield return dependency;
                }

                break;
            case ModelRadialArrayNodeDefinition radial:
                foreach (var dependency in GetPointDependencies(radial.Center))
                {
                    yield return dependency;
                }

                foreach (var dependency in GetExpressionDependencies(radial.RadiusX))
                {
                    yield return dependency;
                }

                foreach (var dependency in GetExpressionDependencies(radial.RadiusY))
                {
                    yield return dependency;
                }

                foreach (var dependency in GetExpressionDependencies(radial.StartAngle))
                {
                    yield return dependency;
                }

                foreach (var dependency in GetExpressionDependencies(radial.Phase))
                {
                    yield return dependency;
                }

                foreach (var dependency in GetMappingDependencies(radial.Mapping))
                {
                    yield return dependency;
                }

                foreach (var dependency in GetVariationDependencies(radial.Variation))
                {
                    yield return dependency;
                }

                foreach (var dependency in GetExpressionDependencies(radial.Tone))
                {
                    yield return dependency;
                }

                break;
            case ModelGeometryGroupNodeDefinition group:
                foreach (var part in group.Parts)
                {
                    if (!string.IsNullOrWhiteSpace(part))
                    {
                        yield return part;
                    }
                }

                break;
        }
    }

    private static IEnumerable<string> GetPointDependencies(ModelPointExpression? point)
    {
        if (point is null)
        {
            yield break;
        }

        foreach (var dependency in GetExpressionDependencies(point.X))
        {
            yield return dependency;
        }

        foreach (var dependency in GetExpressionDependencies(point.Y))
        {
            yield return dependency;
        }
    }

    private static IEnumerable<string> GetMappingDependencies(ModelProfileMappingDefinition? mapping)
    {
        if (mapping is null)
        {
            yield break;
        }

        foreach (var dependency in GetExpressionDependencies(mapping.ScaleX))
        {
            yield return dependency;
        }

        foreach (var dependency in GetExpressionDependencies(mapping.ScaleY))
        {
            yield return dependency;
        }

        foreach (var dependency in GetExpressionDependencies(mapping.Length))
        {
            yield return dependency;
        }

        foreach (var dependency in GetExpressionDependencies(mapping.Width))
        {
            yield return dependency;
        }

        foreach (var dependency in GetExpressionDependencies(mapping.RootOffset))
        {
            yield return dependency;
        }

        foreach (var dependency in GetExpressionDependencies(mapping.Bend))
        {
            yield return dependency;
        }

        foreach (var dependency in GetExpressionDependencies(mapping.TransverseNormalization))
        {
            yield return dependency;
        }
    }

    private static IEnumerable<string> GetVariationDependencies(ModelRadialVariationDefinition? variation)
    {
        if (variation is null)
        {
            yield break;
        }

        foreach (var dependency in GetExpressionDependencies(variation.Angle))
        {
            yield return dependency;
        }

        foreach (var dependency in GetExpressionDependencies(variation.Length))
        {
            yield return dependency;
        }

        foreach (var dependency in GetExpressionDependencies(variation.Width))
        {
            yield return dependency;
        }

        foreach (var dependency in GetExpressionDependencies(variation.Bend))
        {
            yield return dependency;
        }

        foreach (var dependency in GetExpressionDependencies(variation.Tone))
        {
            yield return dependency;
        }
    }

    private static IEnumerable<string> GetExpressionDependencies(ModelNumberExpression? expression)
    {
        switch (expression)
        {
            case ModelReferenceExpression reference when !string.IsNullOrWhiteSpace(reference.Path):
                var parts = reference.Path.Split(':', StringSplitOptions.None);
                if (parts.Length == 4 && parts[0] == "node" && !string.IsNullOrWhiteSpace(parts[1]))
                {
                    yield return parts[1];
                }

                break;
            case ModelArithmeticExpression arithmetic:
                foreach (var operand in arithmetic.Operands)
                {
                    foreach (var dependency in GetExpressionDependencies(operand))
                    {
                        yield return dependency;
                    }
                }

                break;
            case ModelClampExpression clamp:
                foreach (var dependency in GetExpressionDependencies(clamp.Value))
                {
                    yield return dependency;
                }

                foreach (var dependency in GetExpressionDependencies(clamp.Minimum))
                {
                    yield return dependency;
                }

                foreach (var dependency in GetExpressionDependencies(clamp.Maximum))
                {
                    yield return dependency;
                }

                break;
        }
    }

    private static void ValidateCubicPath(
        ModelCubicPathNodeDefinition node,
        string path,
        ModelDefinitionCatalogOptions options,
        ImmutableDictionary<string, ModelParameterDefinition>? parameters,
        ImmutableArray<string> mounts,
        IReadOnlyDictionary<string, ModelNodeDefinition> priorNodes,
        Action<string, string, string> add)
    {
        if (node.Segments.IsDefaultOrEmpty)
        {
            add("definition.empty-path", $"{path}.segments", "A cubic path must contain at least one segment.");
            return;
        }

        for (var index = 0; index < node.Segments.Length; index++)
        {
            var segment = node.Segments[index];
            if (segment is null)
            {
                add("definition.null-path-segment", $"{path}.segments[{index}]", "Cubic path segments cannot be null.");
                continue;
            }

            ValidatePointExpression(segment.Start, $"{path}.segments[{index}].start", options, parameters, mounts, priorNodes, add);
            ValidatePointExpression(segment.Control1, $"{path}.segments[{index}].control1", options, parameters, mounts, priorNodes, add);
            ValidatePointExpression(segment.Control2, $"{path}.segments[{index}].control2", options, parameters, mounts, priorNodes, add);
            ValidatePointExpression(segment.End, $"{path}.segments[{index}].end", options, parameters, mounts, priorNodes, add);
        }
    }

    private static void ValidateGroup(
        ModelGeometryGroupNodeDefinition node,
        string path,
        Dictionary<string, ModelNodeDefinition> priorNodes,
        Action<string, string, string> add)
    {
        if (node.Parts.IsDefaultOrEmpty)
        {
            add("definition.empty-group", $"{path}.parts", "Geometry groups must contain at least one earlier node.");
            return;
        }

        for (var index = 0; index < node.Parts.Length; index++)
        {
            if (string.IsNullOrWhiteSpace(node.Parts[index]) || !priorNodes.ContainsKey(node.Parts[index]))
            {
                add(
                    "definition.group-reference-not-found",
                    $"{path}.parts[{index}]",
                    $"Group member '{node.Parts[index]}' must reference an earlier node.");
            }
        }
    }

    private static void ValidateMapping(
        ModelProfileMappingDefinition? mapping,
        string path,
        ModelDefinitionCatalogOptions options,
        ImmutableDictionary<string, ModelParameterDefinition>? parameters,
        ImmutableArray<string> mounts,
        IReadOnlyDictionary<string, ModelNodeDefinition> priorNodes,
        Action<string, string, string> add)
    {
        if (mapping is null)
        {
            add("definition.mapping-required", path, "Profile mapping settings are required.");
            return;
        }

        ValidateExpression(mapping.ScaleX, $"{path}.scaleX", options, parameters, mounts, priorNodes, add);
        ValidateExpression(mapping.ScaleY, $"{path}.scaleY", options, parameters, mounts, priorNodes, add);
        ValidateExpression(mapping.Length, $"{path}.length", options, parameters, mounts, priorNodes, add);
        ValidateExpression(mapping.Width, $"{path}.width", options, parameters, mounts, priorNodes, add);
        ValidateExpression(mapping.RootOffset, $"{path}.rootOffset", options, parameters, mounts, priorNodes, add);
        ValidateExpression(mapping.Bend, $"{path}.bend", options, parameters, mounts, priorNodes, add);
        ValidateExpression(mapping.TransverseNormalization, $"{path}.transverseNormalization", options, parameters, mounts, priorNodes, add);
    }

    private static void ValidateVariation(
        ModelRadialVariationDefinition? variation,
        string path,
        ModelDefinitionCatalogOptions options,
        ImmutableDictionary<string, ModelParameterDefinition>? parameters,
        ImmutableArray<string> mounts,
        IReadOnlyDictionary<string, ModelNodeDefinition> priorNodes,
        Action<string, string, string> add)
    {
        if (variation is null)
        {
            add("definition.variation-required", path, "Radial variation settings are required.");
            return;
        }

        ValidateExpression(variation.Angle, $"{path}.angle", options, parameters, mounts, priorNodes, add);
        ValidateExpression(variation.Length, $"{path}.length", options, parameters, mounts, priorNodes, add);
        ValidateExpression(variation.Width, $"{path}.width", options, parameters, mounts, priorNodes, add);
        ValidateExpression(variation.Bend, $"{path}.bend", options, parameters, mounts, priorNodes, add);
        ValidateExpression(variation.Tone, $"{path}.tone", options, parameters, mounts, priorNodes, add);
    }

    private static void ValidatePointExpression(
        ModelPointExpression? point,
        string path,
        ModelDefinitionCatalogOptions options,
        ImmutableDictionary<string, ModelParameterDefinition>? parameters,
        ImmutableArray<string> mounts,
        IReadOnlyDictionary<string, ModelNodeDefinition> priorNodes,
        Action<string, string, string> add)
    {
        if (point is null)
        {
            add("definition.point-required", path, "A point expression is required.");
            return;
        }

        ValidateExpression(point.X, $"{path}.x", options, parameters, mounts, priorNodes, add);
        ValidateExpression(point.Y, $"{path}.y", options, parameters, mounts, priorNodes, add);
    }

    private static void ValidateExpression(
        ModelNumberExpression? expression,
        string path,
        ModelDefinitionCatalogOptions options,
        ImmutableDictionary<string, ModelParameterDefinition>? parameters,
        ImmutableArray<string> mounts,
        IReadOnlyDictionary<string, ModelNodeDefinition> priorNodes,
        Action<string, string, string> add)
    {
        var remaining = options.MaximumExpressionNodes;
        ValidateExpressionNode(expression, path, 1, options.MaximumExpressionDepth, ref remaining, parameters, mounts, priorNodes, add);
        if (remaining == options.MaximumExpressionNodes && expression is not null)
        {
            add("definition.empty-expression", path, "Numeric expressions must contain a supported expression node.");
        }
    }

    private static void ValidateExpressionNode(
        ModelNumberExpression? expression,
        string path,
        int depth,
        int maximumDepth,
        ref int remaining,
        ImmutableDictionary<string, ModelParameterDefinition>? parameters,
        ImmutableArray<string> mounts,
        IReadOnlyDictionary<string, ModelNodeDefinition> priorNodes,
        Action<string, string, string> add)
    {
        if (expression is null)
        {
            add("definition.expression-required", path, "A numeric expression is required.");
            return;
        }

        remaining--;
        if (remaining < 0)
        {
            add("definition.expression-node-limit", path, "The expression exceeds the configured node limit.");
            return;
        }

        if (depth > maximumDepth)
        {
            add("definition.expression-depth-limit", path, "The expression exceeds the configured depth limit.");
            return;
        }

        switch (expression)
        {
            case ModelLiteralExpression literal:
                if (!double.IsFinite(literal.Value))
                {
                    add("definition.non-finite-literal", path, "Numeric literals must be finite.");
                }

                break;
            case ModelReferenceExpression reference:
                ValidateReference(reference.Path, path, parameters, mounts, priorNodes, add);
                break;
            case ModelArithmeticExpression arithmetic:
                if (!Enum.IsDefined(arithmetic.Operator) || arithmetic.Operands.IsDefaultOrEmpty ||
                    arithmetic.Operands.Length < 2 ||
                    ((arithmetic.Operator is ModelArithmeticOperator.Subtract or ModelArithmeticOperator.Divide) && arithmetic.Operands.Length != 2))
                {
                    add("definition.invalid-arithmetic-expression", path, "Arithmetic expressions require a supported operator and a valid operand count.");
                    break;
                }

                for (var index = 0; index < arithmetic.Operands.Length; index++)
                {
                    ValidateExpressionNode(
                        arithmetic.Operands[index],
                        $"{path}.operands[{index}]",
                        depth + 1,
                        maximumDepth,
                        ref remaining,
                        parameters,
                        mounts,
                        priorNodes,
                        add);
                }

                break;
            case ModelClampExpression clamp:
                ValidateExpressionNode(clamp.Value, $"{path}.value", depth + 1, maximumDepth, ref remaining, parameters, mounts, priorNodes, add);
                ValidateExpressionNode(clamp.Minimum, $"{path}.minimum", depth + 1, maximumDepth, ref remaining, parameters, mounts, priorNodes, add);
                ValidateExpressionNode(clamp.Maximum, $"{path}.maximum", depth + 1, maximumDepth, ref remaining, parameters, mounts, priorNodes, add);
                break;
            default:
                add("definition.unsupported-expression", path, $"Expression type '{expression.GetType().Name}' is not supported.");
                break;
        }
    }

    private static void ValidateReference(
        string reference,
        string path,
        ImmutableDictionary<string, ModelParameterDefinition>? parameters,
        ImmutableArray<string> mounts,
        IReadOnlyDictionary<string, ModelNodeDefinition> priorNodes,
        Action<string, string, string> add)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            add("definition.empty-reference", path, "Numeric references must be non-empty.");
            return;
        }

        var parts = reference.Split('.', StringSplitOptions.None);
        if (parts.Length == 2 && parts[0] == "parameters")
        {
            if (parameters is null || !parameters.TryGetValue(parts[1], out var parameter) ||
                parameter is null || parameter.Type != ModelParameterType.Number)
            {
                add("definition.parameter-reference-not-found", path, $"Numeric parameter reference '{reference}' does not resolve to a number parameter.");
            }

            return;
        }

        if (parts.Length == 3 && parts[0] == "frame")
        {
            if (!mounts.Contains(parts[1], StringComparer.Ordinal) || !FrameMetrics.Contains(parts[2]))
            {
                add("definition.frame-reference-not-found", path, $"Frame reference '{reference}' does not resolve to a declared mount metric.");
            }

            return;
        }

        if (parts.Length == 4 && parts[0] == "frame" && parts[2] == "openingCenter")
        {
            if (!mounts.Contains(parts[1], StringComparer.Ordinal) || parts[3] is not ("x" or "y"))
            {
                add("definition.frame-reference-not-found", path, $"Frame reference '{reference}' does not resolve to a declared mount metric.");
            }

            return;
        }

        var nodeParts = reference.Split(':', StringSplitOptions.None);
        if (nodeParts.Length == 4 && nodeParts[0] == "node")
        {
            if (!priorNodes.TryGetValue(nodeParts[1], out var node))
            {
                add("definition.node-reference-not-found", path, $"Node reference '{reference}' must target an earlier operation node.");
                return;
            }

            var validLandmark = node switch
            {
                ModelCurveProjectNodeDefinition => nodeParts[2] is "point" or "tangent",
                ModelProfileMapNodeDefinition => nodeParts[2] is "root" or "tip" or "attachment",
                _ => false,
            };
            if (!validLandmark || nodeParts[3] is not ("x" or "y"))
            {
                add("definition.invalid-node-landmark", path, $"Node reference '{reference}' does not identify a numeric landmark.");
            }

            return;
        }

        add("definition.invalid-reference", path, $"Reference '{reference}' must use parameters.*, frame.*, or node:<node-id>:<landmark>:<axis> syntax.");
    }
}