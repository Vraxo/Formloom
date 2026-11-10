using System.Numerics;
using Formloom.Core.Core;
using Formloom.Core.Geometry;
using Xunit;

namespace Formloom.Tests.Geometry;

public sealed class ProfileMapperTests
{
    [Fact]
    public void MapsNormalizedProfileIntoAnOrientedScaledBasis()
    {
        Vector2[] profile = [new(0.75f, 0.5f)];
        var options = new ProfileMappingOptions(
            ScaleX: 2,
            ScaleY: 1,
            Length: 0.4f,
            Width: 0.2f,
            RootOffset: 0.1f,
            Bend: 0.15f,
            TransverseNormalization: 0.5f,
            ReverseLongitudinal: false);

        var result = ProfileMapper.Map(profile, new(0.2f, 0.3f), Vector2.UnitX, options);

        Assert.Single(result);
        Assert.Equal(0.8f, result[0].X, 5);
        Assert.Equal(0.5f, result[0].Y, 5);
    }

    [Fact]
    public void ReverseLongitudinalMapsProfileTipTowardItsOrigin()
    {
        Vector2[] profile = [new(0.5f, 0), new(0.5f, 1)];
        var options = new ProfileMappingOptions(1, 1, 0.4f, 0, 0, 0, 0.5f, true);

        var result = ProfileMapper.Map(profile, Vector2.Zero, Vector2.UnitY, options);

        Assert.Equal(new Vector2(0, 0.4f), result[0]);
        Assert.Equal(Vector2.Zero, result[1]);
    }

    [Fact]
    public void RejectsZeroDirectionAndInvalidNormalization()
    {
        Vector2[] profile = [Vector2.Zero];
        var options = new ProfileMappingOptions(1, 1, 1, 1, 0, 0, 0, false);

        Assert.Throws<ArgumentException>(() => ProfileMapper.Map(profile, Vector2.Zero, Vector2.Zero, options));
        Assert.Throws<ArgumentOutOfRangeException>(() => ProfileMapper.Map(
            profile, Vector2.Zero, Vector2.UnitX, options with { TransverseNormalization = 0 }));
    }
    [Fact]
    public void MapsCubicTemplateEndpointsAndControlsThroughTheSameTransform()
    {
        var segment = new CubicSegment(new(0.5f, 0), new(0.75f, 0.25f), new(0.75f, 0.75f), new(0.5f, 1));
        var origin = new Vector2(0.2f, 0.3f);
        var direction = Vector2.UnitY;
        var options = new ProfileMappingOptions(1, 1, 0.4f, 0.2f, 0.03f, 0.05f, 0.5f, false);
        var mapped = ProfileMapper.MapSegments([segment], origin, direction, options);
        var expected = ProfileMapper.Map([segment.Start, segment.Control1, segment.Control2, segment.End], origin, direction, options);

        var result = Assert.Single(mapped);
        Assert.Equal(expected[0], result.Start);
        Assert.Equal(expected[1], result.Control1);
        Assert.Equal(expected[2], result.Control2);
        Assert.Equal(expected[3], result.End);
    }

    [Fact]
    public void RejectsEmptyCubicTemplates()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ProfileMapper.MapSegments([], Vector2.Zero, Vector2.UnitX, new(1, 1, 1, 1, 0, 0, 0.5f, false)));
    }

}
