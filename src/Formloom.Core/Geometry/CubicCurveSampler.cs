using System.Collections.Immutable;
using System.Numerics;
using Formloom.Core.Core;

namespace Formloom.Core.Geometry;

/// <summary>Samples an ordered sequence of cubic segments using a normalized parameter.</summary>
public static class CubicCurveSampler
{
    public static CubicCurveSample Sample(ImmutableArray<CubicSegment> segments, float parameter)
    {
        if (segments.IsDefaultOrEmpty)
        {
            throw new ArgumentException("At least one curve segment is required.", nameof(segments));
        }

        if (!float.IsFinite(parameter) || parameter is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(parameter), "The sample parameter must be finite and in the range 0..1.");
        }

        var scaledParameter = parameter * segments.Length;
        var segmentIndex = parameter == 1
            ? segments.Length - 1
            : Math.Min((int)scaledParameter, segments.Length - 1);
        var localParameter = parameter == 1 ? 1 : scaledParameter - segmentIndex;
        var segment = segments[segmentIndex];
        if (!IsFinite(segment.Start) || !IsFinite(segment.Control1) ||
            !IsFinite(segment.Control2) || !IsFinite(segment.End))
        {
            throw new ArgumentException("Curve coordinates must be finite.", nameof(segments));
        }

        var inverse = 1 - localParameter;
        var tangent =
            (3 * inverse * inverse * (segment.Control1 - segment.Start)) +
            (6 * inverse * localParameter * (segment.Control2 - segment.Control1)) +
            (3 * localParameter * localParameter * (segment.End - segment.Control2));
        tangent = tangent.LengthSquared() <= 0.000000000001f
            ? Vector2.Zero
            : Vector2.Normalize(tangent);

        return new(segmentIndex, localParameter, segment.Evaluate(localParameter), tangent);
    }

    private static bool IsFinite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
}
