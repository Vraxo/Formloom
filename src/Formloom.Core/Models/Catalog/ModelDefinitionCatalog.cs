using System.Collections.Immutable;
using System.Security;
using System.Text.Json;
using Formloom.Core.Models.Definitions;

namespace Formloom.Core.Models.Catalog;

public static class ModelDefinitionCatalog
{
    private const string DefinitionPattern = "*.model.json";

    public static ModelDefinitionCatalogResult Load(
        string? rootPath,
        ModelDefinitionCatalogOptions? options = null)
    {
        var settings = options ?? new ModelDefinitionCatalogOptions();
        var diagnostics = ImmutableArray.CreateBuilder<ModelDefinitionDiagnostic>();
        var models = ImmutableDictionary.CreateBuilder<string, ModelDefinition>(StringComparer.Ordinal);
        var duplicateIds = new HashSet<string>(StringComparer.Ordinal);

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

        string[] files;
        try
        {
            files = Directory
                .EnumerateFiles(
                    fullRoot,
                    DefinitionPattern,
                    new EnumerationOptions
                    {
                        AttributesToSkip = FileAttributes.ReparsePoint,
                        IgnoreInaccessible = false,
                        RecurseSubdirectories = true,
                        ReturnSpecialDirectories = false,
                    })
                .Order(StringComparer.Ordinal)
                .ToArray();
        }
        catch (Exception exception) when (IsFileSystemException(exception))
        {
            diagnostics.Add(new("catalog.enumeration-failed", fullRoot, string.Empty, exception.Message));
            return new(models.ToImmutable(), diagnostics.ToImmutable());
        }

        if (files.Length > settings.MaximumFiles)
        {
            diagnostics.Add(new(
                "catalog.file-limit-exceeded",
                fullRoot,
                string.Empty,
                $"The directory contains {files.Length} model files; the limit is {settings.MaximumFiles}. No definitions were loaded."));
            return new(models.ToImmutable(), diagnostics.ToImmutable());
        }

        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(fullRoot, file);
            var definition = ReadDefinition(file, relativePath, settings, diagnostics);
            if (definition is null)
            {
                continue;
            }

            if (duplicateIds.Contains(definition.Id))
            {
                diagnostics.Add(new(
                    "catalog.duplicate-model-id",
                    relativePath,
                    "id",
                    $"Model ID '{definition.Id}' is already duplicated; every definition with this ID is excluded."));
                continue;
            }

            if (models.Remove(definition.Id))
            {
                duplicateIds.Add(definition.Id);
                diagnostics.Add(new(
                    "catalog.duplicate-model-id",
                    relativePath,
                    "id",
                    $"Model ID '{definition.Id}' occurs in more than one file; every definition with this ID is excluded."));
                continue;
            }

            models.Add(definition.Id, definition);
        }

        foreach (var duplicateId in duplicateIds)
        {
            models.Remove(duplicateId);
        }

        return new(models.ToImmutable(), diagnostics.ToImmutable());
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
            options.MaximumNodes < 1 || options.MaximumProfilePoints < 3 ||
            options.MaximumRepeatCount < 1 || options.MaximumExpressionDepth < 1 ||
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