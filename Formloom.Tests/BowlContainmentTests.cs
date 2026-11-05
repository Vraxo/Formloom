using System.Collections.Immutable;
using System.Numerics;
using Formloom.Core;
using Xunit;

namespace Formloom.Tests;

public sealed class BowlContainmentTests
{
    private static readonly string[] Fruits =
    [
        "a red apple",
        "a green apple",
        "a yellow apple",
        "a green pear",
        "a yellow pear",
        "an orange",
    ];

    private static readonly string[] Glazes = ["blue", "cream"];

    public static TheoryData<string, string> Combinations()
    {
        var data = new TheoryData<string, string>();
        foreach (var fruit in Fruits)
        {
            foreach (var glaze in Glazes)
            {
                data.Add($"{fruit} in a {glaze} ceramic bowl on a wooden table", glaze);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Combinations))]
    public void EveryFruitAndGlazeCombinationParsesAndGenerates(string prompt, string glaze)
    {
        var parsed = PromptParser.ParseContainment(prompt);
        Assert.NotNull(parsed);
        Assert.Equal(CeramicKind.Bowl, parsed.Bowl.Kind);
        Assert.Equal(glaze == "blue" ? CeramicGlaze.Blue : CeramicGlaze.Cream, parsed.Bowl.Glaze);

        var scene = SceneGenerator.Generate(new(prompt)).Scene;
        Assert.NotNull(scene);
        var containment = scene.Containment;
        Assert.NotNull(containment);
        Assert.Equal(CeramicKind.Bowl, containment.Bowl.Kind);
        Assert.Equal(parsed.Content.Kind, containment.Content.Subject);

        // The bowl is the container and the fruit is its content, not two siblings.
        Assert.Equal(2, scene.Objects.Length);
        Assert.Single(scene.Ceramics);
        Assert.Single(scene.Fruits);
        Assert.Equal(containment.Bowl, scene.Ceramics[0]);
        Assert.Equal(containment.Content, scene.Fruits[0]);
        Assert.Equal(containment.Content.Depth, containment.Bowl.Depth + 1);
        Assert.Equal(
            containment.Content.Subject.ToString().ToLowerInvariant() + "-in-" + glaze + "-bowl",
            scene.SubjectToken);
    }

    [Theory]
    [MemberData(nameof(Combinations))]
    public void ContainmentPromptsAreNotObjectLists(string prompt, string _)
    {
        // The containment grammar is a separate typed path; the list grammar must not
        // silently read a contained fruit as a neighbour, and the fruit-only helpers stay
        // fruit-only.
        Assert.Null(PromptParser.ParseObjects(prompt));
        Assert.Null(PromptParser.ParseScene(prompt));
        Assert.Null(PromptParser.ParseFruit(prompt));
        Assert.Null(PromptParser.Parse(prompt));
        Assert.NotNull(PromptParser.ParseContainment(prompt));
    }

    [Theory]
    [InlineData("a red apple in a blue ceramic cup on a wooden table")]
    [InlineData("a red apple in a blue ceramic vase on a wooden table")]
    [InlineData("a green pear in a cream ceramic cup on a wooden table")]
    [InlineData("a blue ceramic bowl with a red apple on a wooden table")]
    [InlineData("a blue ceramic bowl containing a red apple on a wooden table")]
    [InlineData("a blue ceramic bowl with an apple inside on a wooden table")]
    [InlineData("a red apple in a blue ceramic bowl")]
    [InlineData("a red apple in a blue ceramic bowl on a table")]
    [InlineData("a red apple in a blue ceramic bowl on a wooden table and a green pear")]
    [InlineData("a red apple in a blue ceramic bowl and a green pear on a wooden table")]
    [InlineData("a red apple and a green pear in a blue ceramic bowl on a wooden table")]
    [InlineData("two red apples in a blue ceramic bowl on a wooden table")]
    [InlineData("apples in a blue ceramic bowl on a wooden table")]
    [InlineData("a big red apple in a blue ceramic bowl on a wooden table")]
    [InlineData("a red apple in a large blue ceramic bowl on a wooden table")]
    [InlineData("a shiny red apple in a blue ceramic bowl on a wooden table")]
    [InlineData("a red apple in a blue ceramic bowl in a cream ceramic cup on a wooden table")]
    [InlineData("a red apple in a blue ceramic bowl on a wooden table, a green pear")]
    [InlineData("an apple in a blue ceramic bowl on a wooden table")]
    [InlineData("a red apple in the blue ceramic bowl on a wooden table")]
    [InlineData("a red apple in a ceramic bowl on a wooden table")]
    [InlineData("a red apple on a wooden table")]
    [InlineData("a blue ceramic bowl on a wooden table")]
    [InlineData("")]
    public void UnsupportedContainmentPhrasingsAreRejected(string prompt)
    {
        Assert.Null(PromptParser.ParseContainment(prompt));
        var result = SceneGenerator.Generate(new(prompt));
        if (prompt is "a red apple on a wooden table" or "a blue ceramic bowl on a wooden table")
        {
            // Ordinary single-object prompts still work; they simply are not containment.
            Assert.NotNull(result.Scene);
            Assert.Null(result.Scene.Containment);
            return;
        }

        Assert.Null(result.Scene);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void ContainmentIsNullForPromptsThatDoNotContain()
    {
        foreach (var prompt in new[]
        {
            "a red apple on a wooden table",
            "a cream ceramic bowl on a wooden table",
            "a red apple and a blue ceramic bowl on a wooden table",
            "a red apple, a green pear, and an orange on a wooden table",
        })
        {
            var scene = SceneGenerator.Generate(new(prompt)).Scene;
            Assert.NotNull(scene);
            Assert.Null(scene.Containment);
        }
    }

    [Theory]
    [InlineData(20261105UL, 0.0)]
    [InlineData(20261105UL, 1.0)]
    [InlineData(7UL, 0.35)]
    public void ContentIsSeatedInsideTheBowl(ulong seed, double variation)
    {
        foreach (var fruit in Fruits)
        {
            foreach (var glaze in Glazes)
            {
                var prompt = $"{fruit} in a {glaze} ceramic bowl on a wooden table";
                var scene = SceneGenerator.Generate(new(prompt, Seed: seed, Variation: variation)).Scene;
                Assert.NotNull(scene);
                var containment = scene.Containment!;
                var bowl = containment.Bowl;
                var content = containment.Content;
                var profile = containment.Profile;

                // Base near the cavity floor, i.e. just below the near rim and hidden by it.
                Assert.True(containment.ContentBaseY > containment.FrontRimY, "content must sit below the near rim");
                Assert.True(containment.ContentBaseY < containment.FrontRimY + (content.Body.Size.Y * 0.55f), "content must not sink out of sight");
                Assert.True(content.Body.Center.Y - (content.Body.Size.Y / 2) < containment.FrontRimY, "content must rise above the near rim");
                Assert.InRange(containment.OccludedFraction, 0.15f, 0.55f);
                Assert.InRange(containment.VisibleFraction, 0.45f, 0.85f);

                // Horizontal margin inside the inner width.
                Assert.InRange(containment.OpeningCoverage, 0.25f, 0.8f);
                Assert.True(
                    Math.Abs(content.Body.Center.X - containment.OpeningCenter.X) + (content.Body.Size.X / 2) < containment.OpeningRadius.X,
                    "content must keep a margin inside the inner width");

                // Bowl-local placement: the content is inside the bowl's own transform box.
                Assert.InRange(containment.LocalCenter.X, 0f, 1f);
                Assert.InRange(containment.LocalCenter.Y, 0f, 1f);
                Assert.True(containment.LocalSize.X < profile.Width);
                Assert.True(containment.LocalSize.Y < profile.Height);

                // Nothing escapes the safe canvas margins.
                Assert.True(CeramicBounds.For(bowl).Left >= 0f);
                Assert.True(content.Body.Center.X - (content.Body.Size.X / 2) > 0.03f);
                Assert.True(content.Body.Center.X + (content.Body.Size.X / 2) < 0.97f);
                Assert.True(content.Body.Center.Y - (content.Body.Size.Y / 2) > 0.03f);
            }
        }
    }

    [Fact]
    public void EachFruitTypeGetsItsOwnFit()
    {
        var apple = SceneGenerator.Generate(new("a red apple in a blue ceramic bowl on a wooden table")).Scene!.Containment!;
        var pear = SceneGenerator.Generate(new("a green pear in a blue ceramic bowl on a wooden table")).Scene!.Containment!;
        var orange = SceneGenerator.Generate(new("an orange in a blue ceramic bowl on a wooden table")).Scene!.Containment!;

        // A pear stands taller and slimmer than an apple; an orange is rounder and lower.
        Assert.True(pear.Content.Body.Size.Y > apple.Content.Body.Size.Y);
        Assert.True(apple.Content.Body.Size.Y > orange.Content.Body.Size.Y);
        Assert.True(pear.Content.Body.Size.X < apple.Content.Body.Size.X);
        Assert.True(Math.Abs(orange.Content.Body.Size.X - orange.Content.Body.Size.Y) < 0.001f);

        // The same seed and bowl, so the only difference is the fit of the content.
        Assert.Equal(apple.Bowl, pear.Bowl);
        Assert.NotEqual(apple.Content.Body.Size, orange.Content.Body.Size);
    }

    [Fact]
    public void PlacementIsBoundedAcrossSeeds()
    {
        var sizes = new List<float>();
        for (ulong seed = 0; seed < 120; seed++)
        {
            foreach (var variation in new[] { 0.0, 1.0 })
            {
                var scene = SceneGenerator.Generate(new(
                    "a green pear in a cream ceramic bowl on a wooden table",
                    Seed: seed * 7919UL,
                    Variation: variation)).Scene;
                Assert.NotNull(scene);
                var containment = scene.Containment!;
                Assert.InRange(containment.OccludedFraction, 0.15f, 0.55f);
                Assert.InRange(containment.OpeningCoverage, 0.25f, 0.8f);
                Assert.InRange(containment.Content.Body.Size.Y, 0.1f, 0.4f);
                Assert.InRange(containment.Content.Body.Size.X, 0.1f, 0.4f);
                Assert.True(containment.ContentBaseY > containment.FrontRimY);
                sizes.Add(containment.Content.Body.Size.Y);
            }
        }

        // Bounded does not mean identical: seeds must still move the content.
        Assert.True(sizes.Max() - sizes.Min() > 0.002f);
        Assert.True(sizes.Max() - sizes.Min() < 0.06f);
    }

    [Fact]
    public void PlacementIsDeterministic()
    {
        var request = new GenerationRequest("a red apple in a cream ceramic bowl on a wooden table", Seed: 5, Variation: 0.6);
        var first = SceneGenerator.Generate(request).Scene!.Containment!;
        var second = SceneGenerator.Generate(request).Scene!.Containment!;
        Assert.Equal(first, second);
        Assert.Equal(first.Content.Body.Center, second.Content.Body.Center);
        Assert.Equal(first.Content.Seed, second.Content.Seed);
    }

    [Fact]
    public void ContentSeedAndShapeIgnoreTheGlaze()
    {
        var blue = SceneGenerator.Generate(new("a red apple in a blue ceramic bowl on a wooden table", Seed: 11, Variation: 0.8)).Scene!.Containment!;
        var cream = SceneGenerator.Generate(new("a red apple in a cream ceramic bowl on a wooden table", Seed: 11, Variation: 0.8)).Scene!.Containment!;

        Assert.NotEqual(blue.Bowl.Glaze, cream.Bowl.Glaze);
        Assert.Equal(blue.Content.Seed, cream.Content.Seed);
        Assert.Equal(blue.Content.Body, cream.Content.Body);
        Assert.Equal(blue.Bowl.Body, cream.Bowl.Body);
        Assert.Equal(blue.SubjectToken != cream.SubjectToken, true);
    }

    [Fact]
    public void GeometrySplitsTheBowlAtTheRim()
    {
        var bowl = SceneGenerator.Generate(new("a blue ceramic bowl on a wooden table")).Scene!.Ceramics[0];
        var profile = CeramicForms.For(bowl.Kind);

        // The outline starts at the left rim and reaches the right rim partway round, so the
        // near wall is a prefix and the far rim a suffix.
        Assert.Equal(BowlGeometry.LeftRim(profile), profile.Outline[0].Start);
        var front = BowlGeometry.FrontChainLength(profile);
        Assert.Equal(12, front);
        Assert.Equal(BowlGeometry.RightRim(profile), profile.Outline[front - 1].End);

        var wall = BowlGeometry.FrontWall(profile);
        Assert.True(wall.Length > front);
        Assert.Equal(BowlGeometry.LeftRim(profile), wall[0].Start);
        Assert.Equal(BowlGeometry.LeftRim(profile), wall[^1].End);
        Assert.Equal(new Vector2(profile.MouthCenter.X, profile.MouthCenter.Y + profile.MouthRadius.Y), wall[1].End);

        // The band must reach the base of the bowl, otherwise the near wall would not hide
        // the seated part of the content.
        var extent = CurveBuilder.Extent(wall);
        Assert.True(Math.Abs(profile.OutlineBottom - extent.Bottom) < 0.002f);
        Assert.True(Math.Abs(profile.MouthCenter.Y - profile.MouthRadius.Y - extent.Top) < 0.002f);

        var opening = BowlGeometry.Opening(profile);
        Assert.Equal(4, opening.Length);
        Assert.Equal(BowlGeometry.LeftRim(profile), opening[0].Start);
        Assert.Equal(BowlGeometry.LeftRim(profile), opening[^1].End);
    }

    [Fact]
    public void ValidatorRejectsBrokenContainment()
    {
        var scene = SceneGenerator.Generate(new("a red apple in a blue ceramic bowl on a wooden table", Seed: 3)).Scene!;
        var containment = scene.Containment!;
        Assert.Null(SceneValidator.Validate(scene));

        // Content dragged out past the inner width.
        var outside = containment with
        {
            Content = containment.Content with
            {
                Body = containment.Content.Body with { Center = new(0.12f, containment.Content.Body.Center.Y) },
            },
        };
        Assert.NotNull(SceneValidator.Validate(Rebuild(scene, outside)));

        // Content lifted so it floats above the near rim.
        var floating = containment with
        {
            Content = containment.Content with
            {
                Body = containment.Content.Body with
                {
                    Center = new(
                        containment.Content.Body.Center.X,
                        containment.FrontRimY - containment.Content.Body.Size.Y),
                },
            },
        };
        Assert.NotNull(SceneValidator.Validate(Rebuild(scene, floating)));

        // A cup is not a container.
        var cup = containment.Bowl with { Kind = CeramicKind.Cup };
        var withCup = new BowlContainment(cup, containment.Content);
        Assert.NotNull(SceneValidator.Validate(Rebuild(scene, withCup)));

        // The fruit must be the bowl's content, not an unrelated neighbour.
        var neighbour = new AppleDescription(
            AppleColor.Red,
            new Vector2(0.5f, 0.55f),
            new Vector2(0.3f, 0.3f),
            0.02,
            -0.1,
            -0.4,
            0.1);
        var extra = scene with { Entries = scene.Objects.Add(SceneEntry.ForFruit(new(FruitKind.Orange, neighbour, 1, 2))) };
        Assert.NotNull(SceneValidator.Validate(extra));
    }

    [Theory]
    [InlineData("  A  red   apple  in a  blue ceramic bowl  on a wooden table. ")]
    [InlineData("a red apple in a blue ceramic bowl on a wooden table")]
    [InlineData("A RED APPLE IN A BLUE CERAMIC BOWL ON A WOODEN TABLE!")]
    public void CaseWhitespaceAndPunctuationNormalizationIsPreserved(string prompt)
    {
        var parsed = PromptParser.ParseContainment(prompt);
        Assert.NotNull(parsed);
        Assert.Equal(FruitKind.Apple, parsed.Content.Kind);
        Assert.Equal(FruitColor.Red, parsed.Content.Color);
        Assert.Equal(CeramicKind.Bowl, parsed.Bowl.Kind);
        Assert.Equal(CeramicGlaze.Blue, parsed.Bowl.Glaze);
        Assert.NotNull(SceneGenerator.Generate(new(prompt)).Scene);
    }

    [Fact]
    public void ContentKeepsItsSpeciesSilhouetteAndNeverStretches()
    {
        var apple = SceneGenerator.Generate(new("a red apple in a blue ceramic bowl on a wooden table", Seed: 21, Variation: 0.5)).Scene!.Containment!;
        var pear = SceneGenerator.Generate(new("a green pear in a blue ceramic bowl on a wooden table", Seed: 21, Variation: 0.5)).Scene!.Containment!;
        var orange = SceneGenerator.Generate(new("an orange in a blue ceramic bowl on a wooden table", Seed: 21, Variation: 0.5)).Scene!.Containment!;

        // The local transform scales the content's own intact profile, so the species shape is
        // preserved instead of being stretched to fill the bowl.
        Assert.True(Math.Abs((orange.Content.Body.Size.Y / orange.Content.Body.Size.X) - 1f) < 0.001f);
        Assert.InRange(pear.Content.Body.Size.Y / pear.Content.Body.Size.X, 1.2f, 1.4f);
        Assert.InRange(apple.Content.Body.Size.Y / apple.Content.Body.Size.X, 0.95f, 1.15f);
    }

    [Fact]
    public void AlternateSeedsMoveTheContentWithoutChangingIdentity()
    {
        var first = SceneGenerator.Generate(new("a green pear in a cream ceramic bowl on a wooden table", Seed: 42, Variation: 1)).Scene!.Containment!;
        var second = SceneGenerator.Generate(new("a green pear in a cream ceramic bowl on a wooden table", Seed: 1337, Variation: 1)).Scene!.Containment!;

        Assert.Equal(first.Bowl.Kind, second.Bowl.Kind);
        Assert.Equal(first.Bowl.Glaze, second.Bowl.Glaze);
        Assert.Equal(first.Content.Subject, second.Content.Subject);
        Assert.NotEqual(first.Content.Body.Center, second.Content.Body.Center);
        Assert.NotEqual(first.Content.Body.Size, second.Content.Body.Size);
        Assert.InRange(second.OccludedFraction, 0.15f, 0.55f);
        Assert.InRange(second.OpeningCoverage, 0.25f, 0.8f);
    }

    private static StillLifeScene Rebuild(StillLifeScene scene, BowlContainment containment) => scene with
    {
        Apple = containment.Content.Body,
        Subject = containment.Content.Subject,
        Entries = ImmutableArray.Create(
            SceneEntry.ForCeramic(containment.Bowl),
            SceneEntry.ForFruit(containment.Content)),
        Containment = containment,
    };
}
