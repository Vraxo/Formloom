using System.Globalization;
using Formloom.Core.Models.Geometry;
using Formloom.Rendering.Skia.Scenes;
using SkiaSharp;

namespace Formloom.Rendering.Skia.Models;

/// <summary>Renders renderer-neutral model geometry using its selected material-token palette.</summary>
public static class ModelGeometryRenderer
{
    /// <summary>Renders closed paths as solid fills onto a transparent scene-normalized canvas.</summary>
    /// <param name="geometry">The evaluated model geometry and its selected material tokens.</param>
    /// <param name="width">The output width, in pixels.</param>
    /// <param name="height">The output height, in pixels.</param>
    /// <param name="options">Optional rasterization settings.</param>
    /// <returns>A PNG containing the filled model paths.</returns>
    public static RenderedImage Render(
        ModelGeometry geometry,
        int width = 1024,
        int height = 1024,
        RenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        if (width is < 64 or > 4096)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height is < 64 or > 4096)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        if (geometry.CoordinateSpace != ModelGeometryCoordinateSpace.SceneNormalized)
        {
            throw new NotSupportedException($"Coordinate space '{geometry.CoordinateSpace}' is not supported.");
        }

        var diagnostics = ModelGeometryValidator.Validate(geometry);
        if (!diagnostics.IsDefaultOrEmpty)
        {
            var message = string.Join(
                Environment.NewLine,
                diagnostics.Select(item => $"{item.Code}: {item.Message}"));
            throw new ArgumentException(message, nameof(geometry));
        }

        var colors = ResolvePathColors(geometry);
        var antialias = options?.Antialiasing ?? true;
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info) ?? throw new InvalidOperationException("Could not allocate raster surface.");
        var canvas = surface.Canvas;
        canvas.Scale(width, height);
        canvas.Clear(SKColors.Transparent);

        foreach (var path in geometry.Paths)
        {
            if (!path.Closed || path.MaterialSlot is null)
            {
                continue;
            }

            using var shape = CreateClosedPath(path);
            using var paint = new SKPaint
            {
                Color = colors[path.MaterialSlot],
                IsAntialias = antialias,
                Style = SKPaintStyle.Fill,
            };
            canvas.DrawPath(shape, paint);
        }

        using var image = surface.Snapshot();
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return new(width, height, encoded.ToArray(), geometry.ModelId);
    }

    private static Dictionary<string, SKColor> ResolvePathColors(ModelGeometry geometry)
    {
        var colors = new Dictionary<string, SKColor>(StringComparer.Ordinal);
        foreach (var path in geometry.Paths)
        {
            if (!path.Closed || path.MaterialSlot is null || colors.ContainsKey(path.MaterialSlot))
            {
                continue;
            }

            if (!geometry.MaterialTokens.TryGetValue(path.MaterialSlot, out var value))
            {
                throw new ArgumentException(
                    $"Closed path '{path.Id}' references missing material token '{path.MaterialSlot}'.",
                    nameof(geometry));
            }

            try
            {
                colors.Add(path.MaterialSlot, ParseColor(value));
            }
            catch (FormatException exception)
            {
                throw new ArgumentException(
                    $"Material token '{path.MaterialSlot}' has an invalid color value '{value}'.",
                    nameof(geometry),
                    exception);
            }
        }

        return colors;
    }

    private static SKColor ParseColor(string value)
    {
        if (value.Length is not (7 or 9) || value[0] != '#')
        {
            throw new FormatException("Colors must use #RRGGBB or #RRGGBBAA hexadecimal notation.");
        }

        var red = ParseByte(value.AsSpan(1, 2));
        var green = ParseByte(value.AsSpan(3, 2));
        var blue = ParseByte(value.AsSpan(5, 2));
        var alpha = value.Length == 9 ? ParseByte(value.AsSpan(7, 2)) : byte.MaxValue;
        return new(red, green, blue, alpha);
    }

    private static byte ParseByte(ReadOnlySpan<char> value) =>
        byte.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

    private static SKPath CreateClosedPath(ModelGeometryPath path)
    {
        using var builder = new SKPathBuilder();
        var first = path.Segments[0];
        builder.MoveTo(first.Start.X, first.Start.Y);
        foreach (var segment in path.Segments)
        {
            builder.CubicTo(
                segment.Control1.X,
                segment.Control1.Y,
                segment.Control2.X,
                segment.Control2.Y,
                segment.End.X,
                segment.End.Y);
        }

        builder.Close();
        return builder.Detach();
    }
}
