using System.Numerics;
using Formloom.Core.Core;
using Xunit;

namespace Formloom.Tests.Geometry;

public sealed class CurveBuilderTests
{
    [Fact]
    public void BuildsConnectedOpenCubicSegmentsThroughOrderedPoints()
    {
        Vector2[] points = [new(0, 0), new(1, 1), new(2, 0)];
        var segments = CurveBuilder.OpenCubic(points);
        Assert.Equal(2, segments.Length);
        Assert.Equal(points[0], segments[0].Start);
        Assert.Equal(points[1], segments[0].End);
        Assert.Equal(segments[0].End, segments[1].Start);
        Assert.Equal(points[2], segments[1].End);
        Assert.True(Vector2.Distance(segments[0].End - segments[0].Control2, segments[1].Control1 - segments[1].Start) < 0.00001f);
    }

    [Fact]
    public void TwoPointOpenCubicIsACollinearSegment()
    {
        Vector2[] points = [new(0, 0), new(3, 6)];
        var segment = Assert.Single(CurveBuilder.OpenCubic(points));
        Assert.Equal(points[0], segment.Start);
        Assert.Equal(points[1], segment.End);
        Assert.Equal(new Vector2(0.5f, 1), segment.Control1);
        Assert.Equal(new Vector2(2.5f, 5), segment.Control2);
    }

    [Fact]
    public void RejectsTooFewOrNonFiniteOpenCurvePoints()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CurveBuilder.OpenCubic([Vector2.Zero]));
        Assert.Throws<ArgumentException>(() => CurveBuilder.OpenCubic([Vector2.Zero, new(float.NaN, 0)]));
    }
}
