namespace Formloom.Core.Models.Catalog;

public sealed record ModelDefinitionCatalogOptions
{
    public int MaximumFiles { get; init; } = 256;

    public long MaximumFileBytes { get; init; } = 1_048_576;

    public int MaximumNodes { get; init; } = 2_048;

    public int MaximumProfilePoints { get; init; } = 4_096;

    public int MaximumRepeatCount { get; init; } = 512;

    public int MaximumExpressionDepth { get; init; } = 32;

    public int MaximumExpressionNodes { get; init; } = 256;
}