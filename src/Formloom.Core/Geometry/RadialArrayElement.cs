namespace Formloom.Core.Geometry;

public readonly record struct RadialArrayElement(
    int Index,
    float Angle,
    float LengthScale,
    float WidthScale,
    float BendOffset,
    float ToneOffset);
