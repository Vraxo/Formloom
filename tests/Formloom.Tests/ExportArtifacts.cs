namespace Formloom.Tests;

/// <summary>
/// Decides how much evidence the export tests write beside the test binaries.
/// </summary>
/// <remarks>
/// Every run writes the review sheets, their manifests and one canonical image per family, because
/// that is what a reviewer opens. The exhaustive per-case renders, silhouettes and timing sidecars
/// only follow when <c>FORMFLOOM_EXPORT_ALL</c> is <c>1</c>, which keeps the routine CI artifact
/// small; set the variable to get the full dump back. Gating the writes never gates the renders, so
/// every pixel assertion in those tests keeps its coverage either way.
/// </remarks>
internal static class ExportArtifacts
{
    /// <summary>Gets a value indicating whether the exhaustive per-case dump was requested.</summary>
    public static bool All { get; } = Environment.GetEnvironmentVariable("FORMFLOOM_EXPORT_ALL") == "1";

    /// <summary>Decides whether the case written as <paramref name="stem"/> is kept on this run.</summary>
    /// <param name="stem">The file name stem of the case, without extension.</param>
    /// <param name="canonicalStem">The one case per family that is always kept.</param>
    /// <returns>Whether the case's files should be written.</returns>
    public static bool Keeps(string stem, string canonicalStem) =>
        All || string.Equals(stem, canonicalStem, StringComparison.Ordinal);
}
