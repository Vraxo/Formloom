using System.Numerics;

namespace Formloom.Core.Ceramics;

public sealed record CeramicDescription(
    Vector2 Center,
    Vector2 Size,
    double SurfaceVariation);
