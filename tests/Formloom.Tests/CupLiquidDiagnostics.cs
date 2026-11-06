using Formloom.Core;
using Xunit;

namespace Formloom.Tests;

// TEMPORARY diagnostics: runs the cup-liquid test bodies directly and reports the outcomes
// as GitHub Actions annotations, because this sandbox cannot read test logs or artifacts.
// Removed before merge.
public sealed class CupLiquidDiagnostics
{
    [Fact]
    public void Report()
    {
        var core = new CupLiquidTests();
        var render = new CupLiquidRenderingTests();
        var checks = new List<(string Name, Action Body)>();
        foreach (var prompt in CupLiquidTests.Rejected)
        {
            checks.Add((($"reject[{prompt}]"), new Action(() => core.UnsupportedCupContentsPhrasingsAreRejected(prompt))));
        }

        foreach (var glaze in new[] { "blue", "cream" })
        {
            foreach (var beverage in new[] { "coffee", "tea" })
            {
                var prompt = $"a {glaze} ceramic cup filled with {beverage} on a wooden table";
                checks.Add((($"core[{prompt}]"), new Action(() => core.EveryCupAndBeverageCombinationParsesAndGenerates(prompt, glaze, beverage))));
                checks.Add((($"render[{prompt}]"), new Action(() => render.EveryCombinationRendersDeterministicallyAtFullSize(prompt, glaze, beverage))));
                checks.Add((($"mouth[{prompt}]"), new Action(() => render.LiquidIsDrawnOnlyInsideTheCupMouth(prompt, glaze, beverage))));
            }
        }

        foreach (var (width, height) in new[] { (256, 256), (640, 480), (2048, 1024) })
        {
            checks.Add((($"dims[{width}x{height}]"), new Action(() => render.FilledCupsRenderAtOtherSupportedDimensions(width, height))));
        }

        checks.Add(("normalization", new Action(core.ParsingToleratesCaseWhitespaceAndTerminalPunctuation)));
        checks.Add(("notList", new Action(core.FilledCupPromptsAreNotObjectLists)));
        checks.Add(("ordinary", new Action(core.OrdinaryCupPromptsStillParseWithoutContents)));
        checks.Add(("exclusive", new Action(core.CupContentsAndContainmentAreMutuallyExclusive)));
        checks.Add(("validator", new Action(core.ValidatorRejectsBrokenCupContents)));
        checks.Add(("geometry", new Action(core.SurfaceGeometryIsDerivedFromTheCupOpening)));
        checks.Add(("independence", new Action(core.SurfaceIgnoresTheGlazeSeedAndVariation)));
        checks.Add(("visible", new Action(core.VisibleSurfaceStaysInsideTheOpeningWithARimMargin)));
        checks.Add(("distinct", new Action(render.CoffeeAndTeaAreVisiblyDistinct)));
        checks.Add(("glaze", new Action(render.GlazeDoesNotRecolorTheBeverage)));
        checks.Add(("silhouette", new Action(render.FilledCupKeepsTheEmptyCupSilhouetteAndHandleHole)));
        checks.Add(("shadow", new Action(render.FilledCupCastsTheSameTabletopShadow)));
        checks.Add(("detail", new Action(render.DetailOmissionKeepsTheDrinkButDropsItsAccents)));
        checks.Add(("diagnostics", new Action(render.DiagnosticPassesDoNotChangeSubsequentLiquidRendering)));
        checks.Add(("seeds", new Action(render.BeverageIdentitySurvivesSeeds)));
        checks.Add(("exportCase", new Action(() => render.ExportFilledCupsTimingsAndSilhouettes("a blue ceramic cup filled with coffee on a wooden table", "coffee-blue", 42, 0.35))));
        checks.Add(("sheet", new Action(render.ExportCupLiquidReviewSheetAndManifest)));

        var passed = 0;
        var index = 0;
        var failures = new List<string>();
        foreach (var (name, body) in checks)
        {
            index++;
            if (index % 20 == 0)
            {
                Emit($"cup-progress {index}/{checks.Count} failures={failures.Count} last={name}");
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
        return single.Length <= 320 ? single : single[..320];
    }

    private static void Emit(string line)
    {
        using var stdout = Console.OpenStandardOutput();
        using var writer = new StreamWriter(stdout) { AutoFlush = true };
        writer.WriteLine("::error::" + line);
    }
}
