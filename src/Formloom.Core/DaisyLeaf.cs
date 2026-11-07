using System.Collections.Immutable;
using System.Numerics;

namespace Formloom.Core;

// One spatulate leaf of the daisy in scene-normalized space. Attachment sits exactly on the
// stem curve, Tip carries the orientation the tests care about, and Outline/Vein are what the
// renderer fills and strokes.
public sealed record DaisyLeaf(
    Vector2 Attachment,
    Vector2 Tip,
    float Width,
    ImmutableArray<CubicSegment> Outline,
    ImmutableArray<CubicSegment> Vein);
