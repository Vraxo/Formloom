using System.Collections.Immutable;
using System.Numerics;
using Formloom.Core.Core;

namespace Formloom.Core.Roses;

/// <summary>One serrated rose leaf attached to the continuous stem.</summary>
public sealed record RoseLeaf(
    Vector2 Attachment,
    Vector2 Tip,
    float Width,
    ImmutableArray<CubicSegment> Outline,
    ImmutableArray<CubicSegment> Vein);
