namespace Formloom.Core.Models.Definitions;

/// <summary>References a material color token at a normalized gradient offset.</summary>
public sealed record ModelLinearGradientStopDefinition(float Offset, string Token);
