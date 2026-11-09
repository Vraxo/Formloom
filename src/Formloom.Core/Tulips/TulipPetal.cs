using System.Collections.Immutable;
using Formloom.Core.Core;

namespace Formloom.Core.Tulips;

// One petal contour in scene-normalized space plus its place in the bloom's value
// hierarchy: 0 is the darkest back petal, 1 the brightest front petal. Core carries the
// tone as a number; every actual petal colour lives in the renderer's material.
public sealed record TulipPetal(ImmutableArray<CubicSegment> Outline, float Tone);
