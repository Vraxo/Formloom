using System.Collections.Immutable;
using System.Numerics;
using System.Text.Json;
using Formloom.Core;
using Xunit;

namespace Formloom.Tests;

public sealed class VaseTulipDaisyTests
{
    private static readonly (string Prompt, TulipColor TulipColor, CeramicGlaze Glaze)[] Combinations =
    [
        ("a red tulip and a white daisy in a blue ceramic vase on a wooden table", TulipColor.Red, CeramicGlaze.Blue),
        ("a red tulip and a white daisy in a cream ceramic vase on a wooden table", TulipColor.Red, CeramicGlaze.Cream),
        ("a yellow tulip and a white daisy in a blue ceramic vase on a wooden table", TulipColor.Yellow, CeramicGlaze.Blue),
        ("a yellow tulip and a white daisy in a cream ceramic vase on a wooden table", TulipColor.Yellow, CeramicGlaze.Cream),
    ];

    public static TheoryData<string, string, string> CombinationCases()
    {
        var data = new TheoryData<string, string, string>();
        foreach (var (prompt, tulipColor, glaze) in Combinations)
        {
            data.Add(prompt, tulipColor.ToString(), glaze.ToString());
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(CombinationCases))]
    public void FourMixedPromptsParseIntoOneTypedVaseArrangement(string prompt, string tulipColor, string glaze)
    {
        var expectedColor = Enum.Parse<TulipColor>(tulipColor);
        var expectedGlaze = Enum.Parse<CeramicGlaze>(glaze);
        var parsed = PromptParser.ParseVaseTulipDaisy(prompt);
        Assert.NotNull(parsed);
        Assert.Equal(expectedColor, parsed.TulipColor);
        Assert.Equal(DaisyAppearance.WhiteWithGoldenDisk, parsed.DaisyAppearance);
        Assert.Equal(CeramicKind.Vase, parsed.Vase.Kind);
        Assert.Equal(expectedGlaze, parsed.Vase.Glaze);

        var generated = SceneGenerator.Generate(new(prompt));
        Assert.Null(generated.Error);
        var scene = Assert.IsType<StillLifeScene>(generated.Scene);
        var pair = Assert.IsType<VaseTulipDaisy>(scene.VaseTulipDaisy);
        Assert.Equal(expectedColor, pair.TulipColor);
        Assert.Equal(DaisyAppearance.WhiteWithGoldenDisk, pair.DaisyAppearance);
        Assert.Equal(expectedGlaze, pair.Vase.Glaze);
        Assert.Equal($"{tulipColor.ToLowerInvariant()}-tulip-and-white-daisy-in-{glaze.ToLowerInvariant()}-vase", scene.SubjectToken);
        Assert.Equal(pair.SubjectToken, scene.SubjectToken);
        Assert.Null(scene.VaseFlower);
        Assert.Null(scene.VaseDaisy);
        Assert.Null(scene.Containment);
        Assert.Null(scene.CupContents);
        Assert.Null(SceneValidator.Validate(scene));
    }

    [Theory]
    [InlineData("a white daisy and a red tulip in a blue ceramic vase on a wooden table")]
    [InlineData("a yellow tulip and a red tulip in a blue ceramic vase on a wooden table")]
    [InlineData("a red tulip and two white daisies in a cream ceramic vase on a wooden table")]
    [InlineData("a red tulip and a yellow daisy in a blue ceramic vase on a wooden table")]
    [InlineData("a red tulip and a white daisy in a blue ceramic bowl on a wooden table")]
    [InlineData("a red tulip and a white daisy in a cream ceramic cup on a wooden table")]
    [InlineData("a red tulip and a white daisy in a ceramic vase on a wooden table")]
    [InlineData("a red tulip and a white daisy in a red ceramic vase on a wooden table")]
    [InlineData("a red tulip and a white daisy in a blue ceramic vase on a table")]
    [InlineData("a red tulip and a white daisy in a blue ceramic vase")]
    [InlineData("red tulip and white daisy in a blue ceramic vase on a wooden table")]
    [InlineData("a red tulip with a white daisy in a blue ceramic vase on a wooden table")]
    [InlineData("a blue ceramic vase with a red tulip and a white daisy on a wooden table")]
    [InlineData("a red tulip and a white daisy in a blue ceramic vase on a wooden table and a red apple")]
    [InlineData("a red tulip and a white daisy in two blue ceramic vases on a wooden table")]
    [InlineData("a large red tulip and a white daisy in a blue ceramic vase on a wooden table")]
    public void UnsupportedMixedPromptsAreRejectedWhole(string prompt)
    {
        Assert.Null(PromptParser.ParseVaseTulipDaisy(prompt));
        var generated = SceneGenerator.Generate(new(prompt));
        Assert.False(generated.IsSuccess);
        Assert.Null(generated.Scene);
        Assert.NotNull(generated.Error);
    }

    [Fact]
    public void MixedPromptKeepsTheSharedCaseWhitespaceAndPunctuationNormalization()
    {
        foreach (var prompt in new[]
        {
            "A RED TULIP AND A WHITE DAISY IN A BLUE CERAMIC VASE ON A WOODEN TABLE",
            "  a red tulip and a white daisy in a blue ceramic vase on a wooden table  ",
            "a   red   tulip   and   a   white   daisy in a blue ceramic vase on a wooden table",
            "a red tulip and a white daisy in a blue ceramic vase on a wooden table.",
            "a red tulip and a white daisy in a blue ceramic vase on a wooden table!",
            "a red tulip and a white daisy in a blue ceramic vase on a wooden table?",
        })
        {
            Assert.NotNull(PromptParser.ParseVaseTulipDaisy(prompt));
            Assert.NotNull(SceneGenerator.Generate(new(prompt)).Scene);
        }
    }

    [Fact]
    public void MixedFlowersAreTypedContentsOfTheSameSingleVaseEntry()
    {
        var scene = Scene(Combinations[1].Prompt);
        var pair = scene.VaseTulipDaisy!;
        var objects = scene.Objects;
        Assert.Single(objects);
        Assert.True(objects[0].IsCeramic);
        Assert.Same(objects[0].Ceramic, pair.Vase);
        Assert.Same(scene.Ceramics[0], pair.Vase);
        Assert.Empty(scene.Fruits);
        Assert.Equal(CeramicKind.Vase, pair.Vase.Kind);
        Assert.Equal(CeramicGlaze.Cream, pair.Vase.Glaze);
        Assert.Equal(2, pair.Tulip.Leaves.Length);
        Assert.Equal(5, pair.Tulip.Bloom.Petals.Length);
        Assert.Equal(2, pair.Daisy.Leaves.Length);
        Assert.Equal(DaisyGeometry.PetalCount, pair.Daisy.Bloom.Petals.Length);
        Assert.Equal("red-tulip-and-white-daisy-in-cream-vase", scene.SubjectToken);
    }

    [Fact]
    public void LayoutKeepsTheTwoStemsAndBloomsDistinctAcrossSeedsAndVariation()
    {
        foreach (var (prompt, _, _) in Combinations)
        {
            foreach (var seed in new ulong[] { 0, 1, 42, 1337, 8675309, 20261105, ulong.MaxValue })
            {
                foreach (var variation in new[] { 0, 0.35, 0.6, 1 })
                {
                    var scene = Scene(prompt, seed, variation);
                    var pair = scene.VaseTulipDaisy!;
                    Assert.Single(scene.Objects);
                    Assert.Same(scene.Ceramics[0], pair.Vase);
                    Assert.Equal(2, pair.Tulip.Leaves.Length);
                    Assert.Equal(5, pair.Tulip.Bloom.Petals.Length);
                    Assert.Equal(2, pair.Daisy.Leaves.Length);
                    Assert.Equal(DaisyGeometry.PetalCount, pair.Daisy.Bloom.Petals.Length);

                    var opening = pair.OpeningCenter;
                    var radius = pair.OpeningRadius;
                    Assert.InRange((opening.X - pair.Tulip.StemBase.X) / radius.X, 0.195f, 0.245f);
                    Assert.InRange((pair.Daisy.StemBase.X - opening.X) / radius.X, 0.195f, 0.245f);
                    Assert.True(pair.Tulip.StemBase.X < opening.X);
                    Assert.True(pair.Daisy.StemBase.X > opening.X);
                    Assert.True(pair.Tulip.Bloom.Base.X < pair.Daisy.Bloom.Center.X);
                    Assert.True(pair.Tulip.Bloom.Base.Y > pair.Daisy.Bloom.Center.Y);

                    AssertStemFitsOpening(pair.Tulip.StemBase, pair.Tulip.StemThickness, opening, radius);
                    AssertStemFitsOpening(pair.Daisy.StemBase, pair.Daisy.StemThickness, opening, radius);
                    Assert.True(pair.Tulip.StemBase.Y > pair.MouthTop);
                    Assert.True(pair.Daisy.StemBase.Y > pair.MouthTop);
                    Assert.True(pair.Tulip.StemTop.Y < pair.MouthTop - 0.004f);
                    Assert.True(pair.Daisy.StemTop.Y < pair.MouthTop - 0.004f);

                    var tulipStem = CurveBuilder.Extent(pair.Tulip.Stem);
                    var daisyStem = CurveBuilder.Extent(pair.Daisy.Stem);
                    Assert.True(
                        daisyStem.Left - tulipStem.Right > (pair.Tulip.StemThickness + pair.Daisy.StemThickness) / 2,
                        $"stem bounds must remain separated for seed={seed}, variation={variation}");

                    var tulipBloom = TulipBloomBounds(pair.Tulip);
                    var daisyBloom = DaisyBloomBounds(pair.Daisy);
                    Assert.True(
                        daisyBloom.Left - tulipBloom.Right > 0.003f,
                        $"bloom bounds must remain separated for seed={seed}, variation={variation}: gap={daisyBloom.Left - tulipBloom.Right:0.0000}");
                    Assert.True(pair.Bounds.Left > 0.02f);
                    Assert.True(pair.Bounds.Right < 0.98f);
                    Assert.True(pair.Bounds.Top > 0.02f);

                    foreach (var leaf in pair.Tulip.Leaves)
                    {
                        Assert.True(TulipGeometry.DistanceToStem(pair.Tulip, leaf.Attachment) < 0.0015f);
                        Assert.True(leaf.Attachment.Y < pair.MouthTop - 0.004f);
                        Assert.True(leaf.Tip.Y < leaf.Attachment.Y);
                    }

                    foreach (var leaf in pair.Daisy.Leaves)
                    {
                        Assert.True(DaisyGeometry.DistanceToStem(pair.Daisy, leaf.Attachment) < 0.0015f);
                        Assert.True(leaf.Attachment.Y < pair.MouthTop - 0.004f);
                        Assert.True(leaf.Tip.Y < leaf.Attachment.Y);
                    }
                }
            }
        }
    }

    [Fact]
    public void LayoutIsDeterministicAndIndependentOfTulipColourAndVaseGlaze()
    {
        const ulong seed = 20261105;
        const double variation = 0.35;
        var redBlue = Scene(Combinations[0].Prompt, seed, variation).VaseTulipDaisy!;
        var redBlueAgain = Scene(Combinations[0].Prompt, seed, variation).VaseTulipDaisy!;
        var yellowBlue = Scene(Combinations[2].Prompt, seed, variation).VaseTulipDaisy!;
        var redCream = Scene(Combinations[1].Prompt, seed, variation).VaseTulipDaisy!;

        Assert.Equal(JsonSerializer.Serialize(redBlue.Tulip), JsonSerializer.Serialize(redBlueAgain.Tulip));
        Assert.Equal(JsonSerializer.Serialize(redBlue.Daisy), JsonSerializer.Serialize(redBlueAgain.Daisy));
        Assert.Equal(JsonSerializer.Serialize(redBlue.Tulip), JsonSerializer.Serialize(yellowBlue.Tulip));
        Assert.Equal(JsonSerializer.Serialize(redBlue.Daisy), JsonSerializer.Serialize(yellowBlue.Daisy));
        Assert.Equal(JsonSerializer.Serialize(redBlue.Tulip), JsonSerializer.Serialize(redCream.Tulip));
        Assert.Equal(JsonSerializer.Serialize(redBlue.Daisy), JsonSerializer.Serialize(redCream.Daisy));
    }

    [Fact]
    public void MixedLayoutDoesNotPerturbStandaloneTulipOrDaisyGeometry()
    {
        const ulong seed = 1337;
        const double variation = 0.6;
        const string tulipPrompt = "a red tulip in a blue ceramic vase on a wooden table";
        const string daisyPrompt = "a white daisy in a blue ceramic vase on a wooden table";
        var tulipBefore = Scene(tulipPrompt, seed, variation).VaseFlower!.Plant;
        var daisyBefore = Scene(daisyPrompt, seed, variation).VaseDaisy!.Plant;
        _ = Scene(Combinations[0].Prompt, seed, variation);
        var tulipAfter = Scene(tulipPrompt, seed, variation).VaseFlower!.Plant;
        var daisyAfter = Scene(daisyPrompt, seed, variation).VaseDaisy!.Plant;

        Assert.Equal(JsonSerializer.Serialize(tulipBefore), JsonSerializer.Serialize(tulipAfter));
        Assert.Equal(JsonSerializer.Serialize(daisyBefore), JsonSerializer.Serialize(daisyAfter));
    }

    [Fact]
    public void PairValidationNamesTheInvalidHalfAndEnforcesExclusiveSharedContents()
    {
        var scene = Scene(Combinations[0].Prompt);
        var pair = scene.VaseTulipDaisy!;
        Assert.Contains("tulip must be red or yellow", SceneValidator.Validate(scene with
        {
            VaseTulipDaisy = pair with { TulipColor = (TulipColor)123 },
        }), StringComparison.Ordinal);
        Assert.Contains("daisy must have white petals", SceneValidator.Validate(scene with
        {
            VaseTulipDaisy = pair with { DaisyAppearance = (DaisyAppearance)123 },
        }), StringComparison.Ordinal);
        Assert.Contains("The tulip in the mixed arrangement is invalid", SceneValidator.Validate(scene with
        {
            VaseTulipDaisy = pair with { Tulip = null! },
        }), StringComparison.Ordinal);
        Assert.Contains("The daisy in the mixed arrangement is invalid", SceneValidator.Validate(scene with
        {
            VaseTulipDaisy = pair with { Daisy = null! },
        }), StringComparison.Ordinal);
        Assert.Contains("stems must stay distinct", SceneValidator.Validate(scene with
        {
            VaseTulipDaisy = pair with { Daisy = pair.Daisy with { StemBase = pair.Tulip.StemBase } },
        }), StringComparison.Ordinal);

        var tulip = VaseFlower.Create(pair.Vase, TulipColor.Red, scene.Seed, scene.Variation);
        Assert.Contains("two flower arrangements", SceneValidator.Validate(scene with { VaseFlower = tulip }), StringComparison.Ordinal);
        var vase = pair.Vase with { Glaze = (CeramicGlaze)123 };
        Assert.Contains("vase must have a blue or cream glaze", SceneValidator.Validate(scene with
        {
            VaseTulipDaisy = pair with { Vase = vase },
        }), StringComparison.Ordinal);

        var otherVase = Scene("a cream ceramic vase on a wooden table").Ceramics[0];
        Assert.Contains("exactly its shared vase", SceneValidator.Validate(scene with
        {
            VaseTulipDaisy = pair with { Vase = otherVase },
        }), StringComparison.Ordinal);
    }

    [Fact]
    public void PairIsMutuallyExclusiveWithEveryOtherContentsRelationshipAndExtraEntries()
    {
        var scene = Scene(Combinations[0].Prompt);
        var pair = scene.VaseTulipDaisy!;
        var standaloneDaisy = VaseDaisy.Create(pair.Vase, DaisyAppearance.WhiteWithGoldenDisk, scene.Seed, scene.Variation);
        Assert.Contains("two flower arrangements", SceneValidator.Validate(scene with { VaseDaisy = standaloneDaisy }), StringComparison.Ordinal);

        var bowl = Scene("a red apple in a blue ceramic bowl on a wooden table").Containment!;
        Assert.Contains("another contents relationship", SceneValidator.Validate(scene with { Containment = bowl }), StringComparison.Ordinal);
        var cup = Scene("a blue ceramic cup filled with coffee on a wooden table").CupContents!;
        Assert.Contains("another contents relationship", SceneValidator.Validate(scene with { CupContents = cup }), StringComparison.Ordinal);

        var apple = Scene("a red apple on a wooden table").Objects[0];
        Assert.Contains("exactly its shared vase", SceneValidator.Validate(scene with
        {
            Entries = ImmutableArray.Create(scene.Objects[0], apple),
        }), StringComparison.Ordinal);

        var cupVase = pair.Vase with { Kind = CeramicKind.Cup };
        Assert.Contains("must reference one vase", SceneValidator.Validate(scene with
        {
            VaseTulipDaisy = pair with { Vase = cupVase },
        }), StringComparison.Ordinal);
    }

    [Fact]
    public void PairValidationRejectsMissingLeavesInvalidPetalsAndNonFiniteGeometry()
    {
        var scene = Scene(Combinations[0].Prompt);
        var pair = scene.VaseTulipDaisy!;
        Assert.Contains("two leaves", SceneValidator.Validate(scene with
        {
            VaseTulipDaisy = pair with { Tulip = pair.Tulip with { Leaves = ImmutableArray<TulipLeaf>.Empty } },
        }), StringComparison.Ordinal);
        Assert.Contains("twelve petals", SceneValidator.Validate(scene with
        {
            VaseTulipDaisy = pair with
            {
                Daisy = pair.Daisy with
                {
                    Bloom = pair.Daisy.Bloom with { Petals = ImmutableArray.CreateRange(pair.Daisy.Bloom.Petals.Take(11)) },
                },
            },
        }), StringComparison.Ordinal);
        Assert.Contains("finite and positive", SceneValidator.Validate(scene with
        {
            VaseTulipDaisy = pair with { Tulip = pair.Tulip with { StemThickness = float.NaN } },
        }), StringComparison.Ordinal);
        Assert.Contains("finite and positive", SceneValidator.Validate(scene with
        {
            VaseTulipDaisy = pair with
            {
                Daisy = pair.Daisy with
                {
                    Bloom = pair.Daisy.Bloom with
                    {
                        Disk = pair.Daisy.Bloom.Disk with { RadiusX = -0.01f },
                    },
                },
            },
        }), StringComparison.Ordinal);
    }

    [Fact]
    public void StandaloneCanonicalPlantGeometryRemainsAtItsPhaseEightMeasurements()
    {
        const ulong seed = 42;
        const double variation = 0.35;
        var tulip = Scene("a red tulip in a blue ceramic vase on a wooden table", seed, variation).VaseFlower!;
        AssertVector(tulip.Plant.StemBase, 0.49614555f, 0.23103514f);
        AssertVector(tulip.Plant.StemTop, 0.49111646f, 0.14989832f);
        Assert.InRange(MathF.Abs(tulip.Plant.Bloom.Width - 0.07686101f), 0, 0.000001f);
        Assert.InRange(MathF.Abs(tulip.Plant.Bloom.Height - 0.083544575f), 0, 0.000001f);
        AssertBounds(tulip.Plant.Bounds, 0.43839845f, 0.06514903f, 0.5414572f, 0.234036f);
        Assert.Equal("red-tulip-in-blue-vase", tulip.SubjectToken);

        var daisy = Scene("a white daisy in a blue ceramic vase on a wooden table", seed, variation).VaseDaisy!;
        AssertVector(daisy.Plant.StemBase, 0.49614555f, 0.23103514f);
        AssertVector(daisy.Plant.StemTop, 0.49912068f, 0.09254573f);
        AssertVector(daisy.Plant.Bloom.Center, 0.49912068f, 0.08753747f);
        Assert.InRange(MathF.Abs(daisy.Plant.Bloom.RadiusX - 0.050082657f), 0, 0.000001f);
        Assert.InRange(MathF.Abs(daisy.Plant.Bloom.RadiusY - 0.043071087f), 0, 0.000001f);
        Assert.InRange(MathF.Abs(daisy.Plant.Bloom.Disk.RadiusX - 0.016854826f), 0, 0.000001f);
        Assert.InRange(MathF.Abs(daisy.Plant.Bloom.Disk.RadiusY - 0.014495151f), 0, 0.000001f);
        AssertBounds(daisy.Plant.Bounds, 0.42293796f, 0.03393799f, 0.5618665f, 0.23307572f);
        Assert.Equal("white-daisy-in-blue-vase", daisy.SubjectToken);
    }

    private static StillLifeScene Scene(string prompt, ulong seed = 42, double variation = 0.35)
    {
        var result = SceneGenerator.Generate(new(prompt, Seed: seed, Variation: variation));
        Assert.True(result.IsSuccess, result.Error);
        return result.Scene!;
    }

    private static void AssertVector(Vector2 actual, float x, float y)
    {
        Assert.InRange(MathF.Abs(actual.X - x), 0, 0.000001f);
        Assert.InRange(MathF.Abs(actual.Y - y), 0, 0.000001f);
    }

    private static void AssertBounds(SceneBounds actual, float left, float top, float right, float bottom)
    {
        Assert.InRange(MathF.Abs(actual.Left - left), 0, 0.000002f);
        Assert.InRange(MathF.Abs(actual.Top - top), 0, 0.000002f);
        Assert.InRange(MathF.Abs(actual.Right - right), 0, 0.000002f);
        Assert.InRange(MathF.Abs(actual.Bottom - bottom), 0, 0.000002f);
    }

    private static void AssertStemFitsOpening(Vector2 stemBase, float thickness, Vector2 opening, Vector2 radius)
    {
        var x = (MathF.Abs(stemBase.X - opening.X) + (thickness / 2)) / radius.X;
        var y = MathF.Abs(stemBase.Y - opening.Y) / radius.Y;
        Assert.True((x * x) + (y * y) < 0.95f, "the complete stem base must fit inside the rim-inset opening");
    }

    private static ProfileExtent TulipBloomBounds(TulipPlant plant)
    {
        var bounds = CurveBuilder.Extent(plant.Bloom.Petals[0].Outline);
        foreach (var petal in plant.Bloom.Petals.Skip(1))
        {
            bounds = Union(bounds, CurveBuilder.Extent(petal.Outline));
        }

        return bounds;
    }

    private static ProfileExtent DaisyBloomBounds(DaisyPlant plant)
    {
        var bounds = CurveBuilder.Extent(plant.Bloom.Petals[0].Outline);
        foreach (var petal in plant.Bloom.Petals.Skip(1))
        {
            bounds = Union(bounds, CurveBuilder.Extent(petal.Outline));
        }

        var disk = plant.Bloom.Disk;
        return Union(bounds, new ProfileExtent(
            disk.Center.X - disk.RadiusX,
            disk.Center.Y - disk.RadiusY,
            disk.Center.X + disk.RadiusX,
            disk.Center.Y + disk.RadiusY));
    }

    private static ProfileExtent Union(ProfileExtent first, ProfileExtent second) => new(
        MathF.Min(first.Left, second.Left),
        MathF.Min(first.Top, second.Top),
        MathF.Max(first.Right, second.Right),
        MathF.Max(first.Bottom, second.Bottom));
}
