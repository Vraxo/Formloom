using System.Collections.Immutable;

namespace Formloom.Core.Models.Definitions;

/// <summary>Describes a linear gradient with scene-normalized endpoints or path landmark anchors.</summary>
public sealed record ModelLinearGradientDefinition(
    ModelPoint Start,
    ModelPoint End,
    ImmutableArray<ModelLinearGradientStopDefinition> Stops)
{
    /// <summary>Gets the optional start landmark to resolve independently for each rendered path.</summary>
    public string? StartLandmark { get; init; }

    /// <summary>Gets the optional end landmark to resolve independently for each rendered path.</summary>
    public string? EndLandmark { get; init; }
}
