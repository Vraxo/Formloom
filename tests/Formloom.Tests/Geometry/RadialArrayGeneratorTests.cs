using Formloom.Core.Geometry;
using Xunit;

namespace Formloom.Tests.Geometry;

public sealed class RadialArrayGeneratorTests
{
    [Fact]
    public void GeneratesOrderedEvenlySpacedElements()
    {
        var elements = RadialArrayGenerator.Generate(
            4,
            -MathF.PI / 2,
            0,
            new(0, 0, 0, 0, 0),
            0,
            42,
            "flower.petals");

        Assert.Equal(4, elements.Length);
        Assert.Equal(-MathF.PI / 2, elements[0].Angle, 5);
        Assert.Equal(0, elements[1].Angle, 5);
        Assert.Equal(MathF.PI / 2, elements[2].Angle, 5);
        Assert.Equal(MathF.PI, elements[3].Angle, 5);
        Assert.All(elements, element =>
        {
            Assert.Equal(1, element.LengthScale);
            Assert.Equal(1, element.WidthScale);
            Assert.Equal(0, element.BendOffset);
            Assert.Equal(0, element.ToneOffset);
        });
    }

    [Fact]
    public void AppliesPhaseBeforeElementVariation()
    {
        var elements = RadialArrayGenerator.Generate(
            4,
            0,
            0.5f,
            new(0, 0, 0, 0, 0),
            0,
            42,
            "flower.petals");

        Assert.Equal(MathF.PI / 4, elements[0].Angle, 5);
        Assert.Equal(3 * MathF.PI / 4, elements[1].Angle, 5);
    }

    [Fact]
    public void VariationIsDeterministicPerElementStream()
    {
        var variation = new RadialArrayVariation(0.1f, 0.08f, 0.1f, 0.04f, 0.06f);
        var first = RadialArrayGenerator.Generate(5, 0, 0, variation, 0.8f, 1234, "outer");
        var second = RadialArrayGenerator.Generate(5, 0, 0, variation, 0.8f, 1234, "outer");

        Assert.Equal(first, second);
    }

    [Fact]
    public void ExistingElementVariationDoesNotDependOnTotalCount()
    {
        var variation = new RadialArrayVariation(0.1f, 0.08f, 0.1f, 0.04f, 0.06f);
        var three = RadialArrayGenerator.Generate(3, 0, 0, variation, 1, 99, "layer");
        var five = RadialArrayGenerator.Generate(5, 0, 0, variation, 1, 99, "layer");

        Assert.Equal(three[0].LengthScale, five[0].LengthScale);
        Assert.Equal(three[1].WidthScale, five[1].WidthScale);
        Assert.Equal(three[2].ToneOffset, five[2].ToneOffset);
    }

    [Fact]
    public void RejectsInvalidCountsAndVariationRanges()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RadialArrayGenerator.Generate(0, 0, 0, new(0, 0, 0, 0, 0), 0, 1, "parts"));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RadialArrayGenerator.Generate(3, 0, 0, new(0, 1.1f, 0, 0, 0), 1, 1, "parts"));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RadialArrayGenerator.Generate(3, 0, 0, new(0, 0, 0, 0, 0), 1.1f, 1, "parts"));
    }
}
