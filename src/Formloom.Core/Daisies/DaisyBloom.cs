using System.Collections.Immutable;
using System.Numerics;

namespace Formloom.Core.Daisies;

// The open, radially arranged head: where it sits, the box the petals were authored in, the
// petals in back-to-front order and the disk that covers their roots.
public sealed record DaisyBloom(
    Vector2 Center,
    float RadiusX,
    float RadiusY,
    ImmutableArray<DaisyPetal> Petals,
    DaisyDisk Disk)
{
    public float Top => Center.Y - RadiusY;

    public float Left => Center.X - RadiusX;

    public float Right => Center.X + RadiusX;

    public float Bottom => Center.Y + RadiusY;
}
