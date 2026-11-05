using System.Numerics;

namespace Formloom.Core;

public sealed record CeramicInstance(
    CeramicKind Kind,
    CeramicGlaze Glaze,
    CeramicDescription Body,
    ulong Seed,
    int Depth);
