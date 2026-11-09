using System.Collections.Immutable;
using System.Security;
using System.Text.Json;
using Formloom.Core.Models.Definitions;

namespace Formloom.Core.Models.Catalog;

public static class ModelDefinitionCatalog
{
    private const string DefinitionPattern = "*.model.json";

    public static string DefaultRootPath => Path.Combine(AppContext.BaseDirectory, "models");

    public static ModelDefinitionCatalogResult LoadDefault(ModelDefinitionCatalogOptions? options = null) =>
        Load(DefaultRootPath, options);

    public static ModelDefinitionCatalogResult Load(
        string? rootPath,
        ModelDefinitionCatalogOptions? options = null)
    {
        var settings = options ?? new ModelDefinitionCatalogOptions();
        var diagnostics = ImmutableArray.CreateBuilder<ModelDefinitionDiagnostic>();
        var models = ImmutableDictionary.CreateBuilder<string, ModelDefinition>(StringComparer.Ordinal);

        if (ValidateOptions(settings) is { } optionsError)
        {
            diagnostics.Add(new("catalog.invalid-options", string.Empty, string.Empty, optionsError));
            return new(models.ToImmutable(), diagnostics.ToImmutable());
        }

        if (string.IsNullOrWhiteSpace(rootPath))
        {
            diagnostics.Add(new("catalog.root-required", string.Empty, string.Empty, "A model directory path is required."));
            return new(models.ToImmutable(), diagnostics.ToImmutable());
        }

        string fullRoot;
        try
        {
            fullRoot = Path.GetFullPath(rootPath);
        }
        catch (Exception exception) when (IsPathException(exception))
        {
            diagnostics.Add(new("catalog.invalid-root", rootPath, string.Empty, exception.Message));
            return new(models.ToImmutable(), diagnostics.ToImmutable());
        }

        if (!Directory.Exists(fullRoot))
        {
            diagnostics.Add(new("catalog.root-not-found", fullRoot, string.Empty, "The model directory does not exist."));
            return new(models.ToImmutable(), diagnostics.ToImmutable());
        }

        var files = new List<string>();
        try
        {
            foreach (var file in Directory.EnumerateFiles(
                fullRoot,
                DefinitionPattern,
                new EnumerationOptions
                {
                    AttributesToSkip = FileAttributes.ReparsePoint,
                    IgnoreInaccessible = false,
                    RecurseSubdirectories = true,
                    ReturnSpecialDirectories = false,
                }))
            {
                files.Add(file);
                if (files.Count > settings.MaximumFiles)
                {
                    break;
                }
            }
        }
        catch (Exception exception) when (IsFileSystemException(exception))
        {
            diagnostics.Add(new("catalog.enumeration-failed", fullRoot, string.Empty, exception.Message));
            return new(models.ToImmutable(), diagnostics.ToImmutable());
        }

        if (files.Count > settings.MaximumFiles)
        {
            diagnostics.Add(new(
                "catalog.file-limit-exceeded",
                fullRoot,
                string.Empty,
                $"The directory contains more than {settings.MaximumFiles} model files; no definitions were loaded."));
            return new(models.ToImmutable(), diagnostics.ToImmutable());
        }

        files.Sort(StringComparer.Ordinal);
        var candidates = new List<(string FilePath, ModelDefinition Definition)>();
        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(fullRoot, file);
            var definition = ReadDefinition(file, relativePath, settings, diagnostics);
            if (definition is not null)
            {
                candidates.Add((relativePath, definition));
            }
        }

        var rejectedModelIds = new HashSet<string>(StringComparer.Ordinal);
        var duplicateIds = candidates
            .GroupBy(candidate => candidate.Definition.Id, StringComparer.Ordinal)
            .Where(group => group.Count() > 1);

        foreach (var group in duplicateIds)
        {
            rejectedModelIds.Add(group.Key);
            foreach (var candidate in group)
            {
                diagnostics.Add(new(
                    "catalog.duplicate-model-id",
                    candidate.FilePath,
                    "id",
                    $"Model ID '{group.Key}' occurs in more than one file; every definition with this ID is excluded."));
            }
        }

        var promptOwners = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in candidates)
        {
            var definition = candidate.Definition;
            if (rejectedModelIds.Contains(definition.Id))
            {
                continue;
            }

            var phrasesInDefinition = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var phrase in GetPromptPhrases(definition))
            {
                if (!phrasesInDefinition.Add(phrase))
                {
                    rejectedModelIds.Add(definition.Id);
                    diagnostics.Add(new(
                        "definition.duplicate-prompt-phrase",
                        candidate.FilePath,
                        "prompt",
                        $"The definition produces the prompt phrase '{phrase}' more than once through its noun and aliases."));
                    continue;
                }

                if (!promptOwners.TryGetValue(phrase, out var owners))
                {
                    owners = new Dictionary<string, string>(StringComparer.Ordinal);
                    promptOwners.Add(phrase, owners);
                }

                owners[definition.Id] = candidate.FilePath;
            }
        }

        foreach (var (phrase, owners) in promptOwners.Where(entry => entry.Value.Count > 1))
        {
            foreach (var (modelId, filePath) in owners)
            {
                rejectedModelIds.Add(modelId);
                diagnostics.Add(new(
                    "catalog.duplicate-prompt-phrase",
                    filePath,
                    "prompt",
                    $"Prompt phrase '{phrase}' is also declared by another model; every conflicting definition is excluded."));
            }
        }

        foreach (var candidate in candidates)
        {
            if (!rejectedModelIds.Contains(candidate.Definition.Id))
            {
                models.Add(candidate.Definition.Id, candidate.Definition);
            }
        }

        return new(models.ToImmutable(), diagnostics.ToImmutable());
    }

    private static IEnumerable<string> GetPromptPhrases(ModelDefinition definition)
    {
        var prompt = definition.Prompt;
        if (prompt is null || prompt.Descriptors is null ||
            string.IsNullOrWhiteSpace(prompt.MaterialVariantParameter) ||
            !prompt.Descriptors.TryGetValue(prompt.MaterialVariantParameter, out var descriptors) ||
            descriptors.IsDefaultOrEmpty)
        {
            yield break;
        }

        var nouns = new List<string> { prompt.Noun };
        nouns.AddRange(prompt.Aliases);

        foreach (var descriptor in descriptors)
        {
            foreach (var noun in nouns)
            {
                var phrase = $"{prompt.Article} {descriptor} {noun}";
                yield return string.Join(' ', phrase.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                    .ToLowerInvariant();
            }
        }
    }

    private static ModelDefinition? ReadDefinition(
        string fullPath,
        string relativePath,
        ModelDefinitionCatalogOptions options,
        ImmutableArray<ModelDefinitionDiagnostic>.Builder diagnostics)
    {
        string json;
        try
        {
            var info = new FileInfo(fullPath);
            if ((info.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                diagnostics.Add(new(
                    "catalog.reparse-file-skipped",
                    relativePath,
                    string.Empty,
                    "Model files that are symbolic links or reparse points are not loaded."));
                return null;
            }

            if (info.Length > options.MaximumFileBytes)
            {
                diagnostics.Add(new(
                    "catalog.file-too-large",
                    relativePath,
                    string.Empty,
                    $"The file is {info.Length} bytes; the limit is {options.MaximumFileBytes}."));
                return null;
            }

            json = File.ReadAllText(fullPath);
        }
        catch (Exception exception) when (IsFileSystemException(exception))
        {
            diagnostics.Add(new("catalog.file-read-failed", relativePath, string.Empty, exception.Message));
            return null;
        }

        ModelDefinition? definition;
        try
        {
            definition = JsonSerializer.Deserialize<ModelDefinition>(json, ModelDefinitionJson.CreateOptions());
        }
        catch (JsonException exception)
        {
            var location = exception.Path ?? string.Empty;
            if (exception.LineNumber is { } line && exception.BytePositionInLine is { } column)
            {
                location += $" (line {line + 1}, byte {column + 1})";
            }

            diagnostics.Add(new("catalog.invalid-json", relativePath, location, exception.Message));
            return null;
        }

        if (definition is null)
        {
            diagnostics.Add(new("catalog.empty-document", relativePath, string.Empty, "The model document is empty or null."));
            return null;
        }

        var validation = ModelDefinitionValidator.Validate(definition, relativePath, options);
        diagnostics.AddRange(validation);
        return validation.IsDefaultOrEmpty ? definition : null;
    }

    private static string? ValidateOptions(ModelDefinitionCatalogOptions options)
    {
        if (options.MaximumFiles < 1 || options.MaximumFileBytes < 1 ||
            options.MaximumNodes < 1 || options.MaximumGraphDepth < 1 ||
            options.MaximumProfilePoints < 3 || options.MaximumRepeatCount < 1 ||
            options.MaximumTotalRepeatCount < 1 || options.MaximumExpressionDepth < 1 ||
            options.MaximumExpressionNodes < 1)
        {
            return "Catalog limits must be positive; MaximumProfilePoints must be at least three.";
        }

        return null;
    }

    private static bool IsPathException(Exception exception) =>
        exception is ArgumentException or NotSupportedException or PathTooLongException or SecurityException;

    private static bool IsFileSystemException(Exception exception) =>
        exception is IOException or UnauthorizedAccessException or ArgumentException or
            NotSupportedException or SecurityException;
}
