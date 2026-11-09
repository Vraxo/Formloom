namespace Formloom.Rendering.Skia.Scenes;

public sealed record RenderedImage(int Width, int Height, byte[] PngBytes, string Subject = "apple");
