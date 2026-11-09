using Formloom.Core.Scenes;
using Formloom.Desktop.PngExport;
using Formloom.Rendering.Skia.Scenes;
using Microsoft.Extensions.Logging.Abstractions;
using SkiaSharp;
using Xunit;

namespace Formloom.Desktop.Tests.PngExport;

public sealed class PngFileWriterTests
{
    [Fact]
    public async Task WritesActualPngAndReplacesExistingFileWithoutTemporaryRemnants()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"formloom-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "study.PNG");
            await File.WriteAllBytesAsync(path, [0], TestContext.Current.CancellationToken);
            var scene = SceneGenerator.Generate(new("a red apple on a wooden table", Width: 64, Height: 64)).Scene!;
            var image = StillLifeRenderer.Render(scene);
            var writer = new PngFileWriter(NullLogger<PngFileWriter>.Instance);
            var result = await writer.WriteAsync(path, image);
            Assert.False(result.IsError);
            Assert.Equal(image.PngBytes, await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
            using var decoded = SKBitmap.Decode(path);
            Assert.Equal(64, decoded.Width);
            Assert.Equal(64, decoded.Height);
            Assert.Single(Directory.GetFiles(directory));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CancelledSelectionIsNotAnError(string? path)
    {
        var writer = new PngFileWriter(NullLogger<PngFileWriter>.Instance);
        var result = await writer.WriteAsync(path, new RenderedImage(1, 1, [1]));
        Assert.False(result.IsError);
        Assert.Contains("cancelled", result.Message);
    }

    [Fact]
    public async Task WrongExtensionDoesNotAlterExistingFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"formloom-tests-{Guid.NewGuid():N}.txt");
        try
        {
            await File.WriteAllTextAsync(path, "original", TestContext.Current.CancellationToken);
            var writer = new PngFileWriter(NullLogger<PngFileWriter>.Instance);
            var result = await writer.WriteAsync(path, new RenderedImage(1, 1, [1]));
            Assert.True(result.IsError);
            Assert.Equal("original", await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task MissingDirectoryProducesActionableError()
    {
        var path = Path.Combine(Path.GetTempPath(), $"formloom-missing-{Guid.NewGuid():N}", "study.png");
        var writer = new PngFileWriter(NullLogger<PngFileWriter>.Instance);
        var result = await writer.WriteAsync(path, new RenderedImage(1, 1, [1]));
        Assert.True(result.IsError);
        Assert.Contains("permissions", result.Message);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task FailedReplacementCleansStagedFileAndLeavesDestinationUntouched()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"formloom-tests-{Guid.NewGuid():N}");
        var destination = Path.Combine(directory, "occupied.png");
        Directory.CreateDirectory(destination);
        var sentinel = Path.Combine(destination, "keep.txt");
        try
        {
            await File.WriteAllTextAsync(sentinel, "original", TestContext.Current.CancellationToken);
            var writer = new PngFileWriter(NullLogger<PngFileWriter>.Instance);
            var result = await writer.WriteAsync(destination, new RenderedImage(1, 1, [1]));
            Assert.True(result.IsError);
            Assert.Equal("original", await File.ReadAllTextAsync(sentinel, TestContext.Current.CancellationToken));
            Assert.Empty(Directory.GetFiles(directory));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
