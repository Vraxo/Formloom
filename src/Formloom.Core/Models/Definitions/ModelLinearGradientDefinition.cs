using System.Collections.Immutable;

namespace Formloom.Core.Models.Definitions;

/// <summary>Describes a linear gradient in scene-normalized coordinates.</summary>
public sealed record ModelLinearGradientDefinition(
    ModelPoint Start,
    ModelPoint End,
    ImmutableArray<ModelLinearGradientStopDefinition> Stops);
