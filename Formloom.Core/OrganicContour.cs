using System.Collections.Immutable;
using System.Numerics;

namespace Formloom.Core;

public sealed record OrganicContour(ImmutableArray<CubicSegment> Segments, Vector2 TopAttachment);
