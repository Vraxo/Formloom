using System.Numerics;

namespace Formloom.Core.Ceramics;

public sealed record CeramicInstance(
    CeramicKind Kind,
    CeramicGlaze Glaze,
    CeramicDescription Body,
    ulong Seed,
    int Depth);
