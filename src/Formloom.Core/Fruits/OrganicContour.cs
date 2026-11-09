using System.Collections.Immutable;
using System.Numerics;
using Formloom.Core.Core;

namespace Formloom.Core.Fruits;

public sealed record OrganicContour(ImmutableArray<CubicSegment> Segments, Vector2 TopAttachment);
