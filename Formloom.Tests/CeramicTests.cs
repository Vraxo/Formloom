using System.Collections.Immutable;
using System.Numerics;
using Formloom.Core;
using Xunit;

namespace Formloom.Tests;

public sealed class CeramicTests
{
    [Theory]
    [InlineData("a blue ceramic bowl on a wooden table", CeramicKind.Bowl, CeramicGlaze.Blue)]
    [InlineData("a cream ceramic bowl on a wooden table", CeramicKind.Bowl, CeramicGlaze.Cream)]
    [InlineData("a blue ceramic cup on a wooden table", CeramicKind.Cup, CeramicGlaze.Blue)]
    [InlineData("a cream ceramic cup on a wooden table", CeramicKind.Cup, CeramicGlaze.Cream)]
    [InlineData("a blue ceramic vase on a wooden table", CeramicKind.Vase, CeramicGlaze.Blue)]
    [InlineData("a cream ceramic vase on a wooden table", CeramicKind.Vase, CeramicGlaze.Cream)]
    public void CeramicDescriptorsMapToKindAndGlaze(string prompt, CeramicKind kind, CeramicGlaze glaze)
    {
        var parsed = PromptParser.ParseObjects(prompt);
        Assert.NotNull(parsed);
        var item = Assert.Single(parsed.Value);
        Assert.Null(item.Fruit);
        Assert.Equal(kind, item.Ceramic!.Kind);
        Assert.Equal(glaze, item.Ceramic.Glaze);
        Assert.Null(PromptParser.ParseScene(prompt));
        Assert.Null(PromptParser.ParseFruit(prompt));
        Assert.Null(PromptParser.Parse(prompt));

        var result = SceneGenerator.Generate(new(prompt));
        Assert.True(result.IsSuccess, result.Error);
        var scene = result.Scene!;
        var ceramic = Assert.Single(scene.Ceramics);
        Assert.Equal(kind, ceramic.Kind);
        Assert.Equal(glaze, ceramic.Glaze);
        Assert.Empty(scene.Fruits);
        Assert.Null(SceneValidator.Validate(scene));
        Assert.Equal("ceramic-" + kind.ToString().ToLowerInvariant(), scene.SubjectToken);
    }

    [Theory]
    [InlineData("a red apple and a blue ceramic bowl on a wooden table", "Apple.Red,Bowl.Blue")]
    [InlineData("a green pear and a cream ceramic cup on a wooden table", "Pear.Green,Cup.Cream")]
    [InlineData("an orange and a blue ceramic vase on a wooden table", "Orange.Orange,Vase.Blue")]
    [InlineData("a red apple, a blue ceramic bowl, and an orange on a wooden table", "Apple.Red,Bowl.Blue,Orange.Orange")]
    [InlineData("a cream ceramic cup, a green pear, and a blue ceramic vase on a wooden table", "Cup.Cream,Pear.Green,Vase.Blue")]
    [InlineData(" A BLUE CERAMIC VASE , a red apple and A CREAM CERAMIC BOWL on a wooden table! ", "Vase.Blue,Apple.Red,Bowl.Cream")]
    [InlineData("a blue ceramic cup and a blue ceramic cup on a wooden table", "Cup.Blue,Cup.Blue")]
    public void MixedListsPreservePromptOrderAndIdentity(string prompt, string keys)
    {
        var parsed = PromptParser.ParseObjects(prompt);
        Assert.NotNull(parsed);
        var result = SceneGenerator.Generate(new(prompt));
        Assert.True(result.IsSuccess, result.Error);
        var scene = result.Scene!;
        Assert.Equal(keys, string.Join(",", parsed.Value.Select(item => item.Key)));
        Assert.Equal(keys, string.Join(",", scene.Objects.Select(entry => entry.DescriptorKey)));
        Assert.Equal(scene.Objects.Length, scene.Fruits.Length + scene.Ceramics.Length);
        Assert.Null(SceneValidator.Validate(scene));
    }

    [Theory]
    [InlineData("")]
    [InlineData("a blue bowl on a wooden table")]
    [InlineData("a ceramic bowl on a wooden table")]
    [InlineData("a red ceramic bowl on a wooden table")]
    [InlineData("a blue ceramic plate on a wooden table")]
    [InlineData("a blue ceramic mug on a wooden table")]
    [InlineData("a large blue ceramic bowl on a wooden table")]
    [InlineData("a shiny cream ceramic cup on a wooden table")]
    [InlineData("a blue ceramic bowl in a cream ceramic cup on a wooden table")]
    [InlineData("a blue ceramic bowl on top of a red apple on a wooden table")]
    [InlineData("a blue ceramic vase beside an orange on a wooden table")]
    [InlineData("two blue ceramic bowls on a wooden table")]
    [InlineData("a blue ceramic bowl and a red apple and an orange and a green pear on a wooden table")]
    [InlineData("a blue ceramic bowl, an orange on a wooden table")]
    [InlineData("a blue ceramic bowl or an orange on a wooden table")]
    [InlineData("a blue ceramic bowl and and an orange on a wooden table")]
    [InlineData("a blue ceramic bowl on a wooden shelf")]
    public void UnsupportedCeramicPhrasesAreRejectedWhole(string prompt)
    {
        Assert.Null(PromptParser.ParseObjects(prompt));
        Assert.Null(PromptParser.ParseScene(prompt));
        var result = SceneGenerator.Generate(new(prompt));
        Assert.False(result.IsSuccess);
        Assert.Null(result.Scene);
        Assert.Contains("not supported", result.Error!);
    }

    [Fact]
    public void VesselProfilesAreFiniteClosedAndBounded()
    {
        foreach (var kind in CeramicForms.Kinds)
        {
            var profile = CeramicForms.For(kind);
            Assert.Equal(kind, profile.Kind);
            Assert.NotEmpty(profile.Outline);
            for (var index = 0; index < profile.Outline.Length; index++)
            {
                var segment = profile.Outline[index];
                foreach (var point in new[] { segment.Start, segment.Control1, segment.Control2, segment.End })
                {
                    Assert.True(float.IsFinite(point.X) && float.IsFinite(point.Y));
                    Assert.InRange(point.X, -0.15f, 1.15f);
                    Assert.InRange(point.Y, -0.15f, 1.15f);
                }

                Assert.Equal(segment.End, profile.Outline[(index + 1) % profile.Outline.Length].Start);
            }

            var extent = CurveBuilder.Extent(profile.Outline);
            Assert.InRange(extent.Left, 0f, 0.25f);
            Assert.InRange(extent.Right, 0.75f, 1f);
            Assert.InRange(extent.Top, 0f, 0.15f);
            Assert.InRange(extent.Bottom, 0.75f, 0.93f);
            Assert.True(profile.CavityDepth > 0);
            Assert.True(profile.MouthRadius.X > 0.1f);
            Assert.True(profile.BaseWidth is > 0.15f and < 0.6f);
            Assert.Equal(profile.Outline, CeramicForms.For(kind).Outline);
        }
    }

    [Fact]
    public void ProfilesDifferThroughGeometricInvariants()
    {
        var bowl = CeramicForms.For(CeramicKind.Bowl);
        var cup = CeramicForms.For(CeramicKind.Cup);
        var vase = CeramicForms.For(CeramicKind.Vase);

        Assert.True(bowl.MouthWidth / bowl.Height > 1f);
        Assert.True(cup.MouthWidth / cup.Height < 0.9f);
        Assert.True(cup.MouthWidth / cup.Height > 0.6f);
        Assert.True(vase.Width / vase.MouthWidth > 2f);
        Assert.True(bowl.Width > cup.Width);

        Assert.Null(bowl.Handle);
        Assert.Null(vase.Handle);
        var handle = Assert.IsType<CeramicHandle>(cup.Handle);
        Assert.True(handle.InnerRadius.X < handle.OuterRadius.X);
        Assert.True(handle.InnerRadius.Y < handle.OuterRadius.Y);
        Assert.True(handle.Center.X + handle.OuterRadius.X > cup.OutlineRight);
        Assert.True(handle.TopAttachment.Y < handle.Center.Y);
        Assert.True(handle.BottomAttachment.Y > handle.Center.Y);
    }

    [Fact]
    public void CeramicBoundsIncludeTheHandleAndReachTheContactLine()
    {
        var scene = SceneGenerator.Generate(new("a blue ceramic cup on a wooden table")).Scene!;
        var cup = Assert.Single(scene.Ceramics);
        var profile = CeramicForms.For(CeramicKind.Cup);
        var bounds = CeramicBounds.For(cup);
        var bodyRight = cup.Body.Center.X + ((profile.OutlineRight - 0.5f) * cup.Body.Size.X);
        var contact = cup.Body.Center.Y + ((profile.BaseBottom - 0.5f) * cup.Body.Size.Y);
        Assert.True(bounds.Right > bodyRight + (cup.Body.Size.X * 0.1f));
        Assert.Equal(contact, bounds.Bottom, 3);
        Assert.InRange(bounds.Left, 0f, 1f);
        Assert.InRange(bounds.Top, 0f, 1f);
        Assert.InRange(bounds.Right, 0f, 1f);
    }

    [Fact]
    public void SingleVesselsAreCentredGroundedAndRepeatable()
    {
        string[] prompts =
        [
            "a blue ceramic bowl on a wooden table",
            "a cream ceramic cup on a wooden table",
            "a blue ceramic vase on a wooden table",
        ];
        foreach (var prompt in prompts)
        {
            foreach (var variation in new[] { 0, 0.35, 1 })
            {
                var request = new GenerationRequest(prompt, Variation: variation);
                var result = SceneGenerator.Generate(request);
                Assert.True(result.IsSuccess, result.Error);
                var scene = result.Scene!;
                var entry = Assert.Single(scene.Objects);
                Assert.True(entry.IsCeramic);
                var bounds = entry.Bounds;
                Assert.InRange((bounds.Left + bounds.Right) / 2, 0.45f, 0.55f);
                Assert.InRange(bounds.Bottom, 0.74f, 0.80f);
                Assert.InRange(bounds.Top, 0.03f, 0.60f);
                Assert.Equal(scene.Objects.ToArray(), SceneGenerator.Generate(request).Scene!.Objects.ToArray());
            }
        }
    }

    [Theory]
    [InlineData("a red apple and a blue ceramic bowl on a wooden table")]
    [InlineData("a green pear and a cream ceramic cup on a wooden table")]
    [InlineData("an orange and a blue ceramic vase on a wooden table")]
    [InlineData("a blue ceramic bowl and a cream ceramic cup on a wooden table")]
    [InlineData("a red apple, a blue ceramic bowl, and an orange on a wooden table")]
    [InlineData("a cream ceramic cup, a green pear, and a blue ceramic vase on a wooden table")]
    [InlineData("a blue ceramic vase, a yellow pear, and a cream ceramic bowl on a wooden table")]
    public void MixedLayoutIsBoundedGroundedAndDeterministic(string prompt)
    {
        var expected = PromptParser.ParseObjects(prompt)!.Value.Length;
        for (ulong seed = 0; seed < 40; seed++)
        {
            foreach (var variation in new[] { 0, 0.35, 1 })
            {
                var request = new GenerationRequest(prompt, seed, variation);
                var result = SceneGenerator.Generate(request);
                Assert.True(result.IsSuccess, $"{prompt} seed {seed} variation {variation}: {result.Error}");
                var scene = result.Scene!;
                Assert.Null(SceneValidator.Validate(scene));
                Assert.Equal(expected, scene.Objects.Length);
                Assert.Equal(scene.Objects.ToArray(), SceneGenerator.Generate(request).Scene!.Objects.ToArray());
                foreach (var entry in scene.Objects)
                {
                    var bounds = entry.Bounds;
                    Assert.InRange(bounds.Left, 0.025f, 0.975f);
                    Assert.InRange(bounds.Right, 0.025f, 0.975f);
                    Assert.InRange(bounds.Top, 0.0f, 0.94f);
                    Assert.InRange(bounds.Bottom, 0.70f, 0.94f);
                    var contact = entry.Center.Y + (entry.GroundOffset * entry.Size.Y);
                    Assert.InRange(contact, 0.72f, 0.80f);
                }

                foreach (var entry in scene.Objects)
                {
                    var separation = scene.Objects
                        .Where(other => other != entry)
                        .Min(other => Math.Abs(other.Center.X - entry.Center.X));
                    Assert.True(separation > 0.08f);
                }
            }
        }
    }

    [Fact]
    public void DifferentSeedsRearrangeMixedScenesWithoutChangingObjects()
    {
        const string prompt = "a red apple, a cream ceramic bowl, and a blue ceramic vase on a wooden table";
        var scenes = Enumerable.Range(0, 16).Select(seed => SceneGenerator.Generate(new(prompt, (ulong)seed)).Scene!).ToArray();
        Assert.True(scenes.Select(scene => string.Join(",", scene.Objects.Select(entry => entry.Depth))).Distinct().Count() > 1);
        foreach (var scene in scenes)
        {
            Assert.Equal(
                new[] { "Apple.Red", "Bowl.Cream", "Vase.Blue" },
                scene.Objects.Select(entry => entry.DescriptorKey).ToArray());
        }
    }

    [Fact]
    public void AddingADifferentObjectKeepsExistingLocalStreams()
    {
        var single = SceneGenerator.Generate(new("a red apple on a wooden table")).Scene!;
        var withBowl = SceneGenerator.Generate(new("a red apple and a blue ceramic bowl on a wooden table")).Scene!;
        var withCup = SceneGenerator.Generate(new("a red apple and a cream ceramic cup on a wooden table")).Scene!;
        var reversed = SceneGenerator.Generate(new("a cream ceramic cup and a red apple on a wooden table")).Scene!;

        var apple = single.Fruits[0];
        Assert.Equal(apple.Seed, withBowl.Fruits[0].Seed);
        Assert.Equal(apple.Seed, withCup.Fruits[0].Seed);
        Assert.Equal(apple.Seed, reversed.Fruits[0].Seed);
        Assert.Equal(apple.Body.Asymmetry, withBowl.Fruits[0].Body.Asymmetry);
        Assert.Equal(apple.Body.StemLean, reversed.Fruits[0].Body.StemLean);
        Assert.Equal(
            OrganicForms.Create(apple.Subject, apple.Body.Asymmetry, apple.Seed, single.Variation).Segments.ToArray(),
            OrganicForms.Create(reversed.Fruits[0].Subject, reversed.Fruits[0].Body.Asymmetry, reversed.Fruits[0].Seed, reversed.Variation).Segments.ToArray());

        var cupSingle = SceneGenerator.Generate(new("a cream ceramic cup on a wooden table")).Scene!;
        var cupAlone = cupSingle.Ceramics[0];
        var cupMixed = withCup.Ceramics[0];
        Assert.Equal(cupAlone.Seed, cupMixed.Seed);
        Assert.Equal(cupAlone.Body.SurfaceVariation, cupMixed.Body.SurfaceVariation);
        Assert.Equal(cupAlone.Body.Size, cupMixed.Body.Size);
        Assert.Equal(cupMixed.Seed, reversed.Ceramics[0].Seed);

        var duplicates = SceneGenerator.Generate(new("a blue ceramic bowl and a blue ceramic bowl on a wooden table")).Scene!;
        Assert.NotEqual(duplicates.Ceramics[0].Seed, duplicates.Ceramics[1].Seed);
    }

    [Fact]
    public void FruitOnlyScenesKeepTheEstablishedCompatibilityPath()
    {
        var single = SceneGenerator.Generate(new("a red apple on a wooden table")).Scene!;
        Assert.Null(single.Instances);
        Assert.Null(single.Entries);
        Assert.Empty(single.Ceramics);
        Assert.Equal(FruitKind.Apple, single.Fruits[0].Subject);

        var pair = SceneGenerator.Generate(new("a red apple and a green pear on a wooden table")).Scene!;
        Assert.NotNull(pair.Instances);
        Assert.Null(pair.Entries);
        Assert.Equal("fruit-group", pair.SubjectToken);

        var mixed = SceneGenerator.Generate(new("a red apple and a blue ceramic bowl on a wooden table")).Scene!;
        Assert.Null(mixed.Instances);
        Assert.NotNull(mixed.Entries);
        Assert.Equal("still-life-group", mixed.SubjectToken);
        Assert.Single(mixed.Fruits);
        Assert.Single(mixed.Ceramics);

        var ceramicsOnly = SceneGenerator.Generate(new("a blue ceramic bowl and a cream ceramic cup on a wooden table")).Scene!;
        Assert.Equal("ceramic-group", ceramicsOnly.SubjectToken);
    }

    [Fact]
    public void SceneValidationRejectsInvalidCeramicData()
    {
        var scene = SceneGenerator.Generate(new("a cream ceramic vase on a wooden table")).Scene!;
        var vase = Assert.Single(scene.Ceramics);
        Assert.Null(SceneValidator.Validate(scene));
        Assert.NotNull(SceneValidator.Validate(scene with { Entries = ImmutableArray<SceneEntry>.Empty }));
        Assert.NotNull(SceneValidator.Validate(scene with { Entries = default(ImmutableArray<SceneEntry>) }));
        var entry = SceneEntry.ForCeramic(vase);
        Assert.NotNull(SceneValidator.Validate(scene with { Entries = ImmutableArray.Create(entry, entry, entry, entry) }));
        Assert.NotNull(SceneValidator.Validate(scene with { Entries = ImmutableArray.Create(SceneEntry.ForCeramic(vase with { Kind = (CeramicKind)9 })) }));
        Assert.NotNull(SceneValidator.Validate(scene with { Entries = ImmutableArray.Create(SceneEntry.ForCeramic(vase with { Glaze = (CeramicGlaze)7 })) }));
        Assert.NotNull(SceneValidator.Validate(scene with { Entries = ImmutableArray.Create(SceneEntry.ForCeramic(vase with { Depth = 9 })) }));

        Vector2[] invalidCenters = [new(float.NaN, 0.5f), new(0.5f, float.PositiveInfinity), new(-1, 0.5f), new(1, 0.5f)];
        foreach (var center in invalidCenters)
        {
            Assert.NotNull(SceneValidator.Validate(scene with
            {
                Entries = ImmutableArray.Create(SceneEntry.ForCeramic(vase with { Body = vase.Body with { Center = center } })),
            }));
        }

        Vector2[] invalidSizes = [new(0, 0.4f), new(-1, 0.4f), new(0.9f, 0.9f), new(float.NaN, 0.4f)];
        foreach (var size in invalidSizes)
        {
            Assert.NotNull(SceneValidator.Validate(scene with
            {
                Entries = ImmutableArray.Create(SceneEntry.ForCeramic(vase with { Body = vase.Body with { Size = size } })),
            }));
        }

        var fruit = SceneGenerator.Generate(new("a red apple on a wooden table")).Scene!.Fruits[0];
        Assert.NotNull(SceneValidator.Validate(scene with
        {
            Entries = ImmutableArray.Create(SceneEntry.ForCeramic(vase), SceneEntry.ForFruit(fruit), entry, SceneEntry.ForFruit(fruit)),
        }));
    }
}
