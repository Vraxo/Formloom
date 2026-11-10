using System.Collections.Immutable;
using System.Globalization;
using System.Numerics;
using Formloom.Core.Ceramics;
using Formloom.Core.Core;
using Formloom.Core.Daisies;
using Formloom.Core.Roses;
using Formloom.Core.Scenes;
using Formloom.Core.Tulips;
using Xunit;

namespace Formloom.Tests.Roses;

public sealed class VaseRoseTests
{
    private static readonly (string Prompt, RoseColor Color, CeramicGlaze Glaze)[] Combinations =
    [
        ("a red rose in a blue ceramic vase on a wooden table", RoseColor.Red, CeramicGlaze.Blue),
        ("a red rose in a cream ceramic vase on a wooden table", RoseColor.Red, CeramicGlaze.Cream),
        ("a pink rose in a blue ceramic vase on a wooden table", RoseColor.Pink, CeramicGlaze.Blue),
        ("a pink rose in a cream ceramic vase on a wooden table", RoseColor.Pink, CeramicGlaze.Cream),
    ];

    private static readonly string[] Rejected =
    [
        "a rose in a blue ceramic vase on a wooden table",
        "a white rose in a blue ceramic vase on a wooden table",
        "a yellow rose in a cream ceramic vase on a wooden table",
        "a blue rose in a blue ceramic vase on a wooden table",
        "a purple rose in a cream ceramic vase on a wooden table",
        "a yellow sunflower in a cream ceramic vase on a wooden table",
        "a pink lily in a blue ceramic vase on a wooden table",
        "red rose in a blue ceramic vase on a wooden table",
        "red roses in a blue ceramic vase on a wooden table",
        "two red roses in a cream ceramic vase on a wooden table",
        "a bunch of pink roses in a blue ceramic vase on a wooden table",
        "a bouquet of red roses in a cream ceramic vase on a wooden table",
        "a red rose and a pink rose in a blue ceramic vase on a wooden table",
        "a red rose and a tulip in a cream ceramic vase on a wooden table",
        "a red rose and a white daisy in a blue ceramic vase on a wooden table",
        "a red rose in a blue ceramic bowl on a wooden table",
        "a pink rose in a cream ceramic cup on a wooden table",
        "a red rose in a ceramic vase on a wooden table",
        "a red rose in a red ceramic vase on a wooden table",
        "a pink rose in a yellow ceramic vase on a wooden table",
        "a red rose inside a blue ceramic vase on a wooden table",
        "a red rose within a blue ceramic vase on a wooden table",
        "a blue ceramic vase with a red rose on a wooden table",
        "a blue ceramic vase holding a red rose on a wooden table",
        "a blue ceramic vase with a pink rose on a wooden table",
        "a red rose in two blue ceramic vases on a wooden table",
        "a red rose in a blue ceramic vase on a wooden table and a red apple",
        "a pink rose in a cream ceramic vase on a wooden table, a green pear",
        "a large red rose in a blue ceramic vase on a wooden table",
        "a red rose in a large blue ceramic vase on a wooden table",
        "a red rose in a blue ceramic vase on a table",
        "a red rose in a blue ceramic vase",
        "a pink rose in a cream ceramic vase on a wooden table with a stem",
        string.Empty,
    ];

    public static TheoryData<string, string, string> CombinationCases()
    {
        var data = new TheoryData<string, string, string>();
        foreach (var (prompt, color, glaze) in Combinations)
        {
            data.Add(prompt, color.ToString(), glaze.ToString());
        }

        return data;
    }

    public static TheoryData<string> RejectedCases()
    {
        var data = new TheoryData<string>();
        foreach (var prompt in Rejected)
        {
            data.Add(prompt);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(CombinationCases))]
    public void FourExactPromptsParseAndGenerateTypedRoseContents(string prompt, string color, string glaze)
    {
        var expectedColor = Enum.Parse<RoseColor>(color);
        var expectedGlaze = Enum.Parse<CeramicGlaze>(glaze);
        var parsed = PromptParser.ParseVaseRose(prompt);
        Assert.NotNull(parsed);
        Assert.Equal(expectedColor, parsed.Color);
        Assert.Equal(CeramicKind.Vase, parsed.Vase.Kind);
        Assert.Equal(expectedGlaze, parsed.Vase.Glaze);

        var generated = SceneGenerator.Generate(new(prompt));
        Assert.Null(generated.Error);
        var scene = Assert.IsType<StillLifeScene>(generated.Scene);
        var rose = Assert.IsType<VaseRose>(scene.VaseRose);
        Assert.Equal(FlowerKind.Rose, rose.Flower);
        Assert.Equal(expectedColor, rose.Color);
        Assert.Equal(expectedGlaze, rose.Vase.Glaze);
        Assert.Equal($"{color.ToLowerInvariant()}-rose-in-{glaze.ToLowerInvariant()}-vase", rose.SubjectToken);
        Assert.Equal(rose.SubjectToken, scene.SubjectToken);
        Assert.Null(SceneValidator.Validate(scene));

        var entry = Assert.Single(scene.Objects);
        Assert.True(entry.IsCeramic);
        Assert.Same(entry.Ceramic, rose.Vase);
        Assert.Same(scene.Ceramics[0], rose.Vase);
        Assert.Empty(scene.Fruits);
        Assert.Null(scene.VaseFlower);
        Assert.Null(scene.VaseDaisy);
        Assert.Null(scene.VaseTulipDaisy);
        Assert.Null(scene.Containment);
        Assert.Null(scene.CupContents);

        Assert.Single(rose.Plant.Stem);
        Assert.Equal(2, rose.Plant.Leaves.Length);
        Assert.Equal(RoseGeometry.PetalCount, rose.Plant.Bloom.Petals.Length);
        Assert.Equal(5, rose.Plant.Bloom.Petals.Count(petal => petal.Layer == RosePetalLayer.Outer));
        Assert.Equal(5, rose.Plant.Bloom.Petals.Count(petal => petal.Layer == RosePetalLayer.Middle));
        Assert.Equal(3, rose.Plant.Bloom.Petals.Count(petal => petal.Layer == RosePetalLayer.Inner));
    }

    [Theory]
    [MemberData(nameof(RejectedCases))]
    public void UnsupportedRosePromptsAreRejectedWhole(string prompt)
    {
        Assert.Null(PromptParser.ParseVaseRose(prompt));
        var generated = SceneGenerator.Generate(new(prompt));
        Assert.False(generated.IsSuccess);
        Assert.Null(generated.Scene);
        Assert.NotNull(generated.Error);
    }

    [Fact]
    public void RoseColorRemainsAClosedRedAndPinkChoice()
    {
        Assert.Equal(new[] { nameof(RoseColor.Red), nameof(RoseColor.Pink) }, Enum.GetNames<RoseColor>());
    }

    [Fact]
    public void RoseGrammarKeepsExistingNormalizationWithoutAddingSynonyms()
    {
        foreach (var prompt in new[]
        {
            "A RED ROSE IN A BLUE CERAMIC VASE ON A WOODEN TABLE",
            "  a pink rose in a cream ceramic vase on a wooden table  ",
            "a   red   rose   in   a   blue   ceramic   vase   on   a   wooden   table",
            "a red rose in a blue ceramic vase on a wooden table.",
            "a red rose in a blue ceramic vase on a wooden table!",
            "a red rose in a blue ceramic vase on a wooden table?",
        })
        {
            Assert.NotNull(PromptParser.ParseVaseRose(prompt));
            Assert.NotNull(SceneGenerator.Generate(new(prompt)).Scene);
        }
    }

    [Fact]
    public void RoseGeometryIsLayeredDeterministicAndBoundedAcrossSeedsAndVariation()
    {
        foreach (var (prompt, _, _) in Combinations)
        {
            foreach (var seed in new ulong[] { 0, 1, 42, 1337, 20261105, ulong.MaxValue })
            {
                foreach (var variation in new[] { 0, 0.35, 1 })
                {
                    var scene = Scene(prompt, seed, variation);
                    var rose = scene.VaseRose!;
                    var plant = rose.Plant;
                    Assert.Null(SceneValidator.Validate(scene));
                    Assert.Equal(Signature(plant), Signature(Scene(prompt, seed, variation).VaseRose!.Plant));
                    Assert.Single(plant.Stem);
                    Assert.Equal(2, plant.Leaves.Length);
                    Assert.Equal(RoseGeometry.PetalCount, plant.Bloom.Petals.Length);
                    Assert.Equal(
                        Enumerable.Repeat(RosePetalLayer.Outer, RoseGeometry.OuterPetalCount)
                            .Concat(Enumerable.Repeat(RosePetalLayer.Middle, RoseGeometry.MiddlePetalCount))
                            .Concat(Enumerable.Repeat(RosePetalLayer.Inner, RoseGeometry.InnerPetalCount)),
                        plant.Bloom.Petals.Select(petal => petal.Layer));

                    var opening = rose.OpeningCenter;
                    var radius = rose.OpeningRadius;
                    Assert.Equal(opening.X, plant.StemBase.X);
                    Assert.Equal(opening.Y + (radius.Y * 0.35f), plant.StemBase.Y);
                    var dx = (plant.StemBase.X - opening.X) / radius.X;
                    var dy = (plant.StemBase.Y - opening.Y) / radius.Y;
                    Assert.True((dx * dx) + (dy * dy) < 0.95f, "the rose stem must enter through the mouth");
                    Assert.True(plant.StemBase.Y > rose.MouthTop);
                    Assert.True(plant.StemTop.Y < rose.MouthTop - 0.004f);
                    Assert.Equal(plant.Stem[0].Start, plant.StemBase);
                    Assert.Equal(plant.Stem[0].End, plant.StemTop);
                    Assert.Equal(plant.StemTop.X, plant.Bloom.Center.X);
                    Assert.True(plant.Bloom.Top < rose.MouthTop - 0.01f);
                    Assert.True(plant.Bloom.Bottom < rose.MouthTop - 0.004f);
                    Assert.True(plant.Bloom.Width > 0 && plant.Bloom.Height > 0);

                    foreach (var leaf in plant.Leaves)
                    {
                        Assert.True(RoseGeometry.DistanceToStem(plant, leaf.Attachment) < 0.002f);
                        Assert.True(leaf.Tip.Y < leaf.Attachment.Y);
                        Assert.True(leaf.Tip.Y > plant.Bloom.Bottom, "both leaves must clear the bottom of the bloom");
                        Assert.False(leaf.Outline.IsDefaultOrEmpty);
                        Assert.Equal(7, leaf.Vein.Length);
                    }

                    Assert.True(plant.Leaves[0].Tip.X < plant.Leaves[1].Tip.X);
                    Assert.True(Vector2.Distance(plant.Leaves[0].Attachment, plant.Leaves[1].Attachment) > 0.01f);

                    var bounds = plant.Bounds;
                    Assert.True(float.IsFinite(bounds.Left) && float.IsFinite(bounds.Top) &&
                        float.IsFinite(bounds.Right) && float.IsFinite(bounds.Bottom));
                    Assert.InRange(bounds.Left, 0.02f, 0.98f);
                    Assert.InRange(bounds.Right, 0.02f, 0.98f);
                    Assert.True(bounds.Top >= RoseGeometry.TopMargin - 0.012f);
                    Assert.True(bounds.Bottom < 0.94f);

                    var outerReach = plant.Bloom.Petals.Where(petal => petal.Layer == RosePetalLayer.Outer)
                        .Max(petal => Vector2.Distance(plant.Bloom.Center, petal.Tip));
                    var middleReach = plant.Bloom.Petals.Where(petal => petal.Layer == RosePetalLayer.Middle)
                        .Max(petal => Vector2.Distance(plant.Bloom.Center, petal.Tip));
                    var innerReach = plant.Bloom.Petals.Where(petal => petal.Layer == RosePetalLayer.Inner)
                        .Max(petal => Vector2.Distance(plant.Bloom.Center, petal.Tip));
                    Assert.True(outerReach > middleReach && middleReach > innerReach, "each rose layer must step inward");
                    Assert.All(plant.Bloom.Petals, petal =>
                    {
                        Assert.False(petal.Outline.IsDefaultOrEmpty);
                        Assert.Single(petal.Fold);
                        var extent = CurveBuilder.Extent(petal.Outline);
                        Assert.True(extent.Right > extent.Left && extent.Bottom > extent.Top);
                    });
                }
            }
        }
    }

    [Theory]
    [InlineData("a red rose in a blue ceramic vase on a wooden table", 64, 64)]
    [InlineData("a red rose in a cream ceramic vase on a wooden table", 1024, 1024)]
    [InlineData("a pink rose in a blue ceramic vase on a wooden table", 4096, 1024)]
    [InlineData("a pink rose in a cream ceramic vase on a wooden table", 1024, 4096)]
    [InlineData("a red rose in a blue ceramic vase on a wooden table", 640, 480)]
    public void RoseGeometryIsCanvasSizeIndependent(string prompt, int width, int height)
    {
        var scene = SceneGenerator.Generate(new(prompt, 1337, 1, Width: width, Height: height)).Scene!;
        Assert.Equal(width, scene.Width);
        Assert.Equal(height, scene.Height);
        Assert.Null(SceneValidator.Validate(scene));
        Assert.Equal(Signature(Scene(prompt, 1337, 1).VaseRose!.Plant), Signature(scene.VaseRose!.Plant));
    }

    [Fact]
    public void RoseGeometryIsIndependentOfColourGlazeAndOtherFlowerStreams()
    {
        const ulong seed = 20261105;
        const double variation = 0.35;
        var redBlue = Scene(Combinations[0].Prompt, seed, variation).VaseRose!.Plant;
        var redCream = Scene(Combinations[1].Prompt, seed, variation).VaseRose!.Plant;
        var pinkBlue = Scene(Combinations[2].Prompt, seed, variation).VaseRose!.Plant;
        var pinkCream = Scene(Combinations[3].Prompt, seed, variation).VaseRose!.Plant;
        Assert.Equal(Signature(redBlue), Signature(redCream));
        Assert.Equal(Signature(redBlue), Signature(pinkBlue));
        Assert.Equal(Signature(redBlue), Signature(pinkCream));

        var otherSeed = Scene(Combinations[0].Prompt, 1337, variation).VaseRose!.Plant;
        Assert.NotEqual(Signature(redBlue), Signature(otherSeed));

        var tulipBefore = SceneGenerator.Generate(new("a red tulip in a blue ceramic vase on a wooden table", seed, variation)).Scene!.VaseFlower!.Plant;
        var daisyBefore = SceneGenerator.Generate(new("a white daisy in a blue ceramic vase on a wooden table", seed, variation)).Scene!.VaseDaisy!.Plant;
        var pairBefore = SceneGenerator.Generate(new("a red tulip and a white daisy in a blue ceramic vase on a wooden table", seed, variation)).Scene!.VaseTulipDaisy!;
        _ = Scene(Combinations[0].Prompt, seed, variation);
        var tulipAfter = SceneGenerator.Generate(new("a red tulip in a blue ceramic vase on a wooden table", seed, variation)).Scene!.VaseFlower!.Plant;
        var daisyAfter = SceneGenerator.Generate(new("a white daisy in a blue ceramic vase on a wooden table", seed, variation)).Scene!.VaseDaisy!.Plant;
        var pairAfter = SceneGenerator.Generate(new("a red tulip and a white daisy in a blue ceramic vase on a wooden table", seed, variation)).Scene!.VaseTulipDaisy!;
        Assert.Equal(TulipSignature(tulipBefore), TulipSignature(tulipAfter));
        Assert.Equal(DaisySignature(daisyBefore), DaisySignature(daisyAfter));
        Assert.Equal(TulipSignature(pairBefore.Tulip), TulipSignature(pairAfter.Tulip));
        Assert.Equal(DaisySignature(pairBefore.Daisy), DaisySignature(pairAfter.Daisy));
    }

    [Fact]
    public void ValidationRejectsBrokenRoseGeometryAndMismatchedVaseIdentity()
    {
        var scene = Scene(Combinations[0].Prompt);
        var rose = scene.VaseRose!;
        var plant = rose.Plant;

        Assert.Equal(
            "Only a vase can hold a rose.",
            SceneValidator.Validate(scene with { VaseRose = rose with { Vase = rose.Vase with { Kind = CeramicKind.Bowl } } }));
        Assert.Equal(
            "A vase must have a blue or cream glaze.",
            SceneValidator.Validate(scene with { VaseRose = rose with { Vase = rose.Vase with { Glaze = (CeramicGlaze)9 } } }));
        Assert.Equal(
            "Only a rose is supported by this arrangement.",
            SceneValidator.Validate(scene with { VaseRose = rose with { Flower = FlowerKind.Daisy } }));
        Assert.Equal(
            "A rose must be red or pink.",
            SceneValidator.Validate(scene with { VaseRose = rose with { Color = (RoseColor)9 } }));
        Assert.Contains("one stem", SceneValidator.Validate(scene with { VaseRose = rose with { Plant = null! } }), StringComparison.Ordinal);
        Assert.Equal(
            "A vase rose must have one continuous stem, exactly two leaves and thirteen petals.",
            SceneValidator.Validate(scene with { VaseRose = rose with { Plant = plant with { Stem = [] } } }));
        Assert.Equal(
            "A vase rose must have one continuous stem, exactly two leaves and thirteen petals.",
            SceneValidator.Validate(scene with { VaseRose = rose with { Plant = plant with { Leaves = plant.Leaves.RemoveAt(0) } } }));
        Assert.Equal(
            "A vase rose must have one continuous stem, exactly two leaves and thirteen petals.",
            SceneValidator.Validate(scene with { VaseRose = rose with { Plant = plant with { Bloom = plant.Bloom with { Petals = plant.Bloom.Petals.RemoveAt(0) } } } }));
        Assert.Equal(
            "Rose geometry must be finite and positive.",
            SceneValidator.Validate(scene with { VaseRose = rose with { Plant = plant with { StemThickness = float.NaN } } }));
        Assert.Equal(
            "Rose petals must keep their finite outer, middle and inner layer structure.",
            SceneValidator.Validate(scene with { VaseRose = rose with { Plant = plant with { Bloom = plant.Bloom with { Petals = plant.Bloom.Petals.SetItem(0, plant.Bloom.Petals[0] with { Tone = float.NaN }) } } } }));
        var collapsedPetal = plant.Bloom.Petals[0].Outline
            .Select(_ => new CubicSegment(Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero))
            .ToImmutableArray();
        Assert.Equal(
            "Rose petals must keep their finite outer, middle and inner layer structure.",
            SceneValidator.Validate(scene with { VaseRose = rose with { Plant = plant with { Bloom = plant.Bloom with { Petals = plant.Bloom.Petals.SetItem(0, plant.Bloom.Petals[0] with { Outline = collapsedPetal }) } } } }));
        Assert.Equal(
            "Rose petals must keep their finite outer, middle and inner layer structure.",
            SceneValidator.Validate(scene with { VaseRose = rose with { Plant = plant with { Bloom = plant.Bloom with { Petals = plant.Bloom.Petals.SetItem(10, plant.Bloom.Petals[10] with { Layer = RosePetalLayer.Outer }) } } } }));
        var collapsedLeaf = plant.Leaves[0].Outline
            .Select(_ => new CubicSegment(Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero))
            .ToImmutableArray();
        Assert.Equal(
            "Every rose leaf must describe a finite blade and vein.",
            SceneValidator.Validate(scene with { VaseRose = rose with { Plant = plant with { Leaves = plant.Leaves.SetItem(0, plant.Leaves[0] with { Outline = collapsedLeaf }) } } }));
        var rightPointingLeaf = plant.Leaves[0] with
        {
            Tip = new Vector2(plant.Leaves[0].Attachment.X + 0.01f, plant.Leaves[0].Attachment.Y - 0.04f),
        };
        Assert.Equal(
            "The two rose leaves must remain separate on opposite sides of the stem.",
            SceneValidator.Validate(scene with { VaseRose = rose with { Plant = plant with { Leaves = plant.Leaves.SetItem(0, rightPointingLeaf) } } }));
        Assert.Equal(
            "The rose stem must connect its typed base and bloom attachment.",
            SceneValidator.Validate(scene with { VaseRose = rose with { Plant = plant with { StemBase = new Vector2(0.2f, 0.4f) } } }));

        var foreignVase = rose.Vase with { };
        Assert.NotSame(rose.Vase, foreignVase);
        Assert.Equal(
            "A rose scene must contain exactly its shared vase and no other object.",
            SceneValidator.Validate(scene with { VaseRose = rose with { Vase = foreignVase } }));
        var group = Scene("a blue ceramic vase and a red apple on a wooden table");
        var groupedRose = VaseRose.Create(group.Ceramics[0], RoseColor.Red, group.Seed, group.Variation);
        Assert.Equal(
            "A rose scene must contain exactly its shared vase and no other object.",
            SceneValidator.Validate(group with { VaseRose = groupedRose }));
        Assert.Contains("Only a vase", SceneValidator.Validate(scene with { VaseRose = rose with { Vase = null! } }), StringComparison.Ordinal);
    }

    [Fact]
    public void RoseIsMutuallyExclusiveWithEveryExistingContentsRelationship()
    {
        var scene = Scene(Combinations[0].Prompt);
        var rose = scene.VaseRose!;
        var tulip = Scene("a red tulip in a blue ceramic vase on a wooden table").VaseFlower!;
        var daisy = Scene("a white daisy in a blue ceramic vase on a wooden table").VaseDaisy!;
        var pair = Scene("a red tulip and a white daisy in a blue ceramic vase on a wooden table").VaseTulipDaisy!;
        var containment = Scene("a red apple in a blue ceramic bowl on a wooden table").Containment!;
        var cup = Scene("a blue ceramic cup filled with coffee on a wooden table").CupContents!;

        foreach (var invalid in new[]
        {
            scene with { VaseFlower = tulip },
            scene with { VaseDaisy = daisy },
            scene with { VaseTulipDaisy = pair },
            scene with { Containment = containment },
            scene with { CupContents = cup },
        })
        {
            Assert.Contains("cannot hold", SceneValidator.Validate(invalid), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ExistingFlowerKindValidationStaysSpeciesSpecific()
    {
        var tulipScene = Scene("a red tulip in a blue ceramic vase on a wooden table");
        var tulip = tulipScene.VaseFlower!;
        Assert.Equal(
            "Only a tulip is supported.",
            SceneValidator.Validate(tulipScene with { VaseFlower = tulip with { Flower = FlowerKind.Rose } }));

        var daisyScene = Scene("a white daisy in a blue ceramic vase on a wooden table");
        var daisy = daisyScene.VaseDaisy!;
        Assert.Equal(
            "Only a daisy is supported.",
            SceneValidator.Validate(daisyScene with { VaseDaisy = daisy with { Flower = FlowerKind.Rose } }));
    }

    private static StillLifeScene Scene(string prompt, ulong seed = 42, double variation = 0.35) =>
        SceneGenerator.Generate(new(prompt, seed, variation)).Scene!;

    private static string Signature(RosePlant plant) => string.Join(
        "|",
        Format(plant.StemBase),
        Format(plant.StemTop),
        plant.StemThickness.ToString("R", CultureInfo.InvariantCulture),
        Curves(plant.Stem),
        string.Join(";", plant.Leaves.Select(leaf => string.Join(
            ":", Format(leaf.Attachment), Format(leaf.Tip), leaf.Width.ToString("R", CultureInfo.InvariantCulture),
            Curves(leaf.Outline), Curves(leaf.Vein)))),
        plant.Bloom.Center.X.ToString("R", CultureInfo.InvariantCulture),
        plant.Bloom.Center.Y.ToString("R", CultureInfo.InvariantCulture),
        plant.Bloom.RadiusX.ToString("R", CultureInfo.InvariantCulture),
        plant.Bloom.RadiusY.ToString("R", CultureInfo.InvariantCulture),
        string.Join(";", plant.Bloom.Petals.Select(petal => string.Join(
            ":", petal.Layer, Format(petal.Root), Format(petal.Tip),
            petal.Angle.ToString("R", CultureInfo.InvariantCulture), petal.Tone.ToString("R", CultureInfo.InvariantCulture),
            Curves(petal.Outline), Curves(petal.Fold)))));

    private static string TulipSignature(TulipPlant plant) => string.Join(
        "|", plant.StemBase, plant.StemTop, plant.StemThickness,
        Curves(plant.Stem),
        string.Join(";", plant.Leaves.Select(leaf => Curves(leaf.Outline) + Curves(leaf.Vein))),
        string.Join(";", plant.Bloom.Petals.Select(petal => Curves(petal.Outline))));

    private static string DaisySignature(DaisyPlant plant) => string.Join(
        "|", plant.StemBase, plant.StemTop, plant.StemThickness,
        Curves(plant.Stem),
        string.Join(";", plant.Leaves.Select(leaf => Curves(leaf.Outline) + Curves(leaf.Vein))),
        string.Join(";", plant.Bloom.Petals.Select(petal => Curves(petal.Outline))));

    private static string Curves(ImmutableArray<CubicSegment> segments) => string.Join(
        ";", segments.Select(segment => string.Join(",", Format(segment.Start), Format(segment.Control1), Format(segment.Control2), Format(segment.End))));

    private static string Format(Vector2 point) =>
        point.X.ToString("R", CultureInfo.InvariantCulture) + "," + point.Y.ToString("R", CultureInfo.InvariantCulture);
}
