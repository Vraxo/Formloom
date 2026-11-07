using System.Collections.Immutable;
using System.Numerics;

namespace Formloom.Core;

// One petal of the daisy's radial bloom, in scene-normalized space. Angle is the direction the
// petal radiates in and Root/Tip are the ends the renderer shades between, both kept so tests
// can prove the arrangement stays radial. Tone is the petal's place in the bloom's value
// hierarchy: 0 is the darkest back petal, 1 the brightest front petal. Every actual colour
// lives in the renderer's material.
public sealed record DaisyPetal(
    ImmutableArray<CubicSegment> Outline,
    float Angle,
    float Tone,
    Vector2 Root,
    Vector2 Tip);
