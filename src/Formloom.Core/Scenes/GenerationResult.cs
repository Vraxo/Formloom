namespace Formloom.Core.Scenes;

public sealed record GenerationResult(StillLifeScene? Scene, string? Error)
{
    public bool IsSuccess => Scene is not null && Error is null;
}
