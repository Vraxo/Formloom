using Formloom.Core.Models.Geometry;
using Formloom.Core.Scenes;

namespace Formloom.Core.Models.Generation;

/// <summary>Contains a catalog-generated model and the scene that hosts its declared mount.</summary>
public sealed record ModelSceneGenerationResult(
    bool IsModelPrompt,
    StillLifeScene? HostScene,
    ModelGeometry? Geometry,
    string? SubjectToken,
    string? Error)
{
    public bool IsSuccess =>
        IsModelPrompt &&
        HostScene is not null &&
        Geometry is not null &&
        SubjectToken is not null &&
        Error is null;

    public static ModelSceneGenerationResult NotMatched { get; } = new(false, null, null, null, null);
}
