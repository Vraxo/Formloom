using Formloom.Core;
using Formloom.Core.Randomness;
using Xunit;

namespace Formloom.Tests;

public sealed class OrganicFormTests
{
    [Theory]
    [InlineData("a red apple on a wooden table", FruitKind.Apple, FruitColor.Red)]
    [InlineData("a green apple on a wooden table", FruitKind.Apple, FruitColor.Green)]
    [InlineData("a yellow apple on a wooden table", FruitKind.Apple, FruitColor.Yellow)]
    [InlineData(" A GREEN pear\ton a wooden table! ", FruitKind.Pear, FruitColor.Green)]
    [InlineData("a yellow pear on a wooden table", FruitKind.Pear, FruitColor.Yellow)]
    [InlineData("AN orange ON a wooden TABLE.", FruitKind.Orange, FruitColor.Orange)]
    public void SupportedPromptsHaveExplicitSubjectAndColor(string prompt, FruitKind kind, FruitColor color)
    {
        var parsed = PromptParser.ParseFruit(prompt);
        Assert.NotNull(parsed);
        Assert.Equal(kind, parsed.Kind);
        Assert.Equal(color, parsed.Color);
        var scene = SceneGenerator.Generate(new(prompt)).Scene!;
        Assert.Equal(kind, scene.Subject);
        Assert.Equal(color, scene.Color);
    }

    [Theory]
    [InlineData("a red pear on a wooden table")]
    [InlineData("a green orange on a wooden table")]
    [InlineData("a pear on a wooden table")]
    [InlineData("a juicy orange on a wooden table")]
    [InlineData("an orange under a wooden table")]
    [InlineData("an orange on a metal table")]
    [InlineData("a banana on a wooden table")]
    public void UnsupportedCombinationsAreNotSilentlyInterpreted(string prompt)
    {
        Assert.Null(PromptParser.ParseFruit(prompt));
        var result = SceneGenerator.Generate(new(prompt));
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Theory]
    [InlineData(FruitKind.Apple)]
    [InlineData(FruitKind.Pear)]
    [InlineData(FruitKind.Orange)]
    public void ContoursAreClosedFiniteBoundedAndRepeatable(FruitKind kind)
    {
        double[] variations = [0.0, 0.35, 1.0];
        for (ulong seed = 0; seed < 40; seed++)
        {
            foreach (var variation in variations)
            {
                var contour = OrganicForms.Create(kind, 0.025, seed, variation);
                var repeated = OrganicForms.Create(kind, 0.025, seed, variation);
                Assert.Equal(contour.Segments.ToArray(), repeated.Segments.ToArray());
                Assert.Equal(contour.Segments[0].Start, contour.Segments[^1].End);
                for (var index = 0; index < contour.Segments.Length; index++)
                {
                    var segment = contour.Segments[index];
                    Assert.Equal(segment.End, contour.Segments[(index + 1) % contour.Segments.Length].Start);
                    for (var sample = 0; sample <= 20; sample++)
                    {
                        var point = segment.Evaluate(sample / 20f);
                        Assert.True(float.IsFinite(point.X) && float.IsFinite(point.Y));
                        Assert.InRange(point.X, -0.01f, 1.01f);
                        Assert.InRange(point.Y, -0.01f, 1.01f);
                    }
                }
            }
        }
    }

    [Fact]
    public void PearHasNarrowNeckAndBroadLowerBodyAndDistinctProfiles()
    {
        var pear = OrganicForms.Create(FruitKind.Pear, 0, 42, 0);
        var points = pear.Segments.SelectMany(segment => Enumerable.Range(0, 31).Select(index => segment.Evaluate(index / 30f))).ToArray();
        var neck = points.Where(point => point.Y is > 0.12f and < 0.25f).ToArray();
        var belly = points.Where(point => point.Y is > 0.65f and < 0.8f).ToArray();
        var neckWidth = neck.Max(point => point.X) - neck.Min(point => point.X);
        var bellyWidth = belly.Max(point => point.X) - belly.Min(point => point.X);
        Assert.True(bellyWidth > neckWidth * 1.8f);
        Assert.NotEqual(pear.Segments.ToArray(), OrganicForms.Create(FruitKind.Orange, 0, 42, 0).Segments.ToArray());
        Assert.NotEqual(pear.Segments.ToArray(), OrganicForms.Create(FruitKind.Apple, 0, 42, 0).Segments.ToArray());
    }

    [Fact]
    public void NewDetailStreamsCannotShiftAppleAndTableValues()
    {
        var before = SceneGenerator.Generate(new("a red apple on a wooden table"));
        var appleRandom = Pcg32.ForStream(42, "apple.shape");
        var expected = Pcg32.ForStream(42, "apple.shape");
        for (var index = 0; index < 100; index++)
        {
            OrganicForms.Create(FruitKind.Orange, 0.02, (ulong)index, 1);
            OrganicForms.Create(FruitKind.Pear, 0.02, (ulong)index, 1);
            Assert.Equal(expected.NextUInt32(), appleRandom.NextUInt32());
        }

        Assert.Equal(before, SceneGenerator.Generate(new("a red apple on a wooden table")));
        var pearScene = SceneGenerator.Generate(new("a green pear on a wooden table")).Scene!;
        Assert.Equal(before.Scene!.GrainStrength, pearScene.GrainStrength);
        Assert.Equal(before.Scene.BackgroundWarmth, pearScene.BackgroundWarmth);
        Assert.NotEqual(
            OrganicForms.Create(FruitKind.Pear, 0.025, 42, 1).Segments.ToArray(),
            OrganicForms.Create(FruitKind.Pear, 0.025, 43, 1).Segments.ToArray());
    }
}
