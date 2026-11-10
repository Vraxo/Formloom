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
    [Fact]
    public void ProjectsToTheNearestSampleOnTheRequestedYCoordinate()
    {
        var segment = new CubicSegment(Vector2.Zero, new(1f / 3, 1f / 3), new(2f / 3, 2f / 3), Vector2.One);
        var result = CubicCurveProjector.ProjectNearestOnAxis([segment], CurveProjectionAxis.Y, 0.52f);
        var expectedParameter = 33f / 64;
        var expectedPoint = segment.Evaluate(expectedParameter);

        Assert.Equal(0, result.SegmentIndex);
        Assert.Equal(expectedParameter, result.Parameter);
        Assert.Equal(expectedPoint, result.Point);
        Assert.Equal(MathF.Abs(expectedPoint.Y - 0.52f), result.CoordinateDistance, 6);
    }

    [Fact]
    public void SearchesAllSegmentsForTheNearestXCoordinate()
    {
        var segments = ImmutableArray.Create(
            new CubicSegment(new(0, 0), new(0, 1f / 3), new(0, 2f / 3), new(0, 1)),
            new CubicSegment(new(2, 0), new(2, 1f / 3), new(2, 2f / 3), new(2, 1)));
        var result = CubicCurveProjector.ProjectNearestOnAxis(segments, CurveProjectionAxis.X, 1.8f);

        Assert.Equal(1, result.SegmentIndex);
        Assert.Equal(new Vector2(2, 0), result.Point);
        Assert.Equal(0.2f, result.CoordinateDistance, 5);
    }

    [Fact]
    public void RejectsInvalidAxisProjectionInputs()
    {
        var segment = new CubicSegment(Vector2.Zero, Vector2.Zero, Vector2.One, Vector2.One);
        Assert.Throws<ArgumentException>(() => CubicCurveProjector.ProjectNearestOnAxis(ImmutableArray<CubicSegment>.Empty, CurveProjectionAxis.Y, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => CubicCurveProjector.ProjectNearestOnAxis([segment], (CurveProjectionAxis)99, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => CubicCurveProjector.ProjectNearestOnAxis([segment], CurveProjectionAxis.Y, float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => CubicCurveProjector.ProjectNearestOnAxis([segment], CurveProjectionAxis.Y, 0, 0));
    }

}
