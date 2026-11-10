using System.Numerics;
using Formloom.Core.Core;
using Xunit;

namespace Formloom.Tests.Geometry;

public sealed class CurveBuilderTests
{
    [Fact]
    public void OpenCubicPassesThroughEveryPointAndKeepsThePathOpen()
    {
        Vector2[] points = [new(0, 0), new(1, 1), new(2, 0)];

        var segments = CurveBuilder.OpenCubic(points);

        Assert.Equal(points.Length - 1, segments.Length);
        for (var index = 0; index < segments.Length; index++)
        {
            Assert.Equal(points[index], segments[index].Start);
            Assert.Equal(points[index + 1], segments[index].End);
            if (index > 0)
            {
                Assert.Equal(segments[index - 1].End, segments[index].Start);
            }
        }

        Assert.NotEqual(segments[0].Start, segments[0].Control1);
        Assert.NotEqual(segments[0].End, segments[0].Control2);
        Assert.NotEqual(segments[^1].End, segments[0].Start);
    }

    [Fact]
    public void OpenCubicWithTwoPointsProducesStraightCubicSegment()
    {
        Vector2 start = new(1, 2);
        Vector2 end = new(4, 8);

        var segment = Assert.Single(CurveBuilder.OpenCubic([start, end]));

        Assert.Equal(start, segment.Start);
        Assert.Equal(end, segment.End);
        Assert.Equal(new Vector2(2.5f, 5), segment.Evaluate(0.5f));
    }

    [Fact]
    public void OpenCubicRejectsNullAndInsufficientPoints()
    {
        Assert.Throws<ArgumentNullException>(() => CurveBuilder.OpenCubic(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => CurveBuilder.OpenCubic([]));
        Assert.Throws<ArgumentOutOfRangeException>(() => CurveBuilder.OpenCubic([Vector2.Zero]));
    }
}
