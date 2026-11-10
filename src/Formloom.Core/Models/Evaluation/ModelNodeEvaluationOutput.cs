using System.Collections.Immutable;
using System.Numerics;
using Formloom.Core.Core;
using Formloom.Core.Models.Geometry;

namespace Formloom.Core.Models.Evaluation;

internal sealed record ModelNodeEvaluationOutput(
    ImmutableArray<ModelGeometryPath> Paths,
    ImmutableArray<CubicSegment> CurveSegments,
    ImmutableDictionary<string, Vector2> Landmarks);
