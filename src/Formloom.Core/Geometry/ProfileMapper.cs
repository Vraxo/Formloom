using System.Collections.Immutable;
using System.Numerics;

namespace Formloom.Core.Geometry;

public static class ProfileMapper
{
    public static ImmutableArray<Vector2> Map(
        IReadOnlyList<Vector2> profile,
        Vector2 origin,
        Vector2 direction,
        ProfileMappingOptions options)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentOutOfRangeException.ThrowIfZero(profile.Count);

        if (!IsFinite(origin) || !IsFinite(direction) || direction.LengthSquared() <= 0.000000000001f)
        {
            throw new ArgumentException("Origin and direction must be finite, and direction must be non-zero.");
        }

        ValidateOptions(options);
        var unitDirection = Vector2.Normalize(direction);
        var normal = new Vector2(-unitDirection.Y, unitDirection.X);
        var scale = new Vector2(options.ScaleX, options.ScaleY);
        var result = ImmutableArray.CreateBuilder<Vector2>(profile.Count);

        foreach (var point in profile)
        {
            if (!IsFinite(point))
            {
                throw new ArgumentException("Profile coordinates must be finite.", nameof(profile));
            }

            var longitudinal = options.ReverseLongitudinal ? 1 - point.Y : point.Y;
            var along = options.RootOffset + (longitudinal * options.Length);
            var across = ((point.X - 0.5f) / options.TransverseNormalization * (options.Width / 2)) +
                (options.Bend * MathF.Sin(MathF.PI * longitudinal));
            var local = (unitDirection * along) + (normal * across);
            result.Add(origin + (local * scale));
        }

        return result.MoveToImmutable();
    }

    /// <summary>
    /// Maps authored cubic endpoints and controls using the profile transform. With nonlinear
    /// bending this transforms the control polygon rather than exactly remapping the Bézier curve.
    /// </summary>
    public static ImmutableArray<CubicSegment> MapSegments(
        IReadOnlyList<CubicSegment> segments,
        Vector2 origin,
        Vector2 direction,
        ProfileMappingOptions options)
    {
        ArgumentNullException.ThrowIfNull(segments);
        ArgumentOutOfRangeException.ThrowIfZero(segments.Count);

        var points = new Vector2[checked(segments.Count * 4)];
        for (var index = 0; index < segments.Count; index++)
        {
            var segment = segments[index] ??
                throw new ArgumentException("Cubic templates cannot contain null segments.", nameof(segments));
            var offset = index * 4;
            points[offset] = segment.Start;
            points[offset + 1] = segment.Control1;
            points[offset + 2] = segment.Control2;
            points[offset + 3] = segment.End;
        }

        var mapped = Map(points, origin, direction, options);
        var result = ImmutableArray.CreateBuilder<CubicSegment>(segments.Count);
        for (var index = 0; index < segments.Count; index++)
        {
            var offset = index * 4;
            result.Add(new(
                mapped[offset],
                mapped[offset + 1],
                mapped[offset + 2],
                mapped[offset + 3]));
        }

        return result.MoveToImmutable();
    }

    private static void ValidateOptions(ProfileMappingOptions options)
    {
        if (!float.IsFinite(options.ScaleX) ||
            !float.IsFinite(options.ScaleY) ||
            !float.IsFinite(options.Length) ||
            !float.IsFinite(options.Width) ||
            !float.IsFinite(options.RootOffset) ||
            !float.IsFinite(options.Bend) ||
            !float.IsFinite(options.TransverseNormalization))
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Profile mapping values must be finite.");
        }

        if (options.Length < 0 || options.Width < 0 || options.TransverseNormalization <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "Length and width must be non-negative, and transverse normalization must be positive.");
        }
    }

    private static bool IsFinite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
}
