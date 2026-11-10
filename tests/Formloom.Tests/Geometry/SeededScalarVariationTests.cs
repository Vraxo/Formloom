using Formloom.Core.Geometry;
using Xunit;

namespace Formloom.Tests.Geometry;

public sealed class SeededScalarVariationTests
{
    [Fact]
    public void ProducesRepeatableOffsetsInsideTheRequestedRange()
    {
        var first = SeededScalarVariation.SampleOffset(0.24f, 0.75f, 42, "rose/stem/center-x");
        var second = SeededScalarVariation.SampleOffset(0.24f, 0.75f, 42, "rose/stem/center-x");
        Assert.Equal(first, second);
        Assert.InRange(first, -0.18f, 0.18f);
    }

    [Fact]
    public void ZeroVariationAmountProducesNoOffset()
    {
        Assert.Equal(0, SeededScalarVariation.SampleOffset(10, 0, ulong.MaxValue, "rose/leaf/left/angle"));
    }

    [Fact]
    public void SamplingOtherNamedStreamsDoesNotPerturbAnExistingStream()
    {
        var before = SeededScalarVariation.SampleOffset(0.2f, 1, 99, "rose/leaf/left/angle");
        _ = SeededScalarVariation.SampleOffset(0.4f, 1, 99, "rose/leaf/right/angle");
        var after = SeededScalarVariation.SampleOffset(0.2f, 1, 99, "rose/leaf/left/angle");
        Assert.Equal(before, after);
    }

    [Fact]
    public void RejectsInvalidRangesAmountsAndStreamNames()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SeededScalarVariation.SampleOffset(-0.1f, 1, 99, "invalid"));
        Assert.Throws<ArgumentOutOfRangeException>(() => SeededScalarVariation.SampleOffset(float.NaN, 1, 99, "invalid"));
        Assert.Throws<ArgumentOutOfRangeException>(() => SeededScalarVariation.SampleOffset(0.1f, float.NaN, 99, "invalid"));
        Assert.Throws<ArgumentOutOfRangeException>(() => SeededScalarVariation.SampleOffset(0.1f, 1.1f, 99, "invalid"));
        Assert.Throws<ArgumentException>(() => SeededScalarVariation.SampleOffset(0.1f, 1, 99, " "));
    }
}
