using Formloom.Core;
using Formloom.Desktop;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Formloom.Desktop.Tests;

public sealed class GenerationServiceTests
{
    [Fact]
    public async Task RealPipelineProducesPreviewFromTheSamePngBytes()
    {
        var service = new GenerationService(NullLogger<GenerationService>.Instance);
        var result = await service.GenerateAsync(new GenerationRequest("a green apple on a wooden table", Width: 64, Height: 64));
        Assert.Null(result.Error);
        Assert.NotNull(result.Image);
        Assert.Equal(64, result.Image.Width);
        Assert.Equal("data:image/png;base64," + Convert.ToBase64String(result.Image.PngBytes), result.PreviewUrl);
    }

    [Fact]
    public async Task RealParserFailureReturnsErrorWithoutPreview()
    {
        var service = new GenerationService(NullLogger<GenerationService>.Instance);
        var result = await service.GenerateAsync(new GenerationRequest("a pear on a glass table"));
        Assert.NotNull(result.Error);
        Assert.Null(result.Image);
        Assert.Null(result.PreviewUrl);
    }
}
