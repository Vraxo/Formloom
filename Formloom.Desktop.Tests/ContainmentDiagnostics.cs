using System.Text;
using Xunit;

namespace Formloom.Desktop.Tests;

// TEMPORARY diagnostics: runs the desktop containment bodies directly and reports outcomes as
// GitHub Actions annotations. Removed before merge.
public sealed class ContainmentDiagnostics
{
    [Fact]
    public void Report()
    {
        var pipeline = new ContainmentPipelineTests();
        var app = new AppComponentTests();
        var checks = new List<(string Name, Action Body)>();
        foreach (var (prompt, subject) in new[]
        {
            ("a red apple in a blue ceramic bowl on a wooden table", "apple-in-blue-bowl"),
            ("a green apple in a cream ceramic bowl on a wooden table", "apple-in-cream-bowl"),
            ("a yellow apple in a blue ceramic bowl on a wooden table", "apple-in-blue-bowl"),
            ("a green pear in a cream ceramic bowl on a wooden table", "pear-in-cream-bowl"),
            ("a yellow pear in a blue ceramic bowl on a wooden table", "pear-in-blue-bowl"),
            ("an orange in a cream ceramic bowl on a wooden table", "orange-in-cream-bowl"),
        })
        {
            checks.Add(($"pipeline[{prompt}]", new Action(() => pipeline.ContainedPromptsUseTheRealPipelineAndKeepThePreviewOnRejection(prompt, subject).GetAwaiter().GetResult())));
        }

        checks.Add(("whitelist", new Action(() => pipeline.SaveNamesWhitelistContainmentTokens())));
        foreach (var prompt in new[]
        {
            "a red apple in a blue ceramic bowl on a wooden table",
            "a green pear in a cream ceramic bowl on a wooden table",
            "an orange in a blue ceramic bowl on a wooden table",
        })
        {
            checks.Add(($"component[{prompt}]", new Action(() => app.InputEventsAndSubmitUseEditedValuesAndRenderPreview(prompt).GetAwaiter().GetResult())));
        }

        checks.Add(("initial", new Action(() => app.InitialMarkupHasBoundDefaultsLabelsAndDisabledSave().GetAwaiter().GetResult())));

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
        return single.Length <= 260 ? single : single[..260];
    }

    private static void Emit(string line)
    {
        using var stdout = Console.OpenStandardOutput();
        using var writer = new StreamWriter(stdout, Encoding.UTF8) { AutoFlush = true };
        writer.WriteLine("::error::" + line);
    }
}
