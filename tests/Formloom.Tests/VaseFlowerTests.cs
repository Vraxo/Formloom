using System.Collections.Immutable;
using System.Globalization;
using System.Numerics;
using Formloom.Core;
using Xunit;

namespace Formloom.Tests;

public sealed class VaseFlowerTests
{
    private static readonly (string Prompt, TulipColor Color, CeramicGlaze Glaze)[] Combinations =
    [
        ("a red tulip in a blue ceramic vase on a wooden table", TulipColor.Red, CeramicGlaze.Blue),
        ("a red tulip in a cream ceramic vase on a wooden table", TulipColor.Red, CeramicGlaze.Cream),
        ("a yellow tulip in a blue ceramic vase on a wooden table", TulipColor.Yellow, CeramicGlaze.Blue),
        ("a yellow tulip in a cream ceramic vase on a wooden table", TulipColor.Yellow, CeramicGlaze.Cream),
    ];

    // Every tulip phrasing this grammar must refuse whole. The separate rose grammar handles
    // red/pink roses without letting the tulip parser reinterpret them.
    public static readonly string[] Rejected =
    [
        "a pink tulip in a blue ceramic vase on a wooden table",
        "a white tulip in a blue ceramic vase on a wooden table",
        "a purple tulip in a cream ceramic vase on a wooden table",
        "a tulip in a blue ceramic vase on a wooden table",
        "a yellow sunflower in a cream ceramic vase on a wooden table",
        "a red daisy in a blue ceramic vase on a wooden table",
        "a red tulip in a blue ceramic bowl on a wooden table",
        "a yellow tulip in a cream ceramic cup on a wooden table",
        "a red tulip in a ceramic vase on a wooden table",
        "a red tulip in a red ceramic vase on a wooden table",
        "a red tulip in a blue ceramic vase on a table",
        "a red tulip in a blue ceramic vase",
        "red tulip in a blue ceramic vase on a wooden table",
        "a red tulip inside a blue ceramic vase on a wooden table",
        "a red tulip within a blue ceramic vase on a wooden table",
        "a blue ceramic vase with a red tulip on a wooden table",
        "a blue ceramic vase holding a red tulip on a wooden table",
        "a red tulip and a yellow tulip in a blue ceramic vase on a wooden table",
        "two red tulips in a blue ceramic vase on a wooden table",
        "a bunch of red tulips in a blue ceramic vase on a wooden table",
        "a bouquet of red tulips in a blue ceramic vase on a wooden table",
        "a red tulip in two blue ceramic vases on a wooden table",
        "a red tulip in a blue ceramic vase on a wooden table and a red apple",
        "a red tulip in a blue ceramic vase on a wooden table, a red apple",
        "a large red tulip in a blue ceramic vase on a wooden table",
        "a tall red tulip in a blue ceramic vase on a wooden table",
        "a red tulip in a large blue ceramic vase on a wooden table",
        "a red tulip in a blue ceramic vase with a stem on a wooden table",
        "a red apple in a blue ceramic vase on a wooden table",
        "coffee in a blue ceramic vase on a wooden table",
        string.Empty,
    ];

    public static TheoryData<string> RejectedCases()
    {
        var data = new TheoryData<string>();
        foreach (var prompt in Rejected)
        {
            data.Add(prompt);
        }

        return data;
    }

    public static TheoryData<string, string, string> ParsedCases()
    {
        var data = new TheoryData<string, string, string>();
        foreach (var (prompt, color, glaze) in Combinations)
        {
            data.Add(prompt, color.ToString(), glaze.ToString());
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ParsedCases))]
    public void EveryCombinationParsesAndGenerates(string prompt, string color, string glaze)
    {
        var expectedColor = Enum.Parse<TulipColor>(color);
        var expectedGlaze = Enum.Parse<CeramicGlaze>(glaze);
        var parsed = PromptParser.ParseVaseFlower(prompt);
        Assert.NotNull(parsed);
        Assert.Equal(FlowerKind.Tulip, parsed.Flower);
        Assert.Equal(expectedColor, parsed.Color);
        Assert.Equal(CeramicKind.Vase, parsed.Vase.Kind);
        Assert.Equal(expectedGlaze, parsed.Vase.Glaze);

        var generated = SceneGenerator.Generate(new(prompt));
        Assert.Null(generated.Error);
        var scene = generated.Scene!;
        var flower = scene.VaseFlower;
        Assert.NotNull(flower);
        Assert.Equal(FlowerKind.Tulip, flower.Flower);
        Assert.Equal(expectedColor, flower.Color);
        Assert.Equal(CeramicKind.Vase, flower.Vase.Kind);
        Assert.Equal(expectedGlaze, flower.Vase.Glaze);
        Assert.Equal($"{color.ToLowerInvariant()}-tulip-in-{glaze.ToLowerInvariant()}-vase", scene.SubjectToken);
        Assert.Null(SceneValidator.Validate(scene));
    }

    [Theory]
    [MemberData(nameof(RejectedCases))]
    public void UnsupportedFlowerPromptsAreRejectedWhole(string prompt)
    {
        Assert.Null(PromptParser.ParseVaseFlower(prompt));
        var generated = SceneGenerator.Generate(new(prompt));
        Assert.False(generated.IsSuccess);
        Assert.NotNull(generated.Error);

        // A refused flower prompt never degrades into an empty vase.
        Assert.Null(generated.Scene);
    }

    [Fact]
    public void RosePromptIsNotParsedAsATulipButUsesTheRoseGenerationPath()
    {
        const string prompt = "a red rose in a blue ceramic vase on a wooden table";
        Assert.Null(PromptParser.ParseVaseFlower(prompt));

        var generated = SceneGenerator.Generate(new(prompt));
        Assert.Null(generated.Error);
        Assert.NotNull(generated.Scene);
        Assert.Equal(RoseColor.Red, generated.Scene!.VaseRose!.Color);
        Assert.Null(generated.Scene.VaseFlower);
    }

    [Fact]
    public void ParsingNormalizesCaseWhitespaceAndPunctuation()
    {
        foreach (var variant in new[]
        {
            "A RED TULIP IN A BLUE CERAMIC VASE ON A WOODEN TABLE",
            "  a red tulip in a blue ceramic vase on a wooden table  ",
            "a   red   tulip in a blue ceramic vase on a wooden table",
            "a red tulip in a blue ceramic vase on a wooden table.",
            "a red tulip in a blue ceramic vase on a wooden table!",
        })
        {
            var parsed = PromptParser.ParseVaseFlower(variant);
            Assert.NotNull(parsed);
            Assert.Equal(TulipColor.Red, parsed.Color);
            Assert.Equal(CeramicGlaze.Blue, parsed.Vase.Glaze);
        }
    }

    [Fact]
    public void FlowerIsTypedVaseContentNotASceneEntry()
    {
        var scene = Scene("a yellow tulip in a cream ceramic vase on a wooden table");
        var flower = scene.VaseFlower!;
        var objects = scene.Objects;
        Assert.Single(objects);
        Assert.True(objects[0].IsCeramic);
        Assert.Equal(objects[0].Ceramic, flower.Vase);
        Assert.Same(scene.Ceramics[0], objects[0].Ceramic);
        Assert.Same(scene.Ceramics[0], flower.Vase);
        Assert.Empty(scene.Fruits);
        Assert.Null(scene.Containment);
        Assert.Null(scene.CupContents);
        Assert.Equal("yellow-tulip-in-cream-vase", scene.SubjectToken);
    }

    [Fact]
    public void StemAttachmentFollowsTheVaseOpening()
    {
        foreach (var (prompt, _, _) in Combinations)
        {
            foreach (var variation in new[] { 0, 0.35, 0.6, 1 })
            {
                var scene = Scene(prompt, 1337, variation);
                var flower = scene.VaseFlower!;
                var plant = flower.Plant;
                var center = flower.OpeningCenter;
                var radius = flower.OpeningRadius;
                var dx = (plant.StemBase.X - center.X) / radius.X;
                var dy = (plant.StemBase.Y - center.Y) / radius.Y;
                Assert.True((dx * dx) + (dy * dy) < 0.95f, "the stem must enter through the opening");
                Assert.True(plant.StemBase.Y > flower.MouthTop, "the stem base must sit behind the rim");
                Assert.True(plant.StemTop.Y < flower.MouthTop - 0.02f, "the stem must rise clear of the rim");
                Assert.True(plant.Bloom.Top < flower.MouthTop - 0.05f, "the bloom must clear the rim");
                Assert.Equal(plant.StemTop.X, plant.Bloom.Base.X);
                Assert.Equal(plant.StemTop.Y, plant.Bloom.Base.Y);
                foreach (var leaf in plant.Leaves)
                {
                    Assert.True(TulipGeometry.DistanceToStem(plant, leaf.Attachment) < 0.002f, "every leaf hangs from the stem");
                    Assert.True(leaf.Tip.Y < leaf.Attachment.Y, "every leaf points upwards");
                }
            }
        }
    }

    [Fact]
    public void ExactlyTwoLeavesAndOneCuppedBloomForEverySeed()
    {
        foreach (var (prompt, _, _) in Combinations)
        {
            foreach (var seed in new ulong[] { 0, 1, 42, 1337, 20261105, ulong.MaxValue })
            {
                foreach (var variation in new[] { 0, 0.5, 1 })
                {
                    var scene = Scene(prompt, seed, variation);
                    var plant = scene.VaseFlower!.Plant;
                    Assert.Equal(2, plant.Leaves.Length);
                    Assert.Single(plant.Stem);
                    Assert.InRange(plant.Bloom.Petals.Length, 3, 5);
                    Assert.Single(plant.Bloom.Petals.Where(petal => Math.Abs(petal.Tone - 1) < 1e-4f));
                    Assert.All(plant.Bloom.Petals, petal => Assert.False(petal.Outline.IsDefault));

                    // Distinct leaves: different attachments and different tips.
                    Assert.NotEqual(plant.Leaves[0].Attachment, plant.Leaves[1].Attachment);
                    Assert.True(plant.Leaves[0].Tip.X < plant.Leaves[0].Attachment.X);
                    Assert.True(plant.Leaves[1].Tip.X > plant.Leaves[1].Attachment.X);
                }
            }
        }
    }

    [Fact]
    public void RedAndYellowShareIdenticalGeometry()
    {
        var red = Scene("a red tulip in a blue ceramic vase on a wooden table", 42, 0.5).VaseFlower!.Plant;
        var yellow = Scene("a yellow tulip in a blue ceramic vase on a wooden table", 42, 0.5).VaseFlower!.Plant;
        Assert.Equal(Signature(red), Signature(yellow));
        Assert.Equal(Signature(red.Bloom), Signature(yellow.Bloom));
    }

    [Fact]
    public void GeometryIsDeterministicAndBoundedAcrossSeeds()
    {
        foreach (var seed in new ulong[] { 0, 7, 42, 1337, 20261105, ulong.MaxValue })
        {
            foreach (var variation in new[] { 0, 0.25, 0.5, 0.75, 1 })
            {
                var scene = Scene("a red tulip in a cream ceramic vase on a wooden table", seed, variation);
                Assert.Null(SceneValidator.Validate(scene));
                var plant = scene.VaseFlower!.Plant;
                Assert.Equal(Signature(plant), Signature(Scene("a red tulip in a cream ceramic vase on a wooden table", seed, variation).VaseFlower!.Plant));
                var bounds = plant.Bounds;
                Assert.True(float.IsFinite(bounds.Left) && float.IsFinite(bounds.Top) && float.IsFinite(bounds.Right) && float.IsFinite(bounds.Bottom));
                Assert.InRange(bounds.Left, 0.02f, 0.98f);
                Assert.InRange(bounds.Right, 0.02f, 0.98f);
                Assert.True(bounds.Top > TulipGeometry.TopMargin - 0.015f, $"seed {seed} at {variation} reached the top margin: {bounds.Top}");
                Assert.True(bounds.Bottom < 0.94f);
                Assert.InRange(plant.Bloom.Width, 0.03f, 0.12f);
                Assert.InRange(plant.Bloom.Height, 0.03f, 0.14f);
                Assert.InRange(plant.StemThickness, 0.002f, 0.01f);
            }
        }
    }

    [Fact]
    public void VaseGlazeAndSeedStreamsDoNotReshapeTheFlower()
    {
        var blue = Scene("a red tulip in a blue ceramic vase on a wooden table", 20261105, 0.35).VaseFlower!.Plant;
        var cream = Scene("a red tulip in a cream ceramic vase on a wooden table", 20261105, 0.35).VaseFlower!.Plant;
        Assert.Equal(Signature(blue), Signature(cream));

        // Different seeds do change the shape, so the geometry is not a constant.
        var other = Scene("a red tulip in a blue ceramic vase on a wooden table", 1337, 0.35).VaseFlower!.Plant;
        Assert.NotEqual(Signature(blue), Signature(other));
    }

    [Fact]
    public void ValidationRejectsBrokenArrangements()
    {
        var prompt = "a red tulip in a blue ceramic vase on a wooden table";
        var scene = Scene(prompt);
        var flower = scene.VaseFlower!;
        var plant = flower.Plant;

        Assert.Equal(
            "Only a vase can hold a flower.",
            SceneValidator.Validate(scene with { VaseFlower = flower with { Vase = flower.Vase with { Kind = CeramicKind.Bowl } } }));
        Assert.Equal(
            "Only a tulip is supported.",
            SceneValidator.Validate(scene with { VaseFlower = flower with { Flower = (FlowerKind)9 } }));
        Assert.Equal(
            "A tulip must be red or yellow.",
            SceneValidator.Validate(scene with { VaseFlower = flower with { Color = (TulipColor)9 } }));
        Assert.Equal(
            "A vase flower must describe a stem, exactly two leaves and one bloom of three to five petals.",
            SceneValidator.Validate(scene with { VaseFlower = flower with { Plant = plant with { Leaves = plant.Leaves.RemoveAt(0) } } }));
        Assert.Equal(
            "A vase flower must describe a stem, exactly two leaves and one bloom of three to five petals.",
            SceneValidator.Validate(scene with { VaseFlower = flower with { Plant = plant with { Stem = [] } } }));
        Assert.Equal(
            "Flower geometry must be finite and positive.",
            SceneValidator.Validate(scene with { VaseFlower = flower with { Plant = plant with { StemThickness = float.NaN } } }));
        Assert.Equal(
            "Flower geometry must be finite and positive.",
            SceneValidator.Validate(scene with { VaseFlower = flower with { Plant = plant with { Bloom = plant.Bloom with { Width = -0.01f } } } }));
        Assert.Equal(
            "A stem must enter through the vase opening rather than the exterior wall.",
            SceneValidator.Validate(scene with { VaseFlower = flower with { Plant = plant with { StemBase = new Vector2(0.2f, 0.4f) } } }));
        Assert.Equal(
            "A stem must rise clear of the vase rim.",
            SceneValidator.Validate(scene with { VaseFlower = flower with { Plant = plant with { StemTop = new Vector2(0.5f, flower.MouthTop) } } }));
        Assert.Equal(
            "A bloom must sit above the vase opening, clear of the rim.",
            SceneValidator.Validate(scene with { VaseFlower = flower with { Plant = plant with { Bloom = plant.Bloom with { Base = new Vector2(0.5f, flower.MouthTop) } } } }));
        Assert.Equal(
            "Every leaf must stay above the vase opening.",
            SceneValidator.Validate(scene with { VaseFlower = flower with { Plant = plant with { Leaves = plant.Leaves.SetItem(0, plant.Leaves[0] with { Tip = new Vector2(0.4f, flower.MouthTop) }) } } }));

        // Another object in the scene, and a vase the scene does not hold, are both refused.
        var group = Scene("a blue ceramic vase and a red apple on a wooden table");
        Assert.Equal(
            "A flower scene must contain exactly that vase and no other object.",
            SceneValidator.Validate(group with { VaseFlower = VaseFlower.Create(group.Ceramics[0], TulipColor.Red, 42, 0.35) }));
        var twoVases = Scene("a blue ceramic vase and a cream ceramic vase on a wooden table");
        Assert.Equal(
            "A flower scene must contain exactly that vase and no other object.",
            SceneValidator.Validate(twoVases with { VaseFlower = VaseFlower.Create(twoVases.Ceramics[0], TulipColor.Red, 42, 0.35) }));
        var other = Scene("a cream ceramic vase on a wooden table").Ceramics[0];
        Assert.Equal(
            "A flower scene must contain exactly that vase and no other object.",
            SceneValidator.Validate(scene with { VaseFlower = VaseFlower.Create(other, TulipColor.Red, 42, 0.35) }));
    }

    [Fact]
    public void ContentsRelationshipsAreMutuallyExclusive()
    {
        var scene = Scene("a red tulip in a blue ceramic vase on a wooden table");
        var bowl = Scene("a red apple in a blue ceramic bowl on a wooden table").Containment!;
        Assert.Equal(
            "A scene cannot hold a flower and another contents relationship at the same time.",
            SceneValidator.Validate(scene with { Containment = bowl }));

        var cup = Scene("a blue ceramic cup filled with coffee on a wooden table").CupContents!;
        Assert.Equal(
            "A scene cannot hold a flower and another contents relationship at the same time.",
            SceneValidator.Validate(scene with { CupContents = cup }));
    }

    [Fact]
    public void ExistingPromptsKeepTheirOwnIdentities()
    {
        var vase = Scene("a blue ceramic vase on a wooden table");
        Assert.Null(vase.VaseFlower);
        Assert.Equal("ceramic-vase", vase.SubjectToken);

        var bowl = Scene("a red apple in a blue ceramic bowl on a wooden table");
        Assert.Null(bowl.VaseFlower);
        Assert.NotNull(bowl.Containment);
        Assert.Equal("apple-in-blue-bowl", bowl.SubjectToken);

        var filled = Scene("a cream ceramic cup filled with tea on a wooden table");
        Assert.Null(filled.VaseFlower);
        Assert.NotNull(filled.CupContents);
        Assert.Equal("tea-in-cream-cup", filled.SubjectToken);

        var group = Scene("a blue ceramic vase and a red apple on a wooden table");
        Assert.Null(group.VaseFlower);
        Assert.Equal(2, group.Objects.Length);
    }

    private static string Signature(TulipPlant plant) => string.Join(
        "|",
        CurveBuilder.Extent(plant.Stem).Left.ToString("F6", CultureInfo.InvariantCulture),
        CurveBuilder.Extent(plant.Stem).Top.ToString("F6", CultureInfo.InvariantCulture),
        plant.StemThickness.ToString("F6", CultureInfo.InvariantCulture),
        string.Join(",", plant.Leaves.Select(leaf => Signature(leaf.Outline) + ":" + Signature(leaf.Vein) + ":" + leaf.Tip.X.ToString("F6", CultureInfo.InvariantCulture))),
        Signature(plant.Bloom));

    private static string Signature(TulipBloom bloom) => string.Join(
        "|",
        bloom.Base.X.ToString("F6", CultureInfo.InvariantCulture),
        bloom.Base.Y.ToString("F6", CultureInfo.InvariantCulture),
        bloom.Width.ToString("F6", CultureInfo.InvariantCulture),
        bloom.Height.ToString("F6", CultureInfo.InvariantCulture),
        string.Join(",", bloom.Petals.Select(petal => Signature(petal.Outline) + ":" + petal.Tone.ToString("F6", CultureInfo.InvariantCulture))));

    private static string Signature(ImmutableArray<CubicSegment> segments) => string.Join(
        ";",
        segments.SelectMany(segment => new[] { segment.Start, segment.Control1, segment.Control2, segment.End })
            .Select(point => point.X.ToString("F6", CultureInfo.InvariantCulture) + "," + point.Y.ToString("F6", CultureInfo.InvariantCulture)));

    private static StillLifeScene Scene(string prompt, ulong seed = 20261105, double variation = 0.35) =>
        SceneGenerator.Generate(new(prompt, seed, variation)).Scene!;
}
