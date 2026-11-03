using System.Numerics;

namespace Formloom.Core;

public sealed record CubicSegment(Vector2 Start, Vector2 Control1, Vector2 Control2, Vector2 End)
{
    public Vector2 Evaluate(float t)
    {
        var inverse = 1 - t;
        return (inverse * inverse * inverse * Start) + (3 * inverse * inverse * t * Control1) +
            (3 * inverse * t * t * Control2) + (t * t * t * End);
    }
}
