using System.Collections.Immutable;
using System.Globalization;
using System.Numerics;
using Formloom.Core;
using Xunit;

namespace Formloom.Tests;

public sealed class VaseDaisyTests
{
    private static readonly (string Prompt, CeramicGlaze Glaze)[] Combinations =
    [
        ("a white daisy in a blue ceramic vase on a wooden table", CeramicGlaze.Blue),
        ("a white daisy in a cream ceramic vase on a wooden table", CeramicGlaze.Cream),
    ];

    // Every daisy phrasing the grammar must refuse whole. Each row names a colour, species,
    // container, quantity or relationship the milestone deliberately does not support, and a
    // refused daisy must never degrade into an empty vase or into the tulip it resembles.
    public static readonly string[] Rejected =
    [
        "a red daisy in a blue ceramic vase on a wooden table",
        "a yellow daisy in a cream ceramic vase on a wooden table",
        "a pink daisy in a blue ceramic vase on a wooden table",
        "a blue daisy in a cream ceramic vase on a wooden table",
        "a daisy in a blue ceramic vase on a wooden table",
        "a white flower in a blue ceramic vase on a wooden table",
        "a white rose in a blue ceramic vase on a wooden table",
        "a white sunflower in a cream ceramic vase on a wooden table",
        "a white daisy in a blue ceramic bowl on a wooden table",
        "a white daisy in a cream ceramic cup on a wooden table",
        "a white daisy in a ceramic vase on a wooden table",
        "a white daisy in a red ceramic vase on a wooden table",
        "a white daisy in a blue ceramic vase on a table",
        "a white daisy in a blue ceramic vase",
        "white daisy in a blue ceramic vase on a wooden table",
        "a white daisy inside a blue ceramic vase on a wooden table",
        "a white daisy within a blue ceramic vase on a wooden table",
        "a blue ceramic vase with a white daisy on a wooden table",
        "a blue ceramic vase holding a white daisy on a wooden table",
        "white daisies in a blue ceramic vase on a wooden table",
        "a bunch of white daisies in a blue ceramic vase on a wooden table",
        "a bouquet of white daisies in a cream ceramic vase on a wooden table",
        "two white daisies in a blue ceramic vase on a wooden table",
        "a white daisy and a red tulip in a blue ceramic vase on a wooden table",
        "a white daisy in two blue ceramic vases on a wooden table",
        "a white daisy in a blue ceramic vase on a wooden table and a red apple",
        "a white daisy in a blue ceramic vase on a wooden table, a red apple",
        "a large white daisy in a blue ceramic vase on a wooden table",
        "a small white daisy in a cream ceramic vase on a wooden table",
        "a white daisy in a blue ceramic vase with a stem on a wooden table",
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

    public static TheoryData<string, string> ParsedCases()
    {
        var data = new TheoryData<string, string>();
        foreach (var (prompt, glaze) in Combinations)
        {
            data.Add(prompt, glaze.ToString());
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ParsedCases))]
    public void EveryCombinationParsesAndGenerates(string prompt, string glaze)
    {
        var expectedGlaze = Enum.Parse<CeramicGlaze>(glaze);
        var parsed = PromptParser.ParseVaseDaisy(prompt);
        Assert.NotNull(parsed);
        Assert.Equal(DaisyAppearance.WhiteWithGoldenDisk, parsed.Appearance);
        Assert.Equal(CeramicKind.Vase, parsed.Vase.Kind);
        Assert.Equal(expectedGlaze, parsed.Vase.Glaze);

        var generated = SceneGenerator.Generate(new(prompt));
        Assert.Null(generated.Error);
        var scene = generated.Scene!;
        var daisy = scene.VaseDaisy;
        Assert.NotNull(daisy);
        Assert.Equal(FlowerKind.Daisy, daisy.Flower);
        Assert.Equal(DaisyAppearance.WhiteWithGoldenDisk, daisy.Appearance);
        Assert.Equal(CeramicKind.Vase, daisy.Vase.Kind);
        Assert.Equal(expectedGlaze, daisy.Vase.Glaze);
        Assert.Equal($"white-daisy-in-{glaze.ToLowerInvariant()}-vase", scene.SubjectToken);
        Assert.Equal(daisy.SubjectToken, scene.SubjectToken);
        Assert.Null(SceneValidator.Validate(scene));
    }

    [Theory]
    [MemberData(nameof(RejectedCases))]
    public void UnsupportedDaisyPromptsAreRejectedWhole(string prompt)
    {
        Assert.Null(PromptParser.ParseVaseDaisy(prompt));
        var generated = SceneGenerator.Generate(new(prompt));
        Assert.False(generated.IsSuccess);
        Assert.NotNull(generated.Error);

        // A refused daisy prompt never degrades into an empty vase or into a tulip.
        Assert.Null(generated.Scene);
    }

    [Fact]
    public void ParsingNormalizesCaseWhitespaceAndPunctuation()
    {
        foreach (var variant in new[]
        {
            "A WHITE DAISY IN A BLUE CERAMIC VASE ON A WOODEN TABLE",
            "  a white daisy in a blue ceramic vase on a wooden table  ",
            "a   white   daisy in a blue ceramic vase on a wooden table",
            "a white daisy in a blue ceramic vase on a wooden table.",
            "a white daisy in a blue ceramic vase on a wooden table!",
        })
        {
            var parsed = PromptParser.ParseVaseDaisy(variant);
            Assert.NotNull(parsed);
            Assert.Equal(DaisyAppearance.WhiteWithGoldenDisk, parsed.Appearance);
            Assert.Equal(CeramicGlaze.Blue, parsed.Vase.Glaze);
        }
    }

    [Fact]
    public void DaisyIsTypedVaseContentNotASceneEntry()
    {
        var scene = Scene("a white daisy in a cream ceramic vase on a wooden table");
        var daisy = scene.VaseDaisy!;
        var objects = scene.Objects;
        Assert.Single(objects);
        Assert.True(objects[0].IsCeramic);
        Assert.Equal(objects[0].Ceramic, daisy.Vase);
        Assert.Same(scene.Ceramics[0], objects[0].Ceramic);
        Assert.Same(scene.Ceramics[0], daisy.Vase);
        Assert.Empty(scene.Fruits);
        Assert.Null(scene.Containment);
        Assert.Null(scene.CupContents);
        Assert.Null(scene.VaseFlower);
        Assert.Equal("white-daisy-in-cream-vase", scene.SubjectToken);
    }

    [Fact]
    public void StemAttachmentFollowsTheVaseOpening()
    {
        foreach (var (prompt, _) in Combinations)
        {
            foreach (var variation in new[] { 0, 0.35, 0.6, 1 })
            {
                var scene = Scene(prompt, 1337, variation);
                var daisy = scene.VaseDaisy!;
                var plant = daisy.Plant;
                var center = daisy.OpeningCenter;
                var radius = daisy.OpeningRadius;
                var dx = (plant.StemBase.X - center.X) / radius.X;
                var dy = (plant.StemBase.Y - center.Y) / radius.Y;
                Assert.True((dx * dx) + (dy * dy) < 0.95f, "the stem must enter through the opening");
                Assert.True(plant.StemBase.Y > daisy.MouthTop, "the stem base must sit behind the rim");
                Assert.True(plant.StemTop.Y < daisy.MouthTop - 0.02f, "the stem must rise clear of the rim");
                Assert.True(plant.Bloom.Top < daisy.MouthTop - 0.05f, "the bloom must clear the rim");
                Assert.True(plant.Bloom.Bottom < daisy.MouthTop - 0.01f, "the whole head must clear the rim");
                Assert.Equal(plant.StemTop.X, plant.Bloom.Center.X);
                Assert.True(plant.Bloom.Center.Y < plant.StemTop.Y, "the head sits on the top of the stem");
                Assert.True(plant.StemThickness > 0);
                Assert.Single(plant.Stem);
            }
        }
    }

    [Fact]
    public void TheBloomIsRadialWithAFixedPetalCount()
    {
        const float turn = MathF.Tau / DaisyGeometry.PetalCount;
        foreach (var (prompt, _) in Combinations)
        {
            foreach (var seed in new ulong[] { 0, 42, 1337, 20261105 })
            {
                foreach (var variation in new[] { 0, 0.5, 1 })
                {
                    var bloom = Scene(prompt, seed, variation).VaseDaisy!.Plant.Bloom;
                    Assert.Equal(DaisyGeometry.PetalCount, bloom.Petals.Length);
                    Assert.Equal(DaisyGeometry.PetalCount, bloom.Petals.Select(petal => petal.Angle).Distinct().Count());

                    // Every petal radiates from the same centre at an even spacing: the jitter may
                    // reshape a petal but it may never collapse the fan or close the gaps.
                    var angles = bloom.Petals.Select(petal => petal.Angle).OrderBy(angle => angle).ToArray();
                    for (var index = 0; index < angles.Length; index++)
                    {
                        var delta = index + 1 < angles.Length
                            ? angles[index + 1] - angles[index]
                            : (angles[0] + MathF.Tau) - angles[index];
                        Assert.InRange(delta, turn * 0.55f, turn * 1.45f);
                    }

                    Assert.Equal(bloom.Center, bloom.Disk.Center);
                    Assert.InRange(bloom.Disk.RadiusX, bloom.RadiusX * 0.30f, bloom.RadiusX * 0.38f);
                    Assert.InRange(bloom.Disk.RadiusY, bloom.Disk.RadiusX * 0.80f, bloom.Disk.RadiusX * 0.92f);

                    foreach (var petal in bloom.Petals)
                    {
                        Assert.False(petal.Outline.IsDefault);
                        Assert.InRange(petal.Tone, 0f, 1f);

                        // The roots hide behind the golden disk and the tips reach past it, in the
                        // bloom's own squashed space.
                        var root = Normalize(petal.Root - bloom.Center, bloom);
                        var tip = Normalize(petal.Tip - bloom.Center, bloom);
                        Assert.True(root.Length() < 0.62f, $"root {root.Length()} reaches past the disk");
                        Assert.InRange(tip.Length(), 0.9f, 1.35f);
                    }

                    // Both value bands are present, so the head is shaded rather than flat.
                    Assert.True(bloom.Petals.Count(petal => petal.Tone < 0.7f) >= 5);
                    Assert.True(bloom.Petals.Count(petal => petal.Tone > 0.8f) >= 5);
                }
            }
        }
    }

    [Fact]
    public void ExactlyTwoLeavesHangFromTheStem()
    {
        foreach (var (prompt, _) in Combinations)
        {
            foreach (var seed in new ulong[] { 0, 42, 1337, 20261105 })
            {
                foreach (var variation in new[] { 0, 0.5, 1 })
                {
                    var daisy = Scene(prompt, seed, variation).VaseDaisy!;
                    var plant = daisy.Plant;
                    Assert.Equal(2, plant.Leaves.Length);
                    Assert.All(plant.Leaves, leaf =>
                    {
                        Assert.False(leaf.Outline.IsDefault);
                        Assert.False(leaf.Vein.IsDefault);
                        Assert.True(leaf.Width > 0);
                        Assert.True(
                            DaisyGeometry.DistanceToStem(plant, leaf.Attachment) < 0.002f,
                            "every leaf hangs from the stem");
                        Assert.True(leaf.Tip.Y < daisy.MouthTop - 0.004f, "every leaf clears the rim");
                    });

                    // One leaf to each side, at different heights, so the head never hides them.
                    Assert.NotEqual(plant.Leaves[0].Attachment, plant.Leaves[1].Attachment);
                    Assert.True(plant.Leaves[0].Tip.X < plant.Leaves[0].Attachment.X);
                    Assert.True(plant.Leaves[1].Tip.X > plant.Leaves[1].Attachment.X);
                }
            }
        }
    }

    [Fact]
    public void GeometryIsDeterministicAndBoundedAcrossSeeds()
    {
        const string prompt = "a white daisy in a cream ceramic vase on a wooden table";
        foreach (var seed in new ulong[] { 0, 7, 42, 1337, 20261105, ulong.MaxValue })
        {
            foreach (var variation in new[] { 0, 0.25, 0.5, 0.75, 1 })
            {
                var scene = Scene(prompt, seed, variation);
                Assert.Null(SceneValidator.Validate(scene));
                var plant = scene.VaseDaisy!.Plant;
                Assert.Equal(Signature(plant), Signature(Scene(prompt, seed, variation).VaseDaisy!.Plant));

                var bounds = plant.Bounds;
                Assert.True(float.IsFinite(bounds.Left) && float.IsFinite(bounds.Top) && float.IsFinite(bounds.Right) && float.IsFinite(bounds.Bottom));
                Assert.InRange(bounds.Left, 0.02f, 0.98f);
                Assert.InRange(bounds.Right, 0.02f, 0.98f);
                Assert.True(bounds.Top > DaisyGeometry.TopMargin - 0.012f, $"seed {seed} at {variation} reached the top margin: {bounds.Top}");
                Assert.True(bounds.Bottom < 0.94f);
                Assert.True(bounds.Top < plant.Bloom.Top);
                Assert.True(bounds.Bottom > plant.StemBase.Y);
            }
        }
    }

    [Fact]
    public void VaseGlazeAndSeedStreamsDoNotReshapeTheDaisy()
    {
        var blue = Scene("a white daisy in a blue ceramic vase on a wooden table", 20261105, 0.35).VaseDaisy!.Plant;
        var cream = Scene("a white daisy in a cream ceramic vase on a wooden table", 20261105, 0.35).VaseDaisy!.Plant;
        Assert.Equal(Signature(blue), Signature(cream));

        // Different seeds do change the shape, so the geometry is not a constant.
        var other = Scene("a white daisy in a blue ceramic vase on a wooden table", 1337, 0.35).VaseDaisy!.Plant;
        Assert.NotEqual(Signature(blue), Signature(other));

        // The daisy owns its own streams: it neither shares the tulip's shapes nor perturbs the
        // tulip when both are generated from the same seed.
        var tulipBefore = Scene("a red tulip in a blue ceramic vase on a wooden table", 42, 0.35).VaseFlower!.Plant;
        _ = Scene("a white daisy in a blue ceramic vase on a wooden table", 42, 0.35);
        var tulipAfter = Scene("a red tulip in a blue ceramic vase on a wooden table", 42, 0.35).VaseFlower!.Plant;
        Assert.Equal(VaseFlowerTestsSignature(tulipBefore), VaseFlowerTestsSignature(tulipAfter));
        var daisy = Scene("a white daisy in a blue ceramic vase on a wooden table", 42, 0.35).VaseDaisy!.Plant;
        Assert.NotEqual(tulipBefore.StemThickness, daisy.StemThickness);
    }

    [Fact]
    public void ValidationRejectsBrokenDaisyArrangements()
    {
        var scene = Scene("a white daisy in a blue ceramic vase on a wooden table");
        var daisy = scene.VaseDaisy!;
        var plant = daisy.Plant;
        var bloom = plant.Bloom;

        Assert.Equal(
            "Only a vase can hold a flower.",
            SceneValidator.Validate(scene with { VaseDaisy = daisy with { Vase = daisy.Vase with { Kind = CeramicKind.Bowl } } }));
        Assert.Equal(
            "A vase must have a blue or cream glaze.",
            SceneValidator.Validate(scene with { VaseDaisy = daisy with { Vase = daisy.Vase with { Glaze = (CeramicGlaze)9 } } }));
        Assert.Equal(
            "A daisy must have white petals and a golden centre.",
            SceneValidator.Validate(scene with { VaseDaisy = daisy with { Appearance = (DaisyAppearance)9 } }));
        Assert.Equal(
            "A vase daisy must describe a stem, exactly two leaves and one bloom of twelve petals.",
            SceneValidator.Validate(scene with { VaseDaisy = daisy with { Plant = plant with { Leaves = plant.Leaves.RemoveAt(0) } } }));
        Assert.Equal(
            "A vase daisy must describe a stem, exactly two leaves and one bloom of twelve petals.",
            SceneValidator.Validate(scene with { VaseDaisy = daisy with { Plant = plant with { Bloom = bloom with { Petals = bloom.Petals.RemoveAt(0) } } } }));
        Assert.Equal(
            "A vase daisy must describe a stem, exactly two leaves and one bloom of twelve petals.",
            SceneValidator.Validate(scene with { VaseDaisy = daisy with { Plant = plant with { Stem = [] } } }));
        Assert.Equal(
            "Daisy geometry must be finite and positive.",
            SceneValidator.Validate(scene with { VaseDaisy = daisy with { Plant = plant with { StemThickness = float.NaN } } }));
        Assert.Equal(
            "Daisy geometry must be finite and positive.",
            SceneValidator.Validate(scene with { VaseDaisy = daisy with { Plant = plant with { Bloom = bloom with { RadiusX = -0.01f } } } }));
        Assert.Equal(
            "A stem must enter through the vase opening rather than the exterior wall.",
            SceneValidator.Validate(scene with { VaseDaisy = daisy with { Plant = plant with { StemBase = new Vector2(0.2f, 0.4f) } } }));
        Assert.Equal(
            "A stem must rise clear of the vase rim.",
            SceneValidator.Validate(scene with { VaseDaisy = daisy with { Plant = plant with { StemTop = new Vector2(bloom.Center.X, daisy.MouthTop) } } }));
        Assert.Equal(
            "A bloom must sit above the vase opening, clear of the rim.",
            SceneValidator.Validate(scene with { VaseDaisy = daisy with { Plant = plant with { Bloom = bloom with { Center = new Vector2(bloom.Center.X, daisy.MouthTop) } } } }));
        Assert.Equal(
            "Every leaf must stay above the vase opening.",
            SceneValidator.Validate(scene with { VaseDaisy = daisy with { Plant = plant with { Leaves = plant.Leaves.SetItem(0, plant.Leaves[0] with { Tip = new Vector2(0.4f, daisy.MouthTop) }) } } }));

        // Another object in the scene, and a vase the scene does not hold, are both refused.
        var group = Scene("a blue ceramic vase and a red apple on a wooden table");
        Assert.Equal(
            "A flower scene must contain exactly that vase and no other object.",
            SceneValidator.Validate(group with { VaseDaisy = VaseDaisy.Create(group.Ceramics[0], DaisyAppearance.WhiteWithGoldenDisk, 42, 0.35) }));
        var other = Scene("a cream ceramic vase on a wooden table").Ceramics[0];
        Assert.Equal(
            "A flower scene must contain exactly that vase and no other object.",
            SceneValidator.Validate(scene with { VaseDaisy = VaseDaisy.Create(other, DaisyAppearance.WhiteWithGoldenDisk, 42, 0.35) }));
    }

    [Fact]
    public void DaisyIsMutuallyExclusiveWithOtherContents()
    {
        var scene = Scene("a white daisy in a blue ceramic vase on a wooden table");
        var bowl = Scene("a red apple in a blue ceramic bowl on a wooden table").Containment!;
        Assert.Equal(
            "A scene cannot hold a flower and another contents relationship at the same time.",
            SceneValidator.Validate(scene with { Containment = bowl }));

        var cup = Scene("a blue ceramic cup filled with coffee on a wooden table").CupContents!;
        Assert.Equal(
            "A scene cannot hold a flower and another contents relationship at the same time.",
            SceneValidator.Validate(scene with { CupContents = cup }));

        var tulip = Scene("a red tulip in a blue ceramic vase on a wooden table").VaseFlower!;
        Assert.Equal(
            "A scene cannot hold two flower arrangements at the same time.",
            SceneValidator.Validate(scene with { VaseFlower = tulip }));
    }

    [Fact]
    public void ExistingPromptsKeepTheirOwnIdentities()
    {
        var vase = Scene("a blue ceramic vase on a wooden table");
        Assert.Null(vase.VaseDaisy);
        Assert.Equal("ceramic-vase", vase.SubjectToken);

        var tulip = Scene("a red tulip in a blue ceramic vase on a wooden table");
        Assert.Null(tulip.VaseDaisy);
        Assert.Equal("red-tulip-in-blue-vase", tulip.SubjectToken);

        var group = Scene("a blue ceramic vase and a red apple on a wooden table");
        Assert.Null(group.VaseDaisy);
        Assert.Equal(2, group.Objects.Length);
    }

    // The bloom's own space is squashed vertically, so distances are measured in that space.
    private static Vector2 Normalize(Vector2 offset, DaisyBloom bloom) =>
        new(offset.X / bloom.RadiusX, offset.Y / bloom.RadiusY);

    private static string Signature(DaisyPlant plant) => string.Join(
        "|",
        plant.StemBase.X.ToString("F6", CultureInfo.InvariantCulture),
        plant.StemBase.Y.ToString("F6", CultureInfo.InvariantCulture),
        plant.StemTop.X.ToString("F6", CultureInfo.InvariantCulture),
        plant.StemTop.Y.ToString("F6", CultureInfo.InvariantCulture),
        plant.StemThickness.ToString("F6", CultureInfo.InvariantCulture),
        string.Join(",", plant.Leaves.Select(leaf => Signature(leaf.Outline) + ":" + Signature(leaf.Vein) + ":" + leaf.Tip.X.ToString("F6", CultureInfo.InvariantCulture))),
        Signature(plant.Bloom));

    private static string Signature(DaisyBloom bloom) => string.Join(
        "|",
        bloom.Center.X.ToString("F6", CultureInfo.InvariantCulture),
        bloom.Center.Y.ToString("F6", CultureInfo.InvariantCulture),
        bloom.RadiusX.ToString("F6", CultureInfo.InvariantCulture),
        bloom.RadiusY.ToString("F6", CultureInfo.InvariantCulture),
        bloom.Disk.RadiusX.ToString("F6", CultureInfo.InvariantCulture),
        string.Join(",", bloom.Petals.Select(petal => Signature(petal.Outline) + ":" + petal.Tone.ToString("F6", CultureInfo.InvariantCulture))));

    private static string Signature(ImmutableArray<CubicSegment> segments) => string.Join(
        ";",
        segments.SelectMany(segment => new[] { segment.Start, segment.Control1, segment.Control2, segment.End })
            .Select(point => point.X.ToString("F6", CultureInfo.InvariantCulture) + "," + point.Y.ToString("F6", CultureInfo.InvariantCulture)));

    // The tulip's own rounded signature, kept local so the daisy test can prove the tulip's
    // streams are untouched without depending on another test class's private helper.
    private static string VaseFlowerTestsSignature(TulipPlant plant) => string.Join(
        "|",
        plant.StemBase.X.ToString("F6", CultureInfo.InvariantCulture),
        plant.StemBase.Y.ToString("F6", CultureInfo.InvariantCulture),
        plant.StemThickness.ToString("F6", CultureInfo.InvariantCulture),
        string.Join(",", plant.Leaves.Select(leaf => Signature(leaf.Outline))),
        plant.Bloom.Base.X.ToString("F6", CultureInfo.InvariantCulture),
        string.Join(",", plant.Bloom.Petals.Select(petal => Signature(petal.Outline))));

    private static StillLifeScene Scene(string prompt, ulong seed = 20261105, double variation = 0.35) =>
        SceneGenerator.Generate(new(prompt, seed, variation)).Scene!;
}
