using Formloom.Core;
using Formloom.Rendering.Skia;
using Xunit;

namespace Formloom.Tests;

// TEMPORARY diagnostics: runs each containment test body directly and reports which ones
// throw, as a GitHub Actions annotation. Removed before merge.
public sealed class ContainmentDiagnostics
{
    [Fact]
    public void ReportFailingTests()
    {
        var core = new BowlContainmentTests();
        var render = new BowlContainmentRenderingTests();
        var checks = new List<(string Name, Action Body)>
        {
            ("combination", () => core.EveryFruitAndGlazeCombinationParsesAndGenerates("a red apple in a blue ceramic bowl on a wooden table", "blue")),
            ("combination2", () => core.EveryFruitAndGlazeCombinationParsesAndGenerates("a yellow pear in a cream ceramic bowl on a wooden table", "cream")),
            ("notList", () => core.ContainmentPromptsAreNotObjectLists("an orange in a cream ceramic bowl on a wooden table", "cream")),
            ("reject", () => core.UnsupportedContainmentPhrasingsAreRejected("a red apple in a blue ceramic cup on a wooden table")),
            ("reject2", () => core.UnsupportedContainmentPhrasingsAreRejected("a blue ceramic bowl with a red apple on a wooden table")),
            ("reject3", () => core.UnsupportedContainmentPhrasingsAreRejected("a red apple on a wooden table")),
            ("reject4", () => core.UnsupportedContainmentPhrasingsAreRejected("a red apple in a blue ceramic bowl and a cream ceramic bowl on a wooden table")),
            ("reject5", () => core.UnsupportedContainmentPhrasingsAreRejected(string.Empty)),
            ("nulls", core.ContainmentIsNullForPromptsThatDoNotContain),
            ("seated0", () => core.ContentIsSeatedInsideTheBowl(20261105UL, 0.0)),
            ("seated1", () => core.ContentIsSeatedInsideTheBowl(7UL, 0.35)),
            ("fit", core.EachFruitTypeGetsItsOwnFit),
            ("bounded", core.PlacementIsBoundedAcrossSeeds),
            ("deterministic", core.PlacementIsDeterministic),
            ("glazeSeed", core.ContentSeedAndShapeIgnoreTheGlaze),
            ("geometry", core.GeometrySplitsTheBowlAtTheRim),
            ("validator", core.ValidatorRejectsBrokenContainment),
            ("normalize", () => core.CaseWhitespaceAndPunctuationNormalizationIsPreserved("  A  red   apple  in a  blue ceramic bowl  on a wooden table. ")),
            ("species", core.ContentKeepsItsSpeciesSilhouetteAndNeverStretches),
            ("altSeeds", core.AlternateSeedsMoveTheContentWithoutChangingIdentity),
            ("render123", () => render.EveryCombinationRendersDeterministicallyAtFullSize("an orange in a blue ceramic bowl on a wooden table", "blue")),
            ("occlusion", render.NearWallOccludesTheSeatedPartOfTheContent),
            ("leaks", render.ContainmentChangesNothingOutsideTheContentAndTheCavity),
            ("shadow", render.ContainedFruitCastsNoTabletopShadow),
            ("cavity", render.ContactShadowIsClippedToTheCavityAndStaysSubtle),
            ("silhouette", render.SilhouetteShowsTheContentAboveTheRim),
            ("detail", render.OmittedContentDetailKeepsTheLayering),
            ("dims", () => render.ContainmentRendersAtOtherSupportedDimensions(640, 480)),
            ("diagPasses", render.DiagnosticPassesDoNotChangeSubsequentContainmentRendering),
            ("export", () => render.ExportContainmentScenesTimingsAndSilhouettes("a red apple in a blue ceramic bowl on a wooden table", "apple-blue", 42, 0.35)),
            ("sheet", render.ExportContainmentReviewSheetAndManifest),
        };

        var passed = 0;
        var failures = new List<string>();
        foreach (var (name, body) in checks)
        {
            try
            {
                body();
                passed++;
            }
            catch (Exception exception)
            {
                failures.Add($"{name}: {exception.GetType().Name}: {Truncate(exception.Message)}");
            }
        }

        Emit($"detail-tests: passed={passed} failed={failures.Count} -> {string.Join("  ##  ", failures)}");
    }

    private static string Truncate(string value)
    {
        var single = value.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " | ", StringComparison.Ordinal);
        return single.Length <= 300 ? single : single[..300];
    }

    private static void Emit(string line)
    {
        using var stdout = Console.OpenStandardOutput();
        using var writer = new StreamWriter(stdout) { AutoFlush = true };
        writer.WriteLine("::error::" + line);
    }
}
