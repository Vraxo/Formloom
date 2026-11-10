using System.Numerics;

namespace Formloom.Core.Models.Evaluation;

public sealed record ModelMountFrameValues(
    Vector2 OpeningCenter,
    float RadiusX,
    float RadiusY,
    float MouthTop,
    float RimThickness,
    float Headroom);
