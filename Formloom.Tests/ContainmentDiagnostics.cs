using Formloom.Core;
using Xunit;

namespace Formloom.Tests;

// TEMPORARY diagnostics: runs the containment test bodies directly and reports outcomes as
// GitHub Actions annotations. Removed before merge.
public sealed class ContainmentDiagnostics
{
    private static readonly string[] FruitDescriptors =
    [
        "a red apple", "a green apple", "a yellow apple", "a green pear", "a yellow pear", "an orange",
    ];

    private static readonly string[] Glazes = ["blue", "cream"];

    [Fact]
    public void Report()
    {
        var core = new BowlContainmentTests();
        var render = new BowlContainmentRenderingTests();
        var checks = new List<(string Name, Action Body)>();

        foreach (var prompt in BowlContainmentTests.Rejected)
        {
            checks.Add(($"reject[{prompt}]", () => core.UnsupportedContainmentPhrasingsAreRejected(prompt)));
        }

        foreach (var fruit in FruitDescriptors)
        {
            foreach (var glaze in Glazes)
            {
                var prompt = $"{fruit} in a {glaze} ceramic bowl on a wooden table";
                checks.Add(($"combo[{prompt}]", () => core.EveryFruitAndGlazeCombinationParsesAndGenerates(prompt, glaze)));
                checks.Add(($"notList[{prompt}]", () => core.ContainmentPromptsAreNotObjectLists(prompt, glaze)));
                checks.Add(($"render[{prompt}]", () => render.EveryCombinationRendersDeterministicallyAtFullSize(prompt, glaze)));
            }
        }

        foreach (var (seed, variation) in new[] { (20261105UL, 0.0), (20261105UL, 1.0), (7UL, 0.35) })
        {
            checks.Add(($"seated[{seed}/{variation}]", () => core.ContentIsSeatedInsideTheBowl(seed, variation)));
        }

        foreach (var (width, height) in new[] { (256, 256), (640, 480), (2048, 1024) })
        {
            checks.Add(($"dims[{width}x{height}]", () => render.ContainmentRendersAtOtherSupportedDimensions(width, height)));
        }

        foreach (var (prompt, name, seed, variation) in new[]
        {
            ("a red apple in a blue ceramic bowl on a wooden table", "apple-blue", 42, 0.35),
            ("a green pear in a cream ceramic bowl on a wooden table", "pear-cream", 42, 0.35),
            ("an orange in a cream ceramic bowl on a wooden table", "orange-cream", 42, 1),
        })
        {
            checks.Add(($"export[{name}]", () => render.ExportContainmentScenesTimingsAndSilhouettes(prompt, name, seed, variation)));
        }

        checks.Add(("nulls", core.ContainmentIsNullForPromptsThatDoNotContain));
        checks.Add(("fit", core.EachFruitTypeGetsItsOwnFit));
        checks.Add(("bounded", core.PlacementIsBoundedAcrossSeeds));
        checks.Add(("deterministic", core.PlacementIsDeterministic));
        checks.Add(("glazeSeed", core.ContentSeedAndShapeIgnoreTheGlaze));
        checks.Add(("geometry", core.GeometrySplitsTheBowlAtTheRim));
        checks.Add(("validator", core.ValidatorRejectsBrokenContainment));
        checks.Add(("species", core.ContentKeepsItsSpeciesSilhouetteAndNeverStretches));
        checks.Add(("altSeeds", core.AlternateSeedsMoveTheContentWithoutChangingIdentity));
        checks.Add(("occlusion", render.NearWallOccludesTheSeatedPartOfTheContent));
        checks.Add(("leaks", render.ContainmentChangesNothingOutsideTheContentAndTheCavity));
        checks.Add(("shadow", render.ContainedFruitCastsNoTabletopShadow));
        checks.Add(("cavity", render.ContactShadowIsClippedToTheCavityAndStaysSubtle));
        checks.Add(("silhouette", render.SilhouetteShowsTheContentAboveTheRim));
        checks.Add(("detail", render.OmittedContentDetailKeepsTheLayering));
        checks.Add(("diagPasses", render.DiagnosticPassesDoNotChangeSubsequentContainmentRendering));
        checks.Add(("sheet", render.ExportContainmentReviewSheetAndManifest));

        var passed = 0;
        var failures = new List<string>();
        var index = 0;
        foreach (var (name, body) in checks)
        {
            index++;
            if (index % 25 == 0)
            {
                Emit($"core-progress {index}/{checks.Count} failures={failures.Count} last={name}");
            }

            try
            {
                body();
                passed++;
            }
            catch (Exception exception)
            {
                failures.Add($"{name}: {exception.GetType().Name}: {Short(exception.Message)}");
            }
        }

        Emit($"formloom-tests: passed={passed} failed={failures.Count} -> {string.Join("  ##  ", failures)}");
    }

    private static string Short(string value)
    {
        var single = value.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " | ", StringComparison.Ordinal);
        return single.Length <= 260 ? single : single[..260];
    }

    private static void Emit(string line)
    {
        using var stdout = Console.OpenStandardOutput();
        using var writer = new StreamWriter(stdout) { AutoFlush = true };
        writer.WriteLine("::error::" + line);
    }
}
