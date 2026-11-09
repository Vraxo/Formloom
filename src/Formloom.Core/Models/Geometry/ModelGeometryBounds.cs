using System.Numerics;

namespace Formloom.Core.Models.Geometry;

public readonly record struct ModelGeometryBounds(float Left, float Top, float Right, float Bottom)
{
    public Vector2 Size => new(Right - Left, Bottom - Top);

    public static ModelGeometryBounds From(ModelGeometryPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        var left = float.MaxValue;
        var top = float.MaxValue;
        var right = float.MinValue;
        var bottom = float.MinValue;
        foreach (var segment in path.Segments)
        {
            Include(segment.Start);
            Include(segment.Control1);
            Include(segment.Control2);
            Include(segment.End);
        }

        return new(left, top, right, bottom);

        void Include(Vector2 point)
        {
            left = MathF.Min(left, point.X);
            top = MathF.Min(top, point.Y);
            right = MathF.Max(right, point.X);
            bottom = MathF.Max(bottom, point.Y);
        }
    }

    public static ModelGeometryBounds Union(ModelGeometryBounds first, ModelGeometryBounds second) =>
        new(
            MathF.Min(first.Left, second.Left),
            MathF.Min(first.Top, second.Top),
            MathF.Max(first.Right, second.Right),
            MathF.Max(first.Bottom, second.Bottom));
}
