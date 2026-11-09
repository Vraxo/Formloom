namespace Formloom.Core.Scenes;

public sealed record GenerationRequest(
    string? Prompt,
    ulong Seed = 42,
    double Variation = 0.35,
    int Width = 1024,
    int Height = 1024);
