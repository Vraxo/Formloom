using System.Collections.Immutable;
using System.Numerics;
using Formloom.Core.Core;

namespace Formloom.Core.Geometry;

public static class CubicCurveProjector
{
    private const int RefinementIterations = 20;

    public static CubicCurveProjection ProjectNearest(
        ImmutableArray<CubicSegment> segments,
        Vector2 target,
        int samplesPerSegment = 64)
    {
        if (segments.IsDefaultOrEmpty)
        {
            throw new ArgumentException("At least one curve segment is required.", nameof(segments));
        }

        if (!IsFinite(target))
        {
            throw new ArgumentOutOfRangeException(nameof(target), "Target coordinates must be finite.");
        }

        if (samplesPerSegment < 4)
        {
            throw new ArgumentOutOfRangeException(nameof(samplesPerSegment), "At least four samples per segment are required.");
        }

        var bestProjection = default(CubicCurveProjection);
        var bestDistance = double.PositiveInfinity;
        for (var segmentIndex = 0; segmentIndex < segments.Length; segmentIndex++)
        {
            var segment = segments[segmentIndex] ??
                throw new ArgumentException("Curve segments cannot contain null entries.", nameof(segments));
            if (!IsFinite(segment.Start) || !IsFinite(segment.Control1) ||
                !IsFinite(segment.Control2) || !IsFinite(segment.End))
            {
                throw new ArgumentException("Curve coordinates must be finite.", nameof(segments));
            }

            var sampleStep = 1f / samplesPerSegment;
            var bestSample = 0f;
            var bestSampleDistance = double.PositiveInfinity;
            for (var sample = 0; sample <= samplesPerSegment; sample++)
            {
                var parameter = sample * sampleStep;
                var distance = DistanceSquared(segment, target, parameter);
                if (distance < bestSampleDistance)
                {
                    bestSampleDistance = distance;
                    bestSample = parameter;
                }
            }

            var lower = MathF.Max(0, bestSample - sampleStep);
            var upper = MathF.Min(1, bestSample + sampleStep);
            var parameterResult = Refine(segment, target, lower, upper);
            var point = segment.Evaluate(parameterResult);
            var distanceSquared = DistanceSquared(segment, target, parameterResult);
            if (distanceSquared >= bestDistance)
            {
                continue;
            }

            bestDistance = distanceSquared;
            bestProjection = new(
                segmentIndex,
                parameterResult,
                point,
                TangentAt(segment, parameterResult),
                (float)distanceSquared);
        }

        return bestProjection;
    }

    /// <summary>Projects to the nearest sampled point on one selected coordinate.</summary>
    public static CubicCurveAxisProjection ProjectNearestOnAxis(
        ImmutableArray<CubicSegment> segments,
        CurveProjectionAxis axis,
        float coordinate,
        int samplesPerSegment = 64)
    {
        if (segments.IsDefaultOrEmpty)
        {
            throw new ArgumentException("At least one curve segment is required.", nameof(segments));
        }

        if (!Enum.IsDefined(axis))
        {
            throw new ArgumentOutOfRangeException(nameof(axis), "The projection axis is not supported.");
        }

        if (!float.IsFinite(coordinate))
        {
            throw new ArgumentOutOfRangeException(nameof(coordinate), "The target coordinate must be finite.");
        }

        if (samplesPerSegment < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(samplesPerSegment), "At least one sample per segment is required.");
        }

        var bestProjection = default(CubicCurveAxisProjection);
        var bestDistance = double.PositiveInfinity;
        for (var segmentIndex = 0; segmentIndex < segments.Length; segmentIndex++)
        {
            var segment = segments[segmentIndex] ??
                throw new ArgumentException("Curve segments cannot contain null entries.", nameof(segments));
            if (!IsFinite(segment.Start) || !IsFinite(segment.Control1) ||
                !IsFinite(segment.Control2) || !IsFinite(segment.End))
            {
                throw new ArgumentException("Curve coordinates must be finite.", nameof(segments));
            }

            for (var sample = 0; sample <= samplesPerSegment; sample++)
            {
                var parameter = sample / (float)samplesPerSegment;
                var point = segment.Evaluate(parameter);
                if (!IsFinite(point))
                {
                    throw new ArgumentException("Curve evaluation produced a non-finite coordinate.", nameof(segments));
                }

                var value = axis == CurveProjectionAxis.X ? point.X : point.Y;
                var distance = Math.Abs((double)value - coordinate);
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                bestProjection = new(
                    segmentIndex,
                    parameter,
                    point,
                    TangentAt(segment, parameter),
                    (float)Math.Min(distance, float.MaxValue));
            }
        }

        return bestProjection;
    }

    private static float Refine(CubicSegment segment, Vector2 target, float lower, float upper)
    {
        const float goldenRatio = 0.618033989f;
        var left = upper - ((upper - lower) * goldenRatio);
        var right = lower + ((upper - lower) * goldenRatio);
        var leftDistance = DistanceSquared(segment, target, left);
        var rightDistance = DistanceSquared(segment, target, right);

        for (var iteration = 0; iteration < RefinementIterations; iteration++)
        {
            if (leftDistance <= rightDistance)
            {
                upper = right;
                right = left;
                rightDistance = leftDistance;
                left = upper - ((upper - lower) * goldenRatio);
                leftDistance = DistanceSquared(segment, target, left);
            }
            else
            {
                lower = left;
                left = right;
                leftDistance = rightDistance;
                right = lower + ((upper - lower) * goldenRatio);
                rightDistance = DistanceSquared(segment, target, right);
            }
        }

        return (lower + upper) / 2;
    }

    private static double DistanceSquared(CubicSegment segment, Vector2 target, float parameter)
    {
        var point = segment.Evaluate(parameter);
        var deltaX = (double)point.X - target.X;
        var deltaY = (double)point.Y - target.Y;
        return (deltaX * deltaX) + (deltaY * deltaY);
    }

    private static Vector2 TangentAt(CubicSegment segment, float parameter)
    {
        var inverse = 1 - parameter;
        var tangent =
            (3 * inverse * inverse * (segment.Control1 - segment.Start)) +
            (6 * inverse * parameter * (segment.Control2 - segment.Control1)) +
            (3 * parameter * parameter * (segment.End - segment.Control2));
        return tangent.LengthSquared() <= 0.000000000001f ? Vector2.Zero : Vector2.Normalize(tangent);
    }

    private static bool IsFinite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
}
