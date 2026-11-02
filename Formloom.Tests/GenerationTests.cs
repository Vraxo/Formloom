using Formloom.Core;
using Formloom.Core.Randomness;
using Xunit;

namespace Formloom.Tests;

public sealed class GenerationTests
{
    [Theory]
    [InlineData("a red apple on a wooden table", AppleColor.Red)]
    [InlineData(" A  GREEN\tapple on a wooden table! ", AppleColor.Green)]
    [InlineData("a yellow apple on a wooden table.", AppleColor.Yellow)]
    public void SupportedPromptsParse(string prompt, AppleColor expected)
    {
        Assert.Equal(expected, PromptParser.Parse(prompt));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("a blue apple on a wooden table")]
    [InlineData("a red apple on a metal table")]
    [InlineData("a red apple under a wooden table")]
    [InlineData("a red apple on a wooden table with a pear")]
    public void UnsupportedPromptsReturnAnError(string? prompt)
    {
        var result = SceneGenerator.Generate(new(prompt));
        Assert.False(result.IsSuccess);
        Assert.Null(result.Scene);
        Assert.NotEmpty(result.Error!);
    }

    [Theory]
    [InlineData(0, 1024, 0.5)]
    [InlineData(1024, 4097, 0.5)]
    [InlineData(1024, 1024, -0.1)]
    [InlineData(1024, 1024, 1.1)]
    [InlineData(1024, 1024, double.NaN)]
    [InlineData(1024, 1024, double.PositiveInfinity)]
    public void InvalidRequestsReturnAnError(int width, int height, double variation)
    {
        var result = SceneGenerator.Generate(new("a red apple on a wooden table", Width: width, Height: height, Variation: variation));
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void SceneIsRepeatableAndScalesWithoutChangingComposition()
    {
        var request = new GenerationRequest("a red apple on a wooden table");
        var first = SceneGenerator.Generate(request);
        Assert.True(first.IsSuccess);
        Assert.Equal(first, SceneGenerator.Generate(request));
        var smaller = SceneGenerator.Generate(request with { Width = 512, Height = 512 });
        Assert.Equal(first.Scene!.Apple, smaller.Scene!.Apple);
        Assert.NotEqual(first.Scene.Apple, SceneGenerator.Generate(request with { Seed = 43 }).Scene!.Apple);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void VariationKeepsAppleOnTableAndInsideCanvas(double variation)
    {
        for (ulong seed = 0; seed < 100; seed++)
        {
            var scene = SceneGenerator.Generate(new("a red apple on a wooden table", seed, variation)).Scene!;
            Assert.InRange(scene.Apple.Size.X, 0.38f, 0.48f);
            Assert.InRange(scene.Apple.Size.Y, 0.40f, 0.48f);
            Assert.InRange(scene.Apple.Center.X - (scene.Apple.Size.X / 2), 0.1f, 0.4f);
            Assert.True(scene.Apple.Center.Y + (scene.Apple.Size.Y / 2) > scene.TableHorizon);
        }
    }

    [Fact]
    public void VariationControlsDeviationFromBaseShape()
    {
        var request = new GenerationRequest("a red apple on a wooden table", Variation: 0);
        var baseline = SceneGenerator.Generate(request).Scene!.Apple;
        var low = SceneGenerator.Generate(request with { Variation = 0.25 }).Scene!.Apple;
        var high = SceneGenerator.Generate(request with { Variation = 1 }).Scene!.Apple;
        Assert.True(Math.Abs(low.Asymmetry - baseline.Asymmetry) < Math.Abs(high.Asymmetry - baseline.Asymmetry));
        Assert.Equal(baseline, SceneGenerator.Generate(request with { Seed = 99 }).Scene!.Apple);
    }

    [Fact]
    public void PcgMatchesReferenceSequence()
    {
        var random = new Pcg32(42, 54);
        uint[] expected = [0xa15c02b7, 0x7b47f409, 0xba1d3330, 0x83d2f293, 0xbfa4784b];
        foreach (var value in expected)
        {
            Assert.Equal(value, random.NextUInt32());
        }
    }

    [Fact]
    public void NamedStreamsAreRepeatableDistinctAndIndependent()
    {
        var shape = Pcg32.ForStream(42, "apple.shape");
        var repeated = Pcg32.ForStream(42, "apple.shape");
        var surface = Pcg32.ForStream(42, "apple.surface");
        Assert.NotEqual(Pcg32.ForStream(43, "apple.shape").NextUInt32(), repeated.NextUInt32());
        repeated = Pcg32.ForStream(42, "apple.shape");
        Assert.NotEqual(surface.NextUInt32(), Pcg32.ForStream(42, "apple.shape").NextUInt32());
        for (var index = 0; index < 100; index++)
        {
            surface.NextUInt32();
            Assert.Equal(shape.NextUInt32(), repeated.NextUInt32());
        }
    }

    [Fact]
    public void RandomHelpersRespectBounds()
    {
        var random = new Pcg32(42);
        for (var index = 0; index < 1000; index++)
        {
            Assert.InRange(random.NextInt(-10, 10), -10, 9);
            Assert.InRange(random.NextDouble(), 0, Math.BitDecrement(1.0));
            Assert.Equal(7, random.NextInt(7, 8));
            Assert.False(random.Chance(0));
            Assert.True(random.Chance(1));
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => random.NextInt(1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => random.Chance(double.NaN));
    }
}
