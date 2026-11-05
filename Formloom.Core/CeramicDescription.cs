using System.Numerics;

namespace Formloom.Core;

public sealed record CeramicDescription(
    Vector2 Center,
    Vector2 Size,
    double SurfaceVariation);
