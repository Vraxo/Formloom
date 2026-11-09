using System.Collections.Immutable;

namespace Formloom.Core.Models.Definitions;

public sealed record ModelProfileDefinition(
    bool Closed,
    ModelProfileInterpolation Interpolation,
    ImmutableArray<ModelPoint> Points);