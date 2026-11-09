using System.Collections.Immutable;
using System.Numerics;
using Formloom.Core.Core;
using Formloom.Core.Geometry;
using Xunit;

namespace Formloom.Tests.Geometry;

public sealed class CubicCurveProjectorTests
{
    [Fact]
    public void FindsNearestPointAndUnitTangentOnStraightCubic()
    {
        var segment = new CubicSegment(
            Vector2.Zero,
            new(1f / 3, 0),
            new(2f / 3, 0),
            Vector2.UnitX);

        var result = CubicCurveProjector.ProjectNearest([segment], new(0.35f, 1));

        Assert.Equal(0, result.SegmentIndex);
        Assert.Equal(0.35f, result.Point.X, 4);
        Assert.Equal(0, result.Point.Y, 5);
        Assert.Equal(Vector2.UnitX, result.Tangent);
        Assert.Equal(1, result.DistanceSquared, 4);
    }

    [Fact]
    public void SearchesAllSegmentsAndReturnsWinningSegmentIndex()
    {
        var segments = ImmutableArray.Create(
            new CubicSegment(new(0, 0), new(0.3f, 0), new(0.7f, 0), new(1, 0)),
            new CubicSegment(new(0, 2), new(0.3f, 2), new(0.7f, 2), new(1, 2)));

        var result = CubicCurveProjector.ProjectNearest(segments, new(0.5f, 1.8f));

        Assert.Equal(1, result.SegmentIndex);
        Assert.Equal(2, result.Point.Y, 4);
    }

    [Fact]
    public void RejectsEmptyCurvesAndNonFiniteTargets()
    {
        Assert.Throws<ArgumentException>(() =>
            CubicCurveProjector.ProjectNearest(ImmutableArray<CubicSegment>.Empty, Vector2.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CubicCurveProjector.ProjectNearest(
                [new(Vector2.Zero, Vector2.Zero, Vector2.One, Vector2.One)],
                new(float.NaN, 0)));
    }
}
