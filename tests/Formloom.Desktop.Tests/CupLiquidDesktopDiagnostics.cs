using Xunit;

namespace Formloom.Desktop.Tests;

// TEMPORARY diagnostics for the desktop-side cup-liquid tests. Removed before merge.
public sealed class CupLiquidDesktopDiagnostics
{
    [Fact]
    public void Report()
    {
        var pipeline = new CupLiquidPipelineTests();
        var app = new AppComponentTests();
        var checks = new List<(string Name, Action Body)>();
        foreach (var (prompt, subject) in new[]
        {
            ("a blue ceramic cup filled with coffee on a wooden table", "coffee-in-blue-cup"),
            ("a cream ceramic cup filled with coffee on a wooden table", "coffee-in-cream-cup"),
            ("a blue ceramic cup filled with tea on a wooden table", "tea-in-blue-cup"),
            ("a cream ceramic cup filled with tea on a wooden table", "tea-in-cream-cup"),
        })
        {
            checks.Add(($"pipeline[{prompt}]", new Action(() => pipeline.FilledCupPromptsUseTheRealPipelineAndKeepThePreviewOnRejection(prompt, subject).GetAwaiter().GetResult())));
        }

        checks.Add(("metadata", new Action(() => pipeline.EmptyCupsAndFilledCupsKeepDistinctMetadata().GetAwaiter().GetResult())));
        checks.Add(("whitelist", new Action(pipeline.SaveNamesWhitelistCupLiquidTokens)));
        checks.Add(("help", new Action(() => app.PromptHelpMentionsContainment().GetAwaiter().GetResult())));
        checks.Add(("initial", new Action(() => app.InitialMarkupHasBoundDefaultsLabelsAndDisabledSave().GetAwaiter().GetResult())));
        foreach (var prompt in new[]
        {
            "a blue ceramic cup filled with coffee on a wooden table",
            "a cream ceramic cup filled with tea on a wooden table",
        })
        {
            checks.Add(($"component[{prompt}]", new Action(() => app.InputEventsAndSubmitUseEditedValuesAndRenderPreview(prompt).GetAwaiter().GetResult())));
        }

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
                var inner = exception is AggregateException aggregate && aggregate.InnerException is { } wrapped ? wrapped : exception;
                failures.Add($"{name}: {inner.GetType().Name}: {Short(inner.Message)}");
            }
        }

        Emit($"desktop-tests: passed={passed} failed={failures.Count} -> {string.Join("  ##  ", failures)}");
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
