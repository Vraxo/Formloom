using System.Collections.Immutable;
using System.Numerics;
using Formloom.Core.Ceramics;
using Formloom.Core.CupLiquids;
using Formloom.Core.Scenes;
using Xunit;

namespace Formloom.Tests.CupLiquids;

public sealed class CupLiquidTests
{
    private static readonly string[] Glazes = ["blue", "cream"];

    private static readonly string[] Beverages = ["coffee", "tea"];

    // Every filled-cup phrasing the grammar must reject. Shared with the diagnostics harness
    // convention used in earlier milestones: each row is refused whole, never simplified.
    public static readonly string[] Rejected =
    [
        "a red apple in a blue ceramic cup on a wooden table",
        "a blue ceramic bowl filled with coffee on a wooden table",
        "a cream ceramic vase filled with tea on a wooden table",
        "a blue ceramic cup with coffee on a wooden table",
        "a blue ceramic cup containing coffee on a wooden table",
        "a blue ceramic cup holding coffee on a wooden table",
        "a blue ceramic cup of coffee on a wooden table",
        "coffee in a blue ceramic cup on a wooden table",
        "a blue ceramic cup filled with coffee",
        "a blue ceramic cup filled with coffee on a table",
        "a ceramic cup filled with coffee on a wooden table",
        "a red ceramic cup filled with coffee on a wooden table",
        "a blue ceramic cup filled with milk on a wooden table",
        "a blue ceramic cup filled with coffee and tea on a wooden table",
        "a blue ceramic cup and a cream ceramic cup filled with coffee on a wooden table",
        "two blue ceramic cups filled with coffee on a wooden table",
        "a blue ceramic cup filled with coffees on a wooden table",
        "a large blue ceramic cup filled with coffee on a wooden table",
        "a blue ceramic cup filled with hot coffee on a wooden table",
        "a blue ceramic cup filled with coffee on a wooden table and a red apple",
        "a blue ceramic cup filled with coffee, on a wooden table",
        "a blue ceramic cup filled with coffee on a wooden table, a red apple",
        "a cream ceramic cup filled with coffee on a wooden table and a green pear",
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

    public static TheoryData<string, string, string> Combinations()
    {
        var data = new TheoryData<string, string, string>();
        foreach (var glaze in Glazes)
        {
            foreach (var beverage in Beverages)
            {
                data.Add($"a {glaze} ceramic cup filled with {beverage} on a wooden table", glaze, beverage);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Combinations))]
    public void EveryCupAndBeverageCombinationParsesAndGenerates(string prompt, string glaze, string beverage)
    {
        var parsed = PromptParser.ParseCupContents(prompt);
        Assert.NotNull(parsed);
        Assert.Equal(CeramicKind.Cup, parsed.Cup.Kind);
        Assert.Equal(glaze == "blue" ? CeramicGlaze.Blue : CeramicGlaze.Cream, parsed.Cup.Glaze);
        Assert.Equal(beverage == "coffee" ? BeverageKind.Coffee : BeverageKind.Tea, parsed.Beverage);

        var result = SceneGenerator.Generate(new(prompt, Seed: 42, Variation: 0.35));
        Assert.Null(result.Error);
        var scene = result.Scene!;
        Assert.NotNull(scene.CupContents);
        Assert.Null(scene.Containment);
        Assert.Single(scene.Objects);
        Assert.Equal(CeramicKind.Cup, scene.Ceramics[0].Kind);

        // The contents refer to the exact cup the scene holds, and the cup itself is the same
        // cup an empty-cup prompt of the same seed builds.
        Assert.Equal(scene.Ceramics[0], scene.CupContents!.Cup);
        Assert.Equal(parsed.Beverage, scene.CupContents.Beverage);
        var empty = SceneGenerator.Generate(new($"a {glaze} ceramic cup on a wooden table", Seed: 42, Variation: 0.35)).Scene!;
        Assert.Equal(empty.Ceramics[0].Body, scene.Ceramics[0].Body);
        Assert.Equal(empty.Ceramics[0].Seed, scene.Ceramics[0].Seed);

        Assert.Equal($"{beverage}-in-{glaze}-cup", scene.SubjectToken);
    }

    [Fact]
    public void ParsingToleratesCaseWhitespaceAndTerminalPunctuation()
    {
        foreach (var prompt in new[]
        {
            "A BLUE CERAMIC CUP FILLED WITH COFFEE ON A WOODEN TABLE",
            "  a blue ceramic cup filled with coffee on a wooden table.  ",
            "a   cream   ceramic cup   filled with   tea on a wooden table!",
            "a Blue Ceramic Cup Filled With Tea On A Wooden Table?",
        })
        {
            Assert.NotNull(PromptParser.ParseCupContents(prompt));
        }
    }

    // A filled cup is one object with contents, not a list of two objects the old parser
    // might read as neighbours of each other.
    [Fact]
    public void FilledCupPromptsAreNotObjectLists()
    {
        const string Prompt = "a blue ceramic cup filled with coffee on a wooden table";
        Assert.Null(PromptParser.ParseObjects(Prompt));
        Assert.Null(PromptParser.Parse(Prompt));
        Assert.Null(PromptParser.ParseFruit(Prompt));
        Assert.Null(PromptParser.ParseScene(Prompt));
        Assert.Null(PromptParser.ParseContainment(Prompt));
        Assert.NotNull(PromptParser.ParseCupContents(Prompt));
    }

    [Theory]
    [MemberData(nameof(RejectedCases))]
    public void UnsupportedCupContentsPhrasingsAreRejected(string prompt)
    {
        Assert.Null(PromptParser.ParseCupContents(prompt));

        // Rejected whole: no silent fallback to an ordinary empty cup.
        Assert.False(SceneGenerator.Generate(new(prompt)).IsSuccess);
    }

    [Fact]
    public void OrdinaryCupPromptsStillParseWithoutContents()
    {
        var scene = SceneGenerator.Generate(new("a blue ceramic cup on a wooden table")).Scene!;
        Assert.Null(scene.CupContents);
        Assert.Equal("ceramic-cup", scene.SubjectToken);

        var mixed = SceneGenerator.Generate(new("a cream ceramic cup and an orange on a wooden table")).Scene!;
        Assert.Null(mixed.CupContents);
        Assert.Equal(2, mixed.Objects.Length);
    }

    [Fact]
    public void CupContentsAndContainmentAreMutuallyExclusive()
    {
        var containment = SceneGenerator.Generate(new("a red apple in a blue ceramic bowl on a wooden table")).Scene!;
        var filled = SceneGenerator.Generate(new("a blue ceramic cup filled with coffee on a wooden table")).Scene!;
        var contradictory = containment with
        {
            CupContents = new(filled.CupContents!.Cup, BeverageKind.Tea),
        };

        Assert.Null(SceneValidator.Validate(containment));
        Assert.Null(SceneValidator.Validate(filled));
        Assert.Equal("A scene cannot be both a filled cup and a fruit inside a bowl.", SceneValidator.Validate(contradictory));
    }

    [Fact]
    public void ValidatorRejectsBrokenCupContents()
    {
        var scene = SceneGenerator.Generate(new("a blue ceramic cup filled with coffee on a wooden table")).Scene!;
        var contents = scene.CupContents!;
        var cup = contents.Cup;
        var fruit = SceneGenerator.Generate(new("a red apple on a wooden table")).Scene!.Objects[0];

        // A bowl or vase cannot hold a drink.
        Assert.Equal(
            "Only a cup can hold a beverage.",
            SceneValidator.Validate(scene with { CupContents = new(cup with { Kind = CeramicKind.Vase }, BeverageKind.Coffee) }));

        // The beverage value is closed: two defined values, nothing else.
        Assert.Equal(
            "A cup can only hold coffee or tea.",
            SceneValidator.Validate(scene with { CupContents = new(cup, (BeverageKind)7) }));

        // The scene must hold exactly the cup the contents refer to.
        Assert.Equal(
            "A filled cup scene must contain exactly that cup and no other object.",
            SceneValidator.Validate(scene with
            {
                CupContents = contents,
                Entries = ImmutableArray.Create(SceneEntry.ForCeramic(cup), SceneEntry.ForCeramic(cup with { Seed = cup.Seed + 1 })),
            }));
        Assert.Equal(
            "A filled cup scene must contain exactly that cup and no other object.",
            SceneValidator.Validate(scene with { CupContents = contents, Entries = ImmutableArray.Create(fruit) }));
    }

    [Fact]
    public void SurfaceGeometryIsDerivedFromTheCupOpening()
    {
        var scene = SceneGenerator.Generate(new("a blue ceramic cup filled with coffee on a wooden table")).Scene!;
        var surface = scene.CupContents!.Surface;
        var profile = scene.CupContents.Profile;
        var mouth = profile.MouthCenter;
        var radius = profile.MouthRadius;
        var rim = profile.RimThickness;

        Assert.Equal(mouth.X, surface.Center.X);
        Assert.True(surface.Radius.X > 0 && surface.Radius.Y > 0);
        Assert.True(float.IsFinite(surface.Center.Y) && float.IsFinite(surface.Radius.X) && float.IsFinite(surface.Radius.Y));

        // Flattened like the opening, not a circle and not a rectangle.
        Assert.True(surface.Radius.Y < surface.Radius.X * 0.25f);

        // Sunk below the lip, with a crescent of the far wall above the drink and the near
        // wall still in front of it.
        Assert.True(surface.Center.Y > mouth.Y);
        Assert.True(surface.Top > (mouth.Y - radius.Y) + (rim * 0.5f), "the drink must sit below the far lip");
        Assert.True(surface.Bottom < mouth.Y + radius.Y, "the drink must never reach the near lip");

        // The clip is the inner opening, inset by the rim, so no lip pixel can be covered.
        Assert.True(surface.ClipRadius.X < radius.X - (rim * 0.5f));
        Assert.True(surface.ClipRadius.Y < radius.Y - (rim * 0.5f));
        Assert.Equal(mouth, surface.ClipCenter);

        // Wider than the opening on purpose: the sides are governed by the clip, so the drink
        // meets the inner wall instead of floating inside a ring of ceramic.
        Assert.True(surface.Radius.X > surface.ClipRadius.X);
    }

    [Fact]
    public void SurfaceIgnoresTheGlazeSeedAndVariation()
    {
        var blue = SceneGenerator.Generate(new("a blue ceramic cup filled with coffee on a wooden table", Seed: 42, Variation: 0.35)).Scene!;
        var cream = SceneGenerator.Generate(new("a cream ceramic cup filled with coffee on a wooden table", Seed: 42, Variation: 0.35)).Scene!;
        Assert.Equal(blue.CupContents!.Surface, cream.CupContents!.Surface);

        foreach (var seed in new ulong[] { 42, 1337, 20261105 })
        {
            foreach (var variation in new double[] { 0, 0.35, 1 })
            {
                var scene = SceneGenerator.Generate(new("a cream ceramic cup filled with tea on a wooden table", Seed: seed, Variation: variation)).Scene!;
                Assert.Equal(blue.CupContents.Surface, scene.CupContents!.Surface);
                Assert.Null(SceneValidator.Validate(scene));
            }
        }

        // Deterministic: the same prompt parsed twice yields the same geometry.
        var again = SceneGenerator.Generate(new("a blue ceramic cup filled with coffee on a wooden table", Seed: 42, Variation: 0.35)).Scene!;
        Assert.Equal(blue.CupContents.Surface, again.CupContents!.Surface);
        Assert.Equal(blue.CupContents.Cup.Body, again.CupContents.Cup.Body);
    }

    [Fact]
    public void VisibleSurfaceStaysInsideTheOpeningWithARimMargin()
    {
        var profile = CeramicForms.For(CeramicKind.Cup);
        var surface = CupLiquidSurface.For(profile);
        var mouth = profile.MouthCenter;
        var radius = profile.MouthRadius;
        var rim = profile.RimThickness;

        // The clip is the inner opening: strictly inside the rim on both axes.
        Assert.True(surface.ClipRadius.X <= radius.X - rim, "the clip must clear the rim horizontally");
        Assert.True(surface.ClipRadius.Y <= radius.Y - rim, "the clip must clear the rim vertically");

        // The visible drink never reaches the outer mouth ellipse: its sides are the clip, its
        // top is the surface's own curve, and its bottom is the nearer of the two.
        var visibleTop = surface.Top;
        var visibleBottom = MathF.Min(surface.Bottom, surface.ClipCenter.Y + surface.ClipRadius.Y);
        var visibleLeft = surface.ClipCenter.X - surface.ClipRadius.X;
        var visibleRight = surface.ClipCenter.X + surface.ClipRadius.X;
        Assert.True(Within(visibleLeft + 0.002f, surface.ClipCenter.Y, mouth, radius));
        Assert.True(Within(visibleRight - 0.002f, surface.ClipCenter.Y, mouth, radius));
        Assert.True(Within(mouth.X, visibleTop + 0.002f, mouth, radius));
        Assert.True(Within(mouth.X, visibleBottom - 0.002f, mouth, radius));

        // A visible crescent of inner wall above the drink is what makes it read as recessed.
        Assert.True(visibleTop - (mouth.Y - surface.ClipRadius.Y) > 0.004f, "the far wall crescent must stay visible");

        // And the near wall is in front of it, so the surface bottom passes the clip bottom.
        Assert.True(surface.Bottom > surface.ClipCenter.Y + surface.ClipRadius.Y, "the drink must tuck behind the near wall");
    }

    private static bool Within(float x, float y, Vector2 center, Vector2 radius)
    {
        var dx = (x - center.X) / radius.X;
        var dy = (y - center.Y) / radius.Y;
        return (dx * dx) + (dy * dy) <= 1f;
    }
}
