using System.Collections.Immutable;
using System.Numerics;

namespace Formloom.Core;

// One lance-shaped leaf in scene-normalized space. Attachment sits exactly on the stem
// curve, Tip carries the orientation tests care about, and Outline/Vein are what the
// renderer fills and strokes.
public sealed record TulipLeaf(
    Vector2 Attachment,
    Vector2 Tip,
    float Width,
    ImmutableArray<CubicSegment> Outline,
    ImmutableArray<CubicSegment> Vein);
