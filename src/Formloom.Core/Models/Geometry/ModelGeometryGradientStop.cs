namespace Formloom.Core.Models.Geometry;

/// <summary>References a resolved palette token at a normalized gradient offset.</summary>
public sealed record ModelGeometryGradientStop(float Offset, string Token);
