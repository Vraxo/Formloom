using System.Collections.Immutable;
using System.Numerics;

namespace Formloom.Core.Core;

public static class CurveBuilder
{
    // Closed Catmull-Rom style smoothing that converts a perimeter point list into
    // cubic segments. Shared by the organic fruit contours and the vessel outlines.
    public static ImmutableArray<CubicSegment> ClosedCubic(Vector2[] points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Length < 3)
        {
            throw new ArgumentOutOfRangeException(nameof(points));
        }

        var segments = ImmutableArray.CreateBuilder<CubicSegment>(points.Length);
        for (var index = 0; index < points.Length; index++)
        {
            var previous = points[(index + points.Length - 1) % points.Length];
            var start = points[index];
            var end = points[(index + 1) % points.Length];
            var next = points[(index + 2) % points.Length];
            segments.Add(new(start, start + ((end - previous) / 6), end - ((next - start) / 6), end));
        }

        return segments.MoveToImmutable();
    }

    /// <summary>Creates connected Catmull-Rom style cubic segments from an ordered open point list.</summary>
    public static ImmutableArray<CubicSegment> OpenCubic(Vector2[] points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Length < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(points), "An open curve requires at least two points.");
        }

        foreach (var point in points)
        {
            if (!float.IsFinite(point.X) || !float.IsFinite(point.Y))
            {
                throw new ArgumentException("Open curve points must be finite.", nameof(points));
            }
        }

        var segments = ImmutableArray.CreateBuilder<CubicSegment>(points.Length - 1);
        for (var index = 0; index < points.Length - 1; index++)
        {
            var previous = index == 0 ? points[index] : points[index - 1];
            var start = points[index];
            var end = points[index + 1];
            var next = index + 2 < points.Length ? points[index + 2] : end;
            segments.Add(new(
                start,
                start + ((end - previous) / 6),
                end - ((next - start) / 6),
                end));
        }

        return segments.MoveToImmutable();
    }

    public static ProfileExtent Extent(ImmutableArray<CubicSegment> segments, int steps = 8)
    {
        var left = float.MaxValue;
        var top = float.MaxValue;
        var right = float.MinValue;
        var bottom = float.MinValue;
        foreach (var segment in segments)
        {
            for (var step = 0; step <= steps; step++)
            {
                var point = segment.Evaluate(step / (float)steps);
                left = Math.Min(left, point.X);
                top = Math.Min(top, point.Y);
                right = Math.Max(right, point.X);
                bottom = Math.Max(bottom, point.Y);
            }
        }

        return new(left, top, right, bottom);
    }
}
