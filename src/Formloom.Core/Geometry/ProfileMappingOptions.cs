namespace Formloom.Core.Geometry;

public readonly record struct ProfileMappingOptions(
    float ScaleX,
    float ScaleY,
    float Length,
    float Width,
    float RootOffset,
    float Bend,
    float TransverseNormalization,
    bool ReverseLongitudinal);
