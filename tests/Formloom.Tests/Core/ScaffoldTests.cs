using System.Reflection;
using Xunit;

namespace Formloom.Tests.Core;

/// <summary>Checks that the solution foundations can be loaded by the test runner.</summary>
public sealed class ScaffoldTests
{
    /// <summary>Core must remain independent of renderer and desktop assemblies.</summary>
    [Fact]
    public void CoreHasNoRenderingOrDesktopDependencies()
    {
        var core = Assembly.Load("Formloom.Core");
        var dependencies = core.GetReferencedAssemblies();

        Assert.DoesNotContain(dependencies, reference =>
            reference.Name is not null &&
            (reference.Name.StartsWith("SkiaSharp", StringComparison.Ordinal) ||
             reference.Name.StartsWith("Photino", StringComparison.Ordinal) ||
             reference.Name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) ||
             reference.Name.StartsWith("Formloom.Rendering", StringComparison.Ordinal) ||
             reference.Name.StartsWith("Formloom.Desktop", StringComparison.Ordinal)));
    }
}
