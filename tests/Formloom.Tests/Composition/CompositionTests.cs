using System.Collections.Immutable;
using System.Numerics;
using Formloom.Core.Composition;
using Formloom.Core.Fruits;
using Formloom.Core.Scenes;
using Xunit;

namespace Formloom.Tests.Composition;

public sealed class CompositionTests
{
    [Theory]
    [InlineData("a red apple and a green pear on a wooden table", "Apple,Pear", "Red,Green")]
    [InlineData("a yellow pear and an orange on a wooden table", "Pear,Orange", "Yellow,Orange")]
    [InlineData("an orange and a yellow apple on a wooden table", "Orange,Apple", "Orange,Yellow")]
    [InlineData("a red apple, a green pear, and an orange on a wooden table", "Apple,Pear,Orange", "Red,Green,Orange")]
    [InlineData(" A YELLOW APPLE , a yellow pear and AN ORANGE on a wooden table! ", "Apple,Pear,Orange", "Yellow,Yellow,Orange")]
    [InlineData("an orange and a green pear and a green apple on a wooden table", "Orange,Pear,Apple", "Orange,Green,Green")]
    [InlineData("a red apple and a red apple on a wooden table", "Apple,Apple", "Red,Red")]
    public void ListsPreserveSubjectAndColorOrder(string prompt, string subjects, string colors)
    {
        var parsed = PromptParser.ParseScene(prompt);
        Assert.NotNull(parsed);
        Assert.Equal(subjects, string.Join(",", parsed.Value.Select(fruit => fruit.Kind)));
        Assert.Equal(colors, string.Join(",", parsed.Value.Select(fruit => fruit.Color)));
        var result = SceneGenerator.Generate(new(prompt));
        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(subjects, string.Join(",", result.Scene!.Fruits.Select(fruit => fruit.Subject)));
        Assert.Equal(colors, string.Join(",", result.Scene.Fruits.Select(fruit => fruit.Color)));
        Assert.Null(PromptParser.ParseFruit(prompt));
    }

    [Theory]
    [InlineData("a red apple, an orange on a wooden table")]
    [InlineData("a red apple and and an orange on a wooden table")]
    [InlineData("a red apple or an orange on a wooden table")]
    [InlineData("a red apple, a green pear, an orange on a wooden table")]
    [InlineData("a red apple and an orange and a yellow pear and a green apple on a wooden table")]
    [InlineData("a red pear and an orange on a wooden table")]
    [InlineData("two apples on a wooden table")]
    [InlineData("a shiny red apple and an orange on a wooden table")]
    [InlineData("an orange and on a wooden table")]
    [InlineData("a red apple,, a green pear and an orange on a wooden table")]
    public void InvalidListsFailWithoutDroppingSubjects(string prompt)
    {
        Assert.Null(PromptParser.ParseScene(prompt));
        var result = SceneGenerator.Generate(new(prompt));
        Assert.False(result.IsSuccess);
        Assert.Null(result.Scene);
        Assert.NotEmpty(result.Error!);
    }

    [Theory]
    [InlineData("a red apple and a green pear on a wooden table")]
    [InlineData("a green pear and an orange on a wooden table")]
    [InlineData("a red apple, a green pear, and an orange on a wooden table")]
    [InlineData("a yellow pear, a green pear, and a yellow pear on a wooden table")]
    public void LayoutIsBoundedRepeatableGroundedAndReadable(string prompt)
    {
        double[] variations = [0, 0.35, 1];
        for (ulong seed = 0; seed < 100; seed++)
        {
            foreach (var variation in variations)
            {
                var request = new GenerationRequest(prompt, seed, variation);
                var result = SceneGenerator.Generate(request);
                Assert.True(result.IsSuccess, result.Error);
                var scene = result.Scene!;
                Assert.Null(SceneValidator.Validate(scene));
                Assert.Equal(scene.Fruits.ToArray(), SceneGenerator.Generate(request).Scene!.Fruits.ToArray());
                Assert.Equal(PromptParser.ParseScene(prompt)!.Value.Length, scene.Fruits.Length);
                foreach (var fruit in scene.Fruits)
                {
                    var bounds = FruitBounds.For(fruit);
                    Assert.InRange(bounds.Left, 0.03f, 0.97f);
                    Assert.InRange(bounds.Right, 0.03f, 0.97f);
                    Assert.InRange(bounds.Top, 0.03f, 0.94f);
                    Assert.InRange(bounds.Bottom, 0.64f, 0.94f);
                    var bottom = fruit.Body.Center.Y + (fruit.Body.Size.Y / 2);
                    Assert.InRange(bottom, 0.73f, 0.80f);
                    foreach (var other in scene.Fruits.Where(other => other != fruit))
                    {
                        var separation = Math.Abs(fruit.Body.Center.X - other.Body.Center.X);
                        Assert.True(separation > 0.23f);
                        var overlap = Math.Max(0, ((fruit.Body.Size.X + other.Body.Size.X) / 2) - separation);
                        Assert.True(overlap < Math.Min(fruit.Body.Size.X, other.Body.Size.X) * 0.3f);
                    }
                }
            }
        }
    }

    [Fact]
    public void SingleFruitCompatibilityAndInstanceStreamsRemainStable()
    {
        var single = SceneGenerator.Generate(new("a red apple on a wooden table")).Scene!;
        var pair = SceneGenerator.Generate(new("a red apple and a green pear on a wooden table")).Scene!;
        var reversed = SceneGenerator.Generate(new("a green pear and a red apple on a wooden table")).Scene!;
        var triple = SceneGenerator.Generate(new("an orange, a red apple, and a green pear on a wooden table")).Scene!;
        Assert.Null(single.Instances);
        Assert.Equal(single.Body, single.Fruits[0].Body);
        Assert.Equal(single.Fruits.ToArray(), FruitComposition.Arrange(single.Fruits, 99, 1).ToArray());
        var apple = pair.Fruits[0];
        Assert.Equal(apple.Seed, reversed.Fruits[1].Seed);
        Assert.Equal(apple.Seed, triple.Fruits[1].Seed);
        Assert.Equal(single.Body.Asymmetry, apple.Body.Asymmetry);
        Assert.Equal(apple.Body.Asymmetry, triple.Fruits[1].Body.Asymmetry);
        Assert.Equal(apple.Body.StemLean, reversed.Fruits[1].Body.StemLean);
        Assert.Equal(apple.Body.LeafAngle, reversed.Fruits[1].Body.LeafAngle);
        Assert.Equal(apple.Body.SurfaceVariation, reversed.Fruits[1].Body.SurfaceVariation);
        Assert.Equal(single.GrainStrength, triple.GrainStrength);
        Assert.Equal(single.BackgroundWarmth, triple.BackgroundWarmth);
        Assert.Equal(
            OrganicForms.Create(apple.Subject, apple.Body.Asymmetry, apple.Seed, pair.Variation).Segments.ToArray(),
            OrganicForms.Create(apple.Subject, triple.Fruits[1].Body.Asymmetry, triple.Fruits[1].Seed, triple.Variation).Segments.ToArray());
        var duplicates = SceneGenerator.Generate(new("a red apple and a red apple on a wooden table")).Scene!;
        Assert.NotEqual(duplicates.Fruits[0].Seed, duplicates.Fruits[1].Seed);
    }

    [Fact]
    public void DifferentSeedsChangeArrangementWithoutChangingSubjects()
    {
        const string prompt = "a red apple, a green pear, and an orange on a wooden table";
        var scenes = Enumerable.Range(0, 16).Select(seed => SceneGenerator.Generate(new(prompt, (ulong)seed)).Scene!).ToArray();
        Assert.True(scenes.Select(scene => string.Join(",", scene.Fruits.Select(fruit => fruit.Depth))).Distinct().Count() > 1);
        foreach (var scene in scenes)
        {
            Assert.Equal(new[] { FruitKind.Apple, FruitKind.Pear, FruitKind.Orange }, scene.Fruits.Select(fruit => fruit.Subject).ToArray());
        }
    }

    [Fact]
    public void InvalidCollectionsTransformsAndAppearanceHaveUsefulErrors()
    {
        var scene = SceneGenerator.Generate(new("a red apple on a wooden table")).Scene!;
        var fruit = scene.Fruits[0];
        Assert.NotNull(SceneValidator.Validate(scene with { Instances = ImmutableArray<FruitInstance>.Empty }));
        Assert.NotNull(SceneValidator.Validate(scene with { Instances = default(ImmutableArray<FruitInstance>) }));
        Assert.NotNull(SceneValidator.Validate(scene with { Instances = ImmutableArray.Create(fruit, fruit, fruit, fruit) }));
        Vector2[] invalidCenters = [new(float.NaN, 0.5f), new(float.PositiveInfinity, 0.5f), new(-1, 0.5f), new(1, 0.5f)];
        foreach (var center in invalidCenters)
        {
            Assert.NotNull(SceneValidator.Validate(scene with { Instances = ImmutableArray.Create(fruit with { Body = fruit.Body with { Center = center } }) }));
        }

        Vector2[] invalidSizes = [new(0, 0), new(-1, 0.4f), new(1, 1), new(float.NaN, 0.4f)];
        foreach (var size in invalidSizes)
        {
            Assert.NotNull(SceneValidator.Validate(scene with { Instances = ImmutableArray.Create(fruit with { Body = fruit.Body with { Size = size } }) }));
        }
    }
}
